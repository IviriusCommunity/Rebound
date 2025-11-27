using System.IO.Pipes;
using System.Text;

namespace Rebound.Core.IPC;

public enum AccessLevel
{
    CurrentProcess,
    ModWhitelist,
    Everyone
}

public interface IPipeConnection
{
    string? ExecutablePath { get; }
    void WriteMessage(string message);
    string? ReadMessage();
}

public class PipeConnection : IPipeConnection, IDisposable
{
    internal NamedPipeServerStream Stream { get; }
    internal int ProcessId { get; }

    private readonly object _writeLock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the full file system path to the executable associated with the current process.
    /// </summary>
    public string? ExecutablePath { get; }

    /// <summary>
    /// Initializes a new instance of the PipeConnection class using the specified named pipe stream, process
    /// identifier, and executable path.
    /// </summary>
    /// <param name="stream">The NamedPipeServerStream instance that provides the underlying pipe connection. Cannot be null.</param>
    /// <param name="pid">The process identifier (PID) associated with the connected client.</param>
    /// <param name="exePath">The file system path to the executable of the connected process, or null if not available.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is null.</exception>
    internal PipeConnection(NamedPipeServerStream stream, int pid, string? exePath)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        ProcessId = pid;
        ExecutablePath = exePath;
    }

    /// <summary>
    /// Writes the specified message to the underlying stream, appending a newline character at the end.
    /// </summary>
    /// <remarks>This method is thread-safe and ensures that messages are written atomically. The message is
    /// encoded using UTF-8 and terminated with a newline character, which may be required by some protocols or
    /// consumers.</remarks>
    /// <param name="message">The message to write to the stream. Cannot be null.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the connection has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
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

    /// <summary>
    /// Reads a message from the underlying stream, returning the message as a string. The message is expected to be
    /// terminated by a newline character ('\n').
    /// </summary>
    /// <remarks>The returned message does not include the terminating newline character. If the stream is
    /// disconnected or an I/O error occurs during reading, the method returns <see langword="null"/> instead of
    /// throwing an exception.</remarks>
    /// <returns>A string containing the message read from the stream, or <see langword="null"/> if the connection is
    /// disconnected or an I/O error occurs.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the connection has been disposed.</exception>
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

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources and
    /// perform other cleanup operations. After calling <see cref="Dispose"/>, the object should not be used.</remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { Stream.Dispose(); } catch { }
    }
}