// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.IO.Pipes;
using System.Text;

namespace Rebound.Core.IPC;

public interface IPipeClient
{
    event Action<string>? MessageReceived;
    Task ConnectAsync();
    Task SendAsync(string message);
    void Stop();
}

public class PipeClient : IPipeClient, IDisposable
{
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipe;
    private readonly object _sendLock = new();
    private volatile bool _running;

    /// <summary>
    /// Occurs when a new message is received, providing the message content as a string.
    /// </summary>
    /// <remarks>Subscribers can handle this event to process incoming messages. The event argument contains
    /// the full message text. If no handlers are attached, messages will be ignored.</remarks>
    public event Action<string>? MessageReceived;

    /// <summary>
    /// Initializes a new instance of the PipeClient class using the specified named pipe.
    /// </summary>
    /// <param name="pipeName">The name of the pipe to connect to. If not specified, defaults to "REBOUND_SERVICE_HOST".</param>
    public PipeClient(string pipeName = "REBOUND_SERVICE_HOST")
    {
        _pipeName = pipeName;
    }

    /// <summary>
    /// Stops the current operation and releases associated resources.
    /// </summary>
    /// <remarks>Calling this method will terminate any ongoing activity and dispose of internal resources.
    /// After calling <see cref="Stop"/>, the instance cannot be restarted unless explicitly supported by the class.
    /// This method is safe to call multiple times; subsequent calls have no effect if the operation is already
    /// stopped.</remarks>
    public void Stop()
    {
        _running = false;
        try { _pipe?.Dispose(); } catch (Exception ex) { }
    }

    /// <summary>
    /// Asynchronously establishes a connection to the named pipe server if not already connected.
    /// </summary>
    /// <remarks>If a connection is already established, the method returns immediately without attempting to
    /// reconnect. Upon successful connection, background threads are started to maintain the connection and handle
    /// incoming data. This method is not thread-safe; concurrent calls may result in undefined behavior.</remarks>
    /// <returns>A task that represents the asynchronous connect operation.</returns>
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

    /// <summary>
    /// Continuously reads messages from the connected pipe asynchronously until the connection is closed or the loop is
    /// stopped.
    /// </summary>
    /// <remarks>Each message is delimited by a newline character and is dispatched via the MessageReceived
    /// event when received. The method disposes the pipe upon completion or error. This method does not throw
    /// exceptions for read errors; errors are handled internally.</remarks>
    /// <returns>A task that represents the asynchronous read loop operation.</returns>
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

    /// <summary>
    /// Ensures that the client is connected to the named pipe asynchronously. If the client is not already connected,
    /// attempts to establish a connection.
    /// </summary>
    /// <remarks>If the client is already connected, the method returns immediately. If the connection cannot
    /// be established within the running state, an exception is thrown. This method should be called before performing
    /// operations that require an active pipe connection.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task completes when the client is connected to the named
    /// pipe.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the client has been stopped before a connection could be established.</exception>
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

    /// <summary>
    /// Asynchronously sends a text message to the connected pipe client.
    /// </summary>
    /// <remarks>Each message is encoded as UTF-8 and terminated with a newline character before being sent.
    /// The method must be called only when the client is running and connected.</remarks>
    /// <param name="message">The text message to send. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the client has not been started or if the pipe is not connected.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
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

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to ensure that all associated
    /// resources are properly released. After calling <see cref="Dispose"/>, the instance should not be used.</remarks>
    public void Dispose()
    {
        Stop();
        _pipe?.Dispose();
    }
}
