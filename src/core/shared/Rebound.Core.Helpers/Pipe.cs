using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace Rebound.Core.Helpers
{
    /// <summary>
    /// Represents one connected client on the host side.
    /// Contains the server-side NamedPipe stream and small helpers for safe writes.
    /// </summary>
    public class PipeConnection : IDisposable
    {
        internal NamedPipeServerStream Stream { get; }
        internal int ProcessId { get; }
        public string? ExecutablePath { get; }

        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private bool _disposed;

        internal PipeConnection(NamedPipeServerStream stream, int pid, string? exePath)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            ProcessId = pid;
            ExecutablePath = exePath;
        }

        internal async Task WriteMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PipeConnection));

            var payload = Encoding.UTF8.GetBytes(message);
            var length = BitConverter.GetBytes(payload.Length);

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Stream.WriteAsync(length, 0, length.Length, cancellationToken).ConfigureAwait(false);
                await Stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
                await Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal async Task<string?> ReadMessageAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            // Read 4-byte length prefix
            int read = await ReadExactAsync(Stream, buffer, 0, 4, cancellationToken).ConfigureAwait(false);
            if (read == 0) return null;
            int length = BitConverter.ToInt32(buffer, 0);
            if (length <= 0) return string.Empty;
            if (buffer.Length < length) buffer = new byte[length];

            read = await ReadExactAsync(Stream, buffer, 0, length, cancellationToken).ConfigureAwait(false);
            if (read == 0) return null;
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        private static async Task<int> ReadExactAsync(Stream s, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < count)
            {
                int n = await s.ReadAsync(buffer, offset + total, count - total, cancellationToken).ConfigureAwait(false);
                if (n == 0) return total == 0 ? 0 : total; // remote closed
                total += n;
            }
            return total;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stream.Dispose();
            _writeLock.Dispose();
        }
    }

    /// <summary>
    /// PipeHost: always-listening named pipe server that accepts multiple clients, allows bi-directional messaging,
    /// and preserves the original permissions model from your constructor arguments.
    /// </summary>
    public class PipeHost : IDisposable
    {
        private readonly string _pipeName;
        private readonly bool _allowEveryone;
        private readonly ConcurrentDictionary<int, PipeConnection> _connections = new();
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Raised when a message arrives from a client. The first parameter is the origin connection.
        /// </summary>
        public event Func<PipeConnection, string, Task>? MessageReceived;

        public PipeHost(string pipeName, bool allowEveryone = false)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _allowEveryone = allowEveryone;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_cts != null) throw new InvalidOperationException("Host already started");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            // Always keep a pending server instance so clients can connect at any time.
            while (!cancellationToken.IsCancellationRequested)
            {
                var pipeSecurity = CreatePipeSecurity();

                var server = NamedPipeServerStreamAcl.Create(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0, 0,
                    pipeSecurity);

                // Start accept task for this instance. We intentionally don't await it here so the loop can immediately create the next instance.
                _ = Task.Run(() => AcceptClientInstanceAsync(server, cancellationToken), cancellationToken);

                // small spacing to avoid busy spinning the loop
                try { await Task.Delay(25, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }

        private PipeSecurity CreatePipeSecurity()
        {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule("Administrators",
                PipeAccessRights.FullControl, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule("SYSTEM",
                PipeAccessRights.FullControl, AccessControlType.Allow));

            if (_allowEveryone)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone",
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }
            else
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone",
                    PipeAccessRights.FullControl, AccessControlType.Deny));
            }

            return pipeSecurity;
        }

        private async Task AcceptClientInstanceAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
        {
            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                server.Dispose();
                return;
            }

            // Obtain client process info - if unavailable, still accept the connection but mark process id as -1
            if (!TryGetClientProcess(server, out var proc))
            {
                // If we can't get a process, accept but use pid -1
                var fallbackConn = new PipeConnection(server, -1, null);
                _ = HandleConnectionAsync(fallbackConn, cancellationToken);
                return;
            }

            // Check trusted clients when not allowing everyone
            var exePath = proc?.MainModule?.FileName;
            var pid = proc?.Id ?? -1;

            if (!_allowEveryone)
            {
                bool trusted = false;
#if DEBUG
                trusted = true;
#else
                if (!string.IsNullOrEmpty(exePath))
                {
                    foreach (var mod in Catalog.Mods)
                    {
                        if (string.Equals(mod.EntryExecutable, exePath, StringComparison.OrdinalIgnoreCase))
                        {
                            trusted = true;
                            break;
                        }
                    }
                }
#endif
                if (!trusted)
                {
                    // Reject: close the connection gracefully
                    try { server.Disconnect(); } catch { }
                    server.Dispose();
                    Debug.WriteLine($"Unauthorized client rejected: {exePath}");
                    return;
                }
            }

            var connection = new PipeConnection(server, pid, exePath);
            if (_connections.TryAdd(connection.ProcessId, connection))
            {
                _ = HandleConnectionAsync(connection, cancellationToken);
            }
            else
            {
                // PID collision or already connected - dispose the new one
                connection.Dispose();
            }
        }

        private async Task HandleConnectionAsync(PipeConnection connection, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            try
            {
                while (connection.Stream.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var msg = await connection.ReadMessageAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (msg == null) break; // client disconnected
                    if (MessageReceived != null)
                        await MessageReceived.Invoke(connection, msg).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection {connection.ProcessId} error: {ex.Message}");
            }
            finally
            {
                _connections.TryRemove(connection.ProcessId, out _);
                connection.Dispose();
            }
        }

        /// <summary>
        /// Sends a message to a specific connection.
        /// </summary>
        public async Task SendAsync(PipeConnection connection, string message, CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (!_connections.ContainsKey(connection.ProcessId)) throw new InvalidOperationException("Connection is not registered");
            await connection.WriteMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Broadcasts a message to all currently connected clients.
        /// </summary>
        public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            foreach (var kvp in _connections)
            {
                try
                {
                    tasks.Add(kvp.Value.WriteMessageAsync(message, cancellationToken));
                }
                catch
                {
                    // ignore individual write preparation errors; cleanup happens elsewhere
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
        {
            process = null;
            if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid)) return false;

            try { process = Process.GetProcessById((int)pid); return true; }
            catch { return false; }
        }

        public void Dispose()
        {
            Stop();
            foreach (var kvp in _connections)
            {
                kvp.Value.Dispose();
            }
            _connections.Clear();
            _cts?.Dispose();
        }
    }

    /// <summary>
    /// PipeClient: always-reconnecting client with symmetric APIs to the host. Maintains a single connection to the host
    /// and exposes an event for incoming messages.
    /// </summary>
    public class PipeClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _pipe;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        /// <summary>
        /// Raised when a message arrives from the server.
        /// </summary>
        public event Func<string, Task>? MessageReceived;

        public PipeClient(string pipeName = "REBOUND_SERVICE_HOST") => _pipeName = pipeName;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_cts != null) throw new InvalidOperationException("Client already started");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenTask = Task.Run(() => ClientLoopAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task ClientLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

                    // read loop
                    while (_pipe != null && _pipe.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        // read length prefix
                        int read = await ReadExactAsync(_pipe, buffer, 0, 4, cancellationToken).ConfigureAwait(false);
                        if (read == 0) break; // server closed

                        int length = BitConverter.ToInt32(buffer, 0);
                        if (length <= 0)
                        {
                            if (MessageReceived != null) await MessageReceived(string.Empty).ConfigureAwait(false);
                            continue;
                        }

                        byte[] payload = buffer;
                        if (payload.Length < length) payload = new byte[length];

                        read = await ReadExactAsync(_pipe, payload, 0, length, cancellationToken).ConfigureAwait(false);
                        if (read == 0) break;

                        var msg = Encoding.UTF8.GetString(payload, 0, length);
                        if (MessageReceived != null) await MessageReceived.Invoke(msg).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (IOException) { /* server disappeared */ }
                catch (Exception ex) { Debug.WriteLine($"Client read loop error: {ex.Message}"); }

                try { _pipe?.Dispose(); } catch { }
                _pipe = null;

                // wait before reconnecting
                try { await Task.Delay(500, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }

        private static async Task<int> ReadExactAsync(Stream s, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < count)
            {
                int n = await s.ReadAsync(buffer, offset + total, count - total, cancellationToken).ConfigureAwait(false);
                if (n == 0) return total == 0 ? 0 : total;
                total += n;
            }
            return total;
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_pipe != null && _pipe.IsConnected) return;

            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            // Keep trying to connect until cancelled. This satisfies "clients always want to connect".
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // small timeout to keep loops responsive to cancellation
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(2));

                    await _pipe.ConnectAsync(cts.Token).ConfigureAwait(false);
                    if (_pipe.IsConnected) return;
                }
                catch (OperationCanceledException) { if (cancellationToken.IsCancellationRequested) throw; }
                catch (Exception) { /* swallow and retry */ }

                try { await Task.Delay(250, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }

            throw new OperationCanceledException("Connect cancelled");
        }

        public async Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_cts == null) throw new InvalidOperationException("Client not started");

            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // ensure connected before sending
                await EnsureConnectedAsync(_cts.Token).ConfigureAwait(false);

                var payload = Encoding.UTF8.GetBytes(message);
                var length = BitConverter.GetBytes(payload.Length);

                await _pipe!.WriteAsync(length, 0, length.Length, cancellationToken).ConfigureAwait(false);
                await _pipe.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
                await _pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally { _sendLock.Release(); }
        }

        public void Dispose()
        {
            Stop();
            _pipe?.Dispose();
            _sendLock.Dispose();
            _cts?.Dispose();
        }
    }
}
