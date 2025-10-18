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
                    if (n == 0) return null; // disconnected
                    if (buffer[0] == '\n') break; // end of message
                    sb.Append((char)buffer[0]);
                }
                return sb.ToString();
            }
            catch (IOException)
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

    public class PipeHost : IDisposable
    {
        private readonly string _pipeName;
        private readonly bool _allowEveryone;
        private readonly ConcurrentDictionary<string, PipeConnection> _connections = new();
        private readonly ConcurrentBag<NamedPipeServerStream> _listeners = new();
        private volatile bool _running;
        private int _connectionCounter;

        public event Action<PipeConnection, string>? MessageReceived;

        public PipeHost(string pipeName, bool allowEveryone = false)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _allowEveryone = allowEveryone;
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
            foreach (var listener in _listeners) { try { listener.Dispose(); } catch { } }
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
                    Debug.WriteLine($"Accept error: {ex.Message}");
                    server?.Dispose();
                }

                if (_running) await Task.Delay(50); // avoid tight loop if errors
            }
        }

        private PipeSecurity CreatePipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();
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
                exePath = proc?.MainModule?.FileName;
                pid = proc?.Id ?? -1;

                if (!_allowEveryone)
                {
                    bool trusted = false;
#if DEBUG
                    trusted = true;
#endif
                    if (!trusted)
                    {
                        try { server.Disconnect(); } catch { }
                        server.Dispose();
                        Debug.WriteLine($"Unauthorized client rejected: {exePath}");
                        return;
                    }
                }
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
                    if (n == 0) break;

                    if (buffer[0] == (byte)'\n')
                    {
                        var message = sb.ToString();
                        sb.Clear();

                        try { MessageReceived?.Invoke(connection, message); }
                        catch (Exception ex) { Debug.WriteLine($"MessageReceived handler error: {ex.Message}"); }
                    }
                    else sb.Append((char)buffer[0]);
                }
            }
            catch (IOException) { }
            finally
            {
                _connections.TryRemove(key, out _);
                connection.Dispose();
            }
        }

        private bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
        {
            process = null;
            if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid)) return false;

            try { process = Process.GetProcessById((int)pid); return true; }
            catch { return false; }
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
                try { kvp.Value.WriteMessage(message); } catch { }
            }
        }

        public void Dispose()
        {
            Stop();
            foreach (var kvp in _connections) kvp.Value.Dispose();
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

        public PipeClient(string pipeName = "REBOUND_SERVICE_HOST") => _pipeName = pipeName;

        public async Task StartAsync()
        {
            if (_running) throw new InvalidOperationException("Client already started");
            _running = true;

            await ConnectLoopAsync(); // fire-and-forget loop
        }

        public void Stop()
        {
            _running = false;
            try { _pipe?.Dispose(); } catch { }
        }

        private async Task ConnectLoopAsync()
        {
            while (_running)
            {
                try
                {
                    await EnsureConnectedAsync();

                    var buffer = new byte[1];
                    var sb = new StringBuilder();

                    while (_running && _pipe != null && _pipe.IsConnected)
                    {
                        int n = await _pipe.ReadAsync(buffer, 0, 1);
                        if (n == 0) break;

                        if (buffer[0] == (byte)'\n')
                        {
                            var msg = sb.ToString();
                            sb.Clear();

                            try { MessageReceived?.Invoke(msg); }
                            catch { }
                        }
                        else sb.Append((char)buffer[0]);
                    }
                }
                catch (IOException) { }
                finally
                {
                    try { _pipe?.Dispose(); } catch { }
                    _pipe = null;
                }

                if (_running) await Task.Delay(250); // prevent tight loop
            }
        }

        private async Task EnsureConnectedAsync()
        {
            if (_pipe != null && _pipe.IsConnected) return;

            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            while (_running)
            {
                try
                {
                    await _pipe.ConnectAsync(2000);
                    if (_pipe.IsConnected) return;
                }
                catch { if (!_running) throw new ObjectDisposedException("Client stopped"); }
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