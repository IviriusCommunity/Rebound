// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Windows.Win32;

namespace Rebound.Core.IPC;

public interface IPipeHost
{
    event Action<PipeConnection, string>? MessageReceived;
    Task StartAsync();
    void Stop();
    void Broadcast(string message);
}

public class PipeHost : IPipeHost, IDisposable
{
    private readonly string _pipeName;
    private readonly AccessLevel _accessLevel;
    private readonly ConcurrentDictionary<string, PipeConnection> _connections = new();
    private readonly ConcurrentBag<NamedPipeServerStream> _listeners = new();
    private volatile bool _running;
    private int _connectionCounter;

    /// <summary>
    /// Occurs when a message is received from the pipe connection.
    /// </summary>
    /// <remarks>The event provides the associated <see cref="PipeConnection"/> and the received message as a
    /// string. Subscribers can use this event to process incoming messages in real time. This event is typically raised
    /// on the thread that receives the message; ensure thread safety when handling the event if accessing shared
    /// resources.</remarks>
    public event Action<PipeConnection, string>? MessageReceived;

    /// <summary>
    /// Gets the collection of file system paths that are permitted for access or operations.
    /// </summary>
    /// <remarks>The returned list contains absolute or relative paths that are considered safe or allowed by
    /// the application. Modifying the contents of this list affects which paths are treated as whitelisted. This
    /// property is read-only; to change the set of whitelisted paths, modify the list directly.</remarks>
    public List<string> WhitelistedPaths { get; } = [];

    /// <summary>
    /// Initializes a new instance of the PipeHost class with the specified pipe name and access level.
    /// </summary>
    /// <param name="pipeName">The name of the pipe to host. This value cannot be null.</param>
    /// <param name="accessLevel">The access level that determines which clients are permitted to connect. Defaults to ModWhitelist if not
    /// specified.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pipeName"/> is null.</exception>
    public PipeHost(string pipeName, AccessLevel accessLevel = AccessLevel.ModWhitelist)
    {
        _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        _accessLevel = accessLevel;
    }

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
        await AcceptLoopAsync(); // fire-and-forget
    }

    /// <summary>
    /// Stops the current operation and releases all associated resources.
    /// </summary>
    /// <remarks>After calling this method, the instance will no longer process events or notify listeners.
    /// This method disposes all registered listeners and suppresses any exceptions thrown during disposal. It is safe
    /// to call this method multiple times; subsequent calls have no effect.</remarks>
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

    /// <summary>
    /// Continuously accepts incoming client connections on the named pipe server while the server is running.
    /// </summary>
    /// <remarks>This method listens for new client connections and initiates handling for each client in a
    /// separate asynchronous operation. The loop continues until the server is stopped. If an error occurs while
    /// accepting a connection, the method waits briefly before retrying to avoid a tight error loop.</remarks>
    /// <returns>A task that represents the asynchronous accept loop operation.</returns>
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

    /// <summary>
    /// Creates a PipeSecurity object that grants full control to the current user and read/write access to all users.
    /// </summary>
    /// <remarks>The returned PipeSecurity allows the current user full control over the pipe, while granting
    /// read and write permissions to all users. This configuration is suitable for scenarios where both the owner and
    /// other processes require access to the named pipe. Ensure that granting access to everyone aligns with your
    /// application's security requirements.</remarks>
    /// <returns>A PipeSecurity instance configured with access rules for the current user and for everyone.</returns>
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

    /// <summary>
    /// Sends the specified message to all active connections.
    /// </summary>
    /// <remarks>If an error occurs while sending the message to a connection, the error is silently ignored
    /// and broadcasting continues for other connections.</remarks>
    /// <param name="message">The message to broadcast to all connected clients. Cannot be null.</param>
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

    /// <summary>
    /// Releases all resources used by the instance and its managed connections.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to ensure that all associated
    /// connections are properly disposed and internal resources are freed. After calling <see cref="Dispose"/>, the
    /// instance should not be used further.</remarks>
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
