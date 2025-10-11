using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Rebound.Core.Helpers.Services;

public class SingleInstanceLaunchEventArgs : EventArgs
{
    public string Arguments { get; }
    public bool IsFirstLaunch { get; }

    public SingleInstanceLaunchEventArgs(string arguments, bool isFirstLaunch)
    {
        Arguments = arguments;
        IsFirstLaunch = isFirstLaunch;
    }
}

public partial class SingleInstanceAppService : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private NamedPipeServerStream? _serverStream;
    private bool _isFirstInstance;
    private bool _disposed;

    public event EventHandler<SingleInstanceLaunchEventArgs>? Launched;

    public SingleInstanceAppService(string appId)
    {
        _mutexName = $"MUTEX_{appId}";
        _pipeName = $"PIPE_{appId}";
    }

    public void Launch(string arguments)
    {
        if (_mutex == null)
            _mutex = new Mutex(true, _mutexName, out _isFirstInstance);

        if (_isFirstInstance)
        {
            StartPipeServer();
            Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(arguments, true));
        }
        else
        {
            SendArgsToFirstInstance(arguments);
            Process.GetCurrentProcess().Kill();
        }
    }

    private void StartPipeServer()
    {
        _serverStream = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1,
            PipeTransmissionMode.Message, PipeOptions.Asynchronous);

        _serverStream.BeginWaitForConnection(OnPipeConnected, _serverStream);
    }

    private void OnPipeConnected(IAsyncResult ar)
    {
        var server = (NamedPipeServerStream)ar.AsyncState!;
        try
        {
            server.EndWaitForConnection(ar);

            string args;
            using (var sr = new StreamReader(server))
            {
                args = sr.ReadToEnd();
            }

            Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(args, false));
        }
        catch { /* ignore errors */ }
        finally
        {
            server.Dispose();
            StartPipeServer(); // keep server ready for next instance
        }
    }

    private void SendArgsToFirstInstance(string arguments)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(3000); // 3s timeout
            using var sw = new StreamWriter(client);
            sw.Write(arguments);
            sw.Flush();
        }
        catch { /* ignore errors */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _serverStream?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}