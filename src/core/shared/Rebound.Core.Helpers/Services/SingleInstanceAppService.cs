using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

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
    private bool _isFirstInstance;
    private bool _disposed;
    private CancellationTokenSource? _cts;

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
            _cts = new CancellationTokenSource();
            _ = RunPipeServerAsync(_cts.Token); // fire-and-forget
            Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(arguments, true));
        }
        else
        {
            _ = SendArgsToFirstInstanceAsync(arguments);
            Process.GetCurrentProcess().Kill();
        }
    }

    private async Task RunPipeServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.In,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(ct);

                using var sr = new StreamReader(server);
                var args = await sr.ReadLineAsync() ?? string.Empty;

                Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(args, false));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Debug.WriteLine($"Pipe server error: {ex.Message}"); }
        }
    }

    private async Task SendArgsToFirstInstanceAsync(string arguments)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await client.ConnectAsync(3000);

            using var sw = new StreamWriter(client) { AutoFlush = true };
            await sw.WriteLineAsync(arguments);
        }
        catch (Exception ex) { Debug.WriteLine($"Pipe client error: {ex.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        if (_isFirstInstance)
            _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}