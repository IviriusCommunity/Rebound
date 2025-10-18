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
            Debug.WriteLine($"[PipeConnection] Created for PID={pid}, ExePath={exePath}");
        }

        public void WriteMessage(string message)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PipeConnection));
            if (message is null) throw new ArgumentNullException(nameof(message));

            Debug.WriteLine($"[PipeConnection] Writing message to PID={ProcessId}: {message}");
            var payload = Encoding.UTF8.GetBytes(message + "\n");
            lock (_writeLock)
            {
                Stream.Write(payload, 0, payload.Length);
                Stream.Flush();
            }
            Debug.WriteLine($"[PipeConnection] Message written successfully to PID={ProcessId}");
        }

        public string? ReadMessage()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PipeConnection));

            Debug.WriteLine($"[PipeConnection] Reading message from PID={ProcessId}");
            var sb = new StringBuilder();
            var buffer = new byte[1];

            try
            {
                while (true)
                {
                    int n = Stream.Read(buffer, 0, 1);
                    if (n == 0)
                    {
                        Debug.WriteLine($"[PipeConnection] Disconnected while reading from PID={ProcessId}");
                        return null; // disconnected
                    }
                    if (buffer[0] == '\n') break; // end of message
                    sb.Append((char)buffer[0]);
                }
                var message = sb.ToString();
                Debug.WriteLine($"[PipeConnection] Read message from PID={ProcessId}: {message}");
                return message;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[PipeConnection] IOException while reading from PID={ProcessId}: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Debug.WriteLine($"[PipeConnection] Disposing connection for PID={ProcessId}");
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
            Debug.WriteLine($"[PipeHost] Created with PipeName={pipeName}, AccessLevel={accessLevel}");
        }

        public async Task StartAsync()
        {
            if (_running) throw new InvalidOperationException("Host already started");
            Debug.WriteLine($"[PipeHost] Starting host for pipe '{_pipeName}'");
            _running = true;
            await AcceptLoopAsync(); // fire-and-forget
        }

        public void Stop()
        {
            Debug.WriteLine($"[PipeHost] Stopping host for pipe '{_pipeName}'");
            _running = false;
            foreach (var listener in _listeners)
            {
                try
                {
                    Debug.WriteLine($"[PipeHost] Disposing listener");
                    listener.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeHost] Error disposing listener: {ex.Message}");
                }
            }
        }

        private async Task AcceptLoopAsync()
        {
            Debug.WriteLine($"[PipeHost] AcceptLoop started");
            while (_running)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    Debug.WriteLine($"[PipeHost] Creating pipe security");
                    var pipeSecurity = CreatePipeSecurity();

                    Debug.WriteLine($"[PipeHost] Creating NamedPipeServerStream for '{_pipeName}'");
                    server = NamedPipeServerStreamAcl.Create(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous,
                        0, 0,
                        pipeSecurity);

                    _listeners.Add(server);
                    Debug.WriteLine($"[PipeHost] Pipe created successfully, waiting for connection...");

                    await server.WaitForConnectionAsync();
                    Debug.WriteLine($"[PipeHost] Client connected!");

                    if (!_running)
                    {
                        Debug.WriteLine($"[PipeHost] Host stopped, disposing server");
                        server.Dispose();
                        break;
                    }

                    Debug.WriteLine($"[PipeHost] Spawning client handler");
                    _ = HandleClientAsync(server); // fire-and-forget per client
                    server = null; // ownership passed
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeHost] Accept error: {ex.GetType().Name}: {ex.Message}");
                    Debug.WriteLine($"[PipeHost] StackTrace: {ex.StackTrace}");
                    if (ex is UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"[PipeHost] UnauthorizedAccessException - Possible causes:");
                        Debug.WriteLine($"[PipeHost]   - Pipe already exists from previous instance");
                        Debug.WriteLine($"[PipeHost]   - Insufficient permissions");
                        Debug.WriteLine($"[PipeHost]   - Security software blocking pipe creation");
                    }
                    server?.Dispose();
                }

                if (_running)
                {
                    Debug.WriteLine($"[PipeHost] Delaying 50ms before next accept attempt");
                    await Task.Delay(50); // avoid tight loop if errors
                }
            }
            Debug.WriteLine($"[PipeHost] AcceptLoop ended");
        }

        private PipeSecurity CreatePipeSecurity()
        {
            Debug.WriteLine($"[PipeHost] Creating PipeSecurity");
            var pipeSecurity = new PipeSecurity();

            // Grant access to current user
            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                Debug.WriteLine($"[PipeHost] Adding FullControl for current user: {currentUser.Value}");
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    currentUser,
                    PipeAccessRights.FullControl,
                    AccessControlType.Allow));
            }
            else
            {
                Debug.WriteLine($"[PipeHost] WARNING: Could not get current user identity");
            }

            // Grant access to Everyone
            var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            Debug.WriteLine($"[PipeHost] Adding ReadWrite for Everyone");
            pipeSecurity.AddAccessRule(new PipeAccessRule(world, PipeAccessRights.ReadWrite, AccessControlType.Allow));

            Debug.WriteLine($"[PipeHost] PipeSecurity created successfully");
            return pipeSecurity;
        }

        private async Task HandleClientAsync(NamedPipeServerStream server)
        {
            Debug.WriteLine($"[PipeHost] HandleClient started");
            string? exePath = null;
            int pid = -1;

            if (TryGetClientProcess(server, out var proc))
            {
                exePath = proc?.MainModule?.FileName;
                pid = proc?.Id ?? -1;
                Debug.WriteLine($"[PipeHost] Client identified: PID={pid}, ExePath={exePath}");

#if !DEBUG
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
#else
                Debug.WriteLine($"[PipeHost] DEBUG mode - skipping access level check");
#endif
            }
            else
            {
                Debug.WriteLine($"[PipeHost] WARNING: Could not identify client process");
            }

            var key = $"{pid}_{Interlocked.Increment(ref _connectionCounter)}";
            Debug.WriteLine($"[PipeHost] Creating connection with key: {key}");
            var connection = new PipeConnection(server, pid, exePath);

            if (!_connections.TryAdd(key, connection))
            {
                Debug.WriteLine($"[PipeHost] ERROR: Failed to add connection to dictionary");
                connection.Dispose();
                return;
            }

            Debug.WriteLine($"[PipeHost] Connection added, total connections: {_connections.Count}");

            try
            {
                var buffer = new byte[1];
                var sb = new StringBuilder();

                Debug.WriteLine($"[PipeHost] Starting message read loop for connection {key}");
                while (_running && server.IsConnected)
                {
                    int n = await server.ReadAsync(buffer, 0, 1);
                    if (n == 0)
                    {
                        Debug.WriteLine($"[PipeHost] Client disconnected (read 0 bytes) for connection {key}");
                        break;
                    }

                    if (buffer[0] == (byte)'\n')
                    {
                        var message = sb.ToString();
                        sb.Clear();
                        Debug.WriteLine($"[PipeHost] Message received from connection {key}: {message}");

                        try
                        {
                            MessageReceived?.Invoke(connection, message);
                            Debug.WriteLine($"[PipeHost] MessageReceived event invoked successfully");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[PipeHost] MessageReceived handler error: {ex.Message}");
                        }
                    }
                    else sb.Append((char)buffer[0]);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[PipeHost] IOException in read loop for connection {key}: {ex.Message}");
            }
            finally
            {
                Debug.WriteLine($"[PipeHost] Cleaning up connection {key}");
                _connections.TryRemove(key, out _);
                connection.Dispose();
                Debug.WriteLine($"[PipeHost] Connection {key} removed, remaining connections: {_connections.Count}");
            }
        }

        private bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
        {
            process = null;
            if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid))
            {
                Debug.WriteLine($"[PipeHost] Failed to get client process ID");
                return false;
            }

            Debug.WriteLine($"[PipeHost] Client process ID: {pid}");
            try
            {
                process = Process.GetProcessById((int)pid);
                Debug.WriteLine($"[PipeHost] Successfully got Process object for PID {pid}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PipeHost] Failed to get Process object for PID {pid}: {ex.Message}");
                return false;
            }
        }

        public void Send(PipeConnection connection, string message)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            Debug.WriteLine($"[PipeHost] Sending message via connection to PID={connection.ProcessId}");
            connection.WriteMessage(message);
        }

        public void Broadcast(string message)
        {
            Debug.WriteLine($"[PipeHost] Broadcasting message to {_connections.Count} connections: {message}");
            foreach (var kvp in _connections.ToArray())
            {
                try
                {
                    Debug.WriteLine($"[PipeHost] Broadcasting to connection {kvp.Key}");
                    kvp.Value.WriteMessage(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeHost] Failed to broadcast to connection {kvp.Key}: {ex.Message}");
                }
            }
            Debug.WriteLine($"[PipeHost] Broadcast complete");
        }

        public void Dispose()
        {
            Debug.WriteLine($"[PipeHost] Disposing PipeHost");
            Stop();
            foreach (var kvp in _connections)
            {
                Debug.WriteLine($"[PipeHost] Disposing connection {kvp.Key}");
                kvp.Value.Dispose();
            }
            _connections.Clear();
            Debug.WriteLine($"[PipeHost] PipeHost disposed");
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
            Debug.WriteLine($"[PipeClient] Created with PipeName={pipeName}");
        }

        public void Stop()
        {
            Debug.WriteLine($"[PipeClient] Stopping client");
            _running = false;
            try { _pipe?.Dispose(); } catch (Exception ex) { Debug.WriteLine($"[PipeClient] Error disposing pipe: {ex.Message}"); }
        }

        public async Task ConnectAsync()
        {
            if (_pipe != null && _pipe.IsConnected)
            {
                Debug.WriteLine($"[PipeClient] Already connected");
                return;
            }

            Debug.WriteLine($"[PipeClient] Connecting to pipe '{_pipeName}'");
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await _pipe.ConnectAsync(2000); // wait for connection
                Debug.WriteLine($"[PipeClient] Connected successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PipeClient] Connection failed: {ex.Message}");
                throw;
            }

            Debug.WriteLine($"[PipeClient] Starting EnsureConnected thread");
            var thread1 = new Thread(async () =>
            {
                try
                {
                    await EnsureConnectedAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeClient] EnsureConnected thread error: {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            thread1.SetApartmentState(ApartmentState.STA);
            thread1.Start();

            Debug.WriteLine($"[PipeClient] Starting ReadLoop thread");
            var thread2 = new Thread(async () =>
            {
                _running = true;
                try
                {
                    await ReadLoopAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeClient] ReadLoop thread error: {ex.Message}");
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
                Debug.WriteLine($"[PipeClient] ReadLoop: Pipe not connected");
                return;
            }

            Debug.WriteLine($"[PipeClient] ReadLoop started");
            var buffer = new byte[1];
            var sb = new StringBuilder();

            try
            {
                while (_running && _pipe.IsConnected)
                {
                    int n = await _pipe.ReadAsync(buffer, 0, 1);
                    if (n == 0)
                    {
                        Debug.WriteLine($"[PipeClient] Server disconnected (read 0 bytes)");
                        break;
                    }

                    if (buffer[0] == (byte)'\n')
                    {
                        var msg = sb.ToString();
                        sb.Clear();
                        Debug.WriteLine($"[PipeClient] Message received: {msg}");
                        try
                        {
                            MessageReceived?.Invoke(msg);
                            Debug.WriteLine($"[PipeClient] MessageReceived event invoked");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[PipeClient] MessageReceived handler error: {ex.Message}");
                        }
                    }
                    else sb.Append((char)buffer[0]);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[PipeClient] IOException in ReadLoop: {ex.Message}");
            }
            finally
            {
                Debug.WriteLine($"[PipeClient] ReadLoop ending, disposing pipe");
                _pipe?.Dispose();
                _pipe = null;
            }
        }

        private async Task EnsureConnectedAsync()
        {
            if (_pipe != null && _pipe.IsConnected)
            {
                Debug.WriteLine($"[PipeClient] EnsureConnected: Already connected");
                return;
            }

            Debug.WriteLine($"[PipeClient] EnsureConnected: Starting reconnection loop");
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            while (_running)
            {
                try
                {
                    Debug.WriteLine($"[PipeClient] EnsureConnected: Attempting to connect...");
                    await _pipe.ConnectAsync(2000);
                    if (_pipe.IsConnected)
                    {
                        Debug.WriteLine($"[PipeClient] EnsureConnected: Connected!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PipeClient] EnsureConnected: Connection attempt failed: {ex.Message}");
                    if (!_running)
                    {
                        Debug.WriteLine($"[PipeClient] EnsureConnected: Client stopped, exiting");
                        throw new ObjectDisposedException("Client stopped");
                    }
                }
                Debug.WriteLine($"[PipeClient] EnsureConnected: Waiting 250ms before retry");
                await Task.Delay(250);
            }

            Debug.WriteLine($"[PipeClient] EnsureConnected: Client stopped");
            throw new ObjectDisposedException("Client stopped");
        }

        public async Task SendAsync(string message)
        {
            if (!_running) throw new InvalidOperationException("Client not started");
            if (message == null) throw new ArgumentNullException(nameof(message));

            Debug.WriteLine($"[PipeClient] Sending message: {message}");
            var payload = Encoding.UTF8.GetBytes(message + "\n");

            lock (_sendLock)
            {
                if (_pipe == null) throw new InvalidOperationException("Pipe not connected");
                _pipe.WriteAsync(payload, 0, payload.Length);
                _pipe.FlushAsync();
            }
            Debug.WriteLine($"[PipeClient] Message sent successfully");
        }

        public void Dispose()
        {
            Debug.WriteLine($"[PipeClient] Disposing PipeClient");
            Stop();
            _pipe?.Dispose();
            Debug.WriteLine($"[PipeClient] PipeClient disposed");
        }
    }
}