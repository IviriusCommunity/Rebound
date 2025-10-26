// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace Rebound.Core.Helpers
{
    public class PipeConnection : IDisposable
    {
        internal NamedPipeServerStream Stream { get; }
        internal int ProcessId { get; }
        public string? ExecutablePath { get; }

        private readonly object _writeLock = new();
        private bool _disposed;

        internal PipeConnection(NamedPipeServerStream stream, int pid, string? exePath)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            ProcessId = pid;
            ExecutablePath = exePath;
        }

        public void WriteMessage(string message)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PipeConnection));
            if (message is null) throw new ArgumentNullException(nameof(message));

            var payload = Encoding.UTF8.GetBytes(message + "\n");
            lock (_writeLock)
            {
                Stream.Write(payload, 0, payload.Length);
                Stream.Flush();
            }
        }

        public string? ReadMessage()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PipeConnection));

            var sb = new StringBuilder();
            var buffer = new byte[1];

            try
            {
                while (true)
                {
                    int n = Stream.Read(buffer, 0, 1);
                    if (n == 0)
                    {
                        return null; // disconnected
                    }
                    if (buffer[0] == '\n') break; // end of message
                    sb.Append((char)buffer[0]);
                }
                var message = sb.ToString();
                return message;
            }
            catch (IOException ex)
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Stream.Dispose(); } catch { }
        }
    }

    public enum AccessLevel
    {
        CurrentProcess,
        ModWhitelist,
        Everyone
    }

    public class PipeHost : IDisposable
    {
        private readonly string _pipeName;
        private readonly AccessLevel _accessLevel;
        private readonly ConcurrentDictionary<string, PipeConnection> _connections = new();
        private readonly ConcurrentBag<NamedPipeServerStream> _listeners = new();
        private volatile bool _running;
        private int _connectionCounter;

        public event Action<PipeConnection, string>? MessageReceived;

        public PipeHost(string pipeName, AccessLevel accessLevel = AccessLevel.ModWhitelist)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _accessLevel = accessLevel;
        }

        public async Task StartAsync()
        {
            if (_running) throw new InvalidOperationException("Host already started");
            _running = true;
            await AcceptLoopAsync(); // fire-and-forget
        }

        public void Stop()
        {
            _running = false;
            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Dispose();
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (_running)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    var pipeSecurity = CreatePipeSecurity();

                    server = NamedPipeServerStreamAcl.Create(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous,
                        0, 0,
                        pipeSecurity);

                    _listeners.Add(server);

                    await server.WaitForConnectionAsync();

                    if (!_running)
                    {
                        server.Dispose();
                        break;
                    }

                    _ = HandleClientAsync(server); // fire-and-forget per client
                    server = null; // ownership passed
                }
                catch (Exception ex)
                {
                    server?.Dispose();
                }

                if (_running)
                {
                    await Task.Delay(50); // avoid tight loop if errors
                }
            }
        }

        private PipeSecurity CreatePipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();

            // Grant access to current user
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    currentUser,
                    PipeAccessRights.FullControl,
                    AccessControlType.Allow));
            }
            else
            {

            }

            // Grant access to Everyone
            var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            pipeSecurity.AddAccessRule(new PipeAccessRule(world, PipeAccessRights.ReadWrite, AccessControlType.Allow));

            return pipeSecurity;
        }

        private async Task HandleClientAsync(NamedPipeServerStream server)
        {
            string? exePath = null;
            int pid = -1;

            if (TryGetClientProcess(server, out var proc))
            {
#if !DEBUG
                // Since processes like Rebound Shell can't access the PID of Task Manager and other elevated apps, the following two lines will be moved to the ModWhiteList access list value.
                exePath = proc?.MainModule?.FileName;
                pid = proc?.Id ?? -1;

                switch (_accessLevel)
                {
                    case AccessLevel.Everyone:
                        {
                            Debug.WriteLine($"[PipeHost] AccessLevel=Everyone, allowing connection");
                            break;
                        }
                    case AccessLevel.CurrentProcess:
                        {
                            var currentPid = Process.GetCurrentProcess().Id;
                            Debug.WriteLine($"[PipeHost] AccessLevel=CurrentProcess, checking PID {pid} vs {currentPid}");
                            if (pid != currentPid)
                            {
                                Debug.WriteLine($"[PipeHost] Access denied: PID mismatch");
                                server.Disconnect();
                                server.Dispose();
                                return;
                            }
                            Debug.WriteLine($"[PipeHost] Access granted: PID matches");
                            break;
                        }
                }
#endif

            }
            else
            {

            }

            var key = $"{pid}_{Interlocked.Increment(ref _connectionCounter)}";
            var connection = new PipeConnection(server, pid, exePath);

            if (!_connections.TryAdd(key, connection))
            {
                connection.Dispose();
                return;
            }

            try
            {
                var buffer = new byte[1];
                var sb = new StringBuilder();

                while (_running && server.IsConnected)
                {
                    int n = await server.ReadAsync(buffer, 0, 1);
                    if (n == 0)
                    {
                        break;
                    }

                    if (buffer[0] == (byte)'\n')
                    {
                        var message = sb.ToString();
                        sb.Clear();

                        try
                        {
                            MessageReceived?.Invoke(connection, message);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else sb.Append((char)buffer[0]);
                }
            }
            catch (IOException ex)
            {

            }
            finally
            {
                _connections.TryRemove(key, out _);
                connection.Dispose();
            }
        }

        private bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
        {
            process = null;
            if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid))
            {
                return false;
            }

            Debug.WriteLine($"[PipeHost] Client process ID: {pid}");
            try
            {
                process = Process.GetProcessById((int)pid);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Send(PipeConnection connection, string message)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            connection.WriteMessage(message);
        }

        public void Broadcast(string message)
        {
            foreach (var kvp in _connections.ToArray())
            {
                try
                {
                    kvp.Value.WriteMessage(message);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void Dispose()
        {
            Stop();
            foreach (var kvp in _connections)
            {
                kvp.Value.Dispose();
            }
            _connections.Clear();
        }
    }

    public class PipeClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _pipe;
        private readonly object _sendLock = new();
        private volatile bool _running;

        public event Action<string>? MessageReceived;

        public PipeClient(string pipeName = "REBOUND_SERVICE_HOST")
        {
            _pipeName = pipeName;
        }

        public void Stop()
        {
            _running = false;
            try { _pipe?.Dispose(); } catch (Exception ex) { }
        }

        public async Task ConnectAsync()
        {
            if (_pipe != null && _pipe.IsConnected)
            {
                return;
            }

            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await _pipe.ConnectAsync(2000); // wait for connection
            }
            catch (Exception ex)
            {
                throw;
            }

            var thread1 = new Thread(async () =>
            {
                try
                {
                    await EnsureConnectedAsync();
                }
                catch (Exception ex)
                {

                }
            })
            {
                IsBackground = true
            };
            thread1.SetApartmentState(ApartmentState.STA);
            thread1.Start();

            var thread2 = new Thread(async () =>
            {
                _running = true;
                try
                {
                    await ReadLoopAsync();
                }
                catch (Exception ex)
                {

                }
            })
            {
                IsBackground = true
            };
            thread2.SetApartmentState(ApartmentState.STA);
            thread2.Start();
        }

        private async Task ReadLoopAsync()
        {
            if (_pipe == null || !_pipe.IsConnected)
            {
                return;
            }

            var buffer = new byte[1];
            var sb = new StringBuilder();

            try
            {
                while (_running && _pipe.IsConnected)
                {
                    int n = await _pipe.ReadAsync(buffer, 0, 1);
                    if (n == 0)
                    {
                        break;
                    }

                    if (buffer[0] == (byte)'\n')
                    {
                        var msg = sb.ToString();
                        sb.Clear();
                        try
                        {
                            MessageReceived?.Invoke(msg);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    else sb.Append((char)buffer[0]);
                }
            }
            catch (IOException ex)
            {

            }
            finally
            {
                _pipe?.Dispose();
                _pipe = null;
            }
        }

        private async Task EnsureConnectedAsync()
        {
            if (_pipe != null && _pipe.IsConnected)
            {
                return;
            }

            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            while (_running)
            {
                try
                {
                    await _pipe.ConnectAsync(2000);
                    if (_pipe.IsConnected)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (!_running)
                    {
                        throw new ObjectDisposedException("Client stopped");
                    }
                }
                await Task.Delay(250);
            }

            throw new ObjectDisposedException("Client stopped");
        }

        public async Task SendAsync(string message)
        {
            if (!_running) throw new InvalidOperationException("Client not started");
            if (message == null) throw new ArgumentNullException(nameof(message));

            var payload = Encoding.UTF8.GetBytes(message + "\n");

            lock (_sendLock)
            {
                if (_pipe == null) throw new InvalidOperationException("Pipe not connected");
                _pipe.WriteAsync(payload, 0, payload.Length);
                _pipe.FlushAsync();
            }
        }

        public void Dispose()
        {
            Stop();
            _pipe?.Dispose();
        }
    }
}