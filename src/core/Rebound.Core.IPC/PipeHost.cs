// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Core.IPC;

public sealed class HostMessageReceivedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public interface IPipeHost
{
    event EventHandler<HostMessageReceivedEventArgs>? MessageReceived;

    Task StartAsync();

    void StopHost();

    void Broadcast(string message);
}

/// <summary>
/// Initializes a new instance of the PipeHost class with the specified pipe name and access level.
/// </summary>
/// <param name="pipeName">The name of the pipe to host. This value cannot be null.</param>
/// <param name="accessLevel">The access level that determines which clients are permitted to connect. Defaults to ModWhitelist if not
/// specified.</param>
/// <exception cref="ArgumentNullException">Thrown if <paramref name="pipeName"/> is null.</exception>
public sealed class PipeHost(string pipeName, AccessLevel accessLevel = AccessLevel.ModWhitelist) : IPipeHost, IDisposable
{
    private readonly string _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
#pragma warning disable CA1823 // Avoid unused private fields
    private readonly AccessLevel _accessLevel = accessLevel;
#pragma warning restore CA1823 // Avoid unused private fields
    private readonly ConcurrentDictionary<string, PipeConnection> _connections = new();
    private readonly ConcurrentBag<NamedPipeServerStream> _listeners = [];
    private volatile bool _running;
    private int _connectionCounter;

    /// <summary>
    /// Occurs when a message is received from the pipe connection.
    /// </summary>
    /// <remarks>The event provides the associated <see cref="PipeConnection"/> and the received message as a
    /// string. Subscribers can use this event to process incoming messages in real time. This event is typically raised
    /// on the thread that receives the message; ensure thread safety when handling the event if accessing shared
    /// resources.</remarks>
    public event EventHandler<HostMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Gets the collection of file system paths that are permitted for access or operations.
    /// </summary>
    /// <remarks>The returned list contains absolute or relative paths that are considered safe or allowed by
    /// the application. Modifying the contents of this list affects which paths are treated as whitelisted. This
    /// property is read-only; to change the set of whitelisted paths, modify the list directly.</remarks>
    public Collection<string> WhitelistedPaths { get; } = [];

    /// <summary>
    /// Starts the host asynchronously, enabling it to begin accepting incoming connections.
    /// </summary>
    /// <remarks>This method should be called only once during the host's lifecycle. Subsequent calls will
    /// result in an exception. The host will begin accepting connections immediately after this method
    /// completes.</remarks>
    /// <returns>A task that represents the asynchronous start operation. The task completes when the host has started accepting
    /// connections.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the host is already running.</exception>
    public async Task StartAsync()
    {
        if (_running) throw new InvalidOperationException("Host already started");
        _running = true;
        await AcceptLoopAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Stops the current operation and releases all associated resources.
    /// </summary>
    /// <remarks>After calling this method, the instance will no longer process events or notify listeners.
    /// This method disposes all registered listeners and suppresses any exceptions thrown during disposal. It is safe
    /// to call this method multiple times; subsequent calls have no effect.</remarks>
    public void StopHost()
    {
        _running = false;
        foreach (var listener in _listeners)
        {
            try
            {
                listener.Dispose();
            }
            catch { }
        }
    }

    /// <summary>
    /// Continuously accepts incoming client connections on the named pipe server while the server is running.
    /// </summary>
    /// <remarks>This method listens for new client connections and initiates handling for each client in a
    /// separate asynchronous operation. The loop continues until the server is stopped. If an error occurs while
    /// accepting a connection, the method waits briefly before retrying to avoid a tight error loop.</remarks>
    /// <returns>A task that represents the asynchronous accept loop operation.</returns>
    private async Task AcceptLoopAsync()
    {
        var pipeSecurity = CreatePipeSecurity();

        while (_running)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = NamedPipeServerStreamAcl.Create(
                    _pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    0, 0,
                    pipeSecurity);

                _listeners.Add(server);

                await server.WaitForConnectionAsync().ConfigureAwait(true);

                if (!_running)
                {
                    await server.DisposeAsync().ConfigureAwait(true);
                    break;
                }

                _ = HandleClientAsync(server); // fire-and-forget per client
                server = null; // ownership passed
            }
            catch
            {
                await server!.DisposeAsync().ConfigureAwait(true);
            }

            if (_running)
            {
                await Task.Delay(50).ConfigureAwait(true); // avoid tight loop if errors
            }
        }
    }

    /// <summary>
    /// Creates a PipeSecurity object that grants full control to the current user and read/write access to all users.
    /// </summary>
    /// <remarks>The returned PipeSecurity allows the current user full control over the pipe, while granting
    /// read and write permissions to all users. This configuration is suitable for scenarios where both the owner and
    /// other processes require access to the named pipe. Ensure that granting access to everyone aligns with your
    /// application's security requirements.</remarks>
    /// <returns>A PipeSecurity instance configured with access rules for the current user and for everyone.</returns>
    private static PipeSecurity CreatePipeSecurity()
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

    /// <summary>
    /// Handles an incoming client connection on the specified named pipe server asynchronously, performing access
    /// checks and managing the connection lifecycle.
    /// </summary>
    /// <remarks>This method enforces access control based on the configured access level and tracks active
    /// connections. It reads messages from the client and invokes the MessageReceived event for each complete message.
    /// The connection is automatically removed and disposed when the client disconnects or an error occurs.</remarks>
    /// <param name="server">The named pipe server stream representing the client connection to handle. Must be connected before calling this
    /// method.</param>
    /// <returns>A task that represents the asynchronous operation of handling the client connection. The task completes when the
    /// connection is closed or disposed.</returns>
    private async Task HandleClientAsync(NamedPipeServerStream server)
    {
        string? exePath = null;
        int pid = -1;

        if (TryGetClientProcess(server, out var proc))
        {
            // Developer build, security hasn't been built yet

/*#if !DEBUG
            switch (_accessLevel)
            {
                case AccessLevel.Everyone:
                    {
                        Debug.WriteLine($"[PipeHost] AccessLevel=Everyone, allowing connection");
                        break;
                    }
                case AccessLevel.CurrentProcess:
                    {
                        pid = proc?.Id ?? -1;

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
                case AccessLevel.ModWhitelist:
                    {
                        exePath = proc?.MainModule?.FileName;

                        if (exePath == null || !WhitelistedPaths.Contains(exePath, StringComparer.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"[PipeHost] Access denied: Executable path not whitelisted: {exePath}");
                            server.Disconnect();
                            server.Dispose();
                            return;
                        }
                        break;
                    }
            }
#endif*/

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
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (_running && server.IsConnected)
            {
                int n = await server.ReadAsync(buffer.AsMemory()).ConfigureAwait(true);

                if (n == 0)
                    break;

                for (int i = 0; i < n; i++)
                {
                    byte b = buffer[i];

                    if (b == (byte)'\n')
                    {
                        var message = sb.ToString();
                        sb.Clear();

                        try
                        {
                            MessageReceived?.Invoke(connection, new(message));
                        }
                        catch { }
                    }
                    else
                    {
                        sb.Append((char)b);
                    }
                }
            }
        }
        catch (IOException)
        {

        }
        finally
        {
            _connections.TryRemove(key, out _);
            connection.Dispose();
        }
    }

    /// <summary>
    /// Attempts to retrieve the process associated with the client connected to the specified named pipe server stream.
    /// </summary>
    /// <remarks>This method does not throw exceptions if the client process cannot be determined. Instead, it
    /// returns false and sets <paramref name="process"/> to <see langword="null"/>. The returned process may represent
    /// a process that has already exited.</remarks>
    /// <param name="pipe">The named pipe server stream representing the connection to the client process.</param>
    /// <param name="process">When this method returns, contains the client process associated with the pipe if successful; otherwise, <see
    /// langword="null"/>.</param>
    /// <returns>true if the client process was successfully retrieved; otherwise, false.</returns>
    private static unsafe bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
    {
        process = null;
        uint pid;
        if (!TerraFX.Interop.Windows.Windows.GetNamedPipeClientProcessId(new((void*)pipe.SafePipeHandle.DangerousGetHandle()), &pid))
        {
            return false;
        }

        try
        {
            process = Process.GetProcessById((int)pid);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sends the specified message to all active connections.
    /// </summary>
    /// <remarks>If an error occurs while sending the message to a connection, the error is silently ignored
    /// and broadcasting continues for other connections.</remarks>
    /// <param name="message">The message to broadcast to all connected clients. Cannot be null.</param>
    public void Broadcast(string message)
    {
        foreach (var connection in _connections.Values)
        {
            try
            {
                connection.WriteMessage(message);
            }
            catch { }
        }
    }

    /// <summary>
    /// Releases all resources used by the instance and its managed connections.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to ensure that all associated
    /// connections are properly disposed and internal resources are freed. After calling <see cref="Dispose"/>, the
    /// instance should not be used further.</remarks>
    public void Dispose()
    {
        StopHost();
        foreach (var kvp in _connections)
        {
            kvp.Value.Dispose();
        }
        _connections.Clear();
    }
}
