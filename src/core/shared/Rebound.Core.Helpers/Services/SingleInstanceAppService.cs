using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Rebound.Core.Helpers;

namespace Rebound.Core.Helpers.Services
{
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
        private PipeHost? _server;
        private CancellationTokenSource? _cts;

        public event EventHandler<SingleInstanceLaunchEventArgs>? Launched;

        public SingleInstanceAppService(string appId)
        {
            _mutexName = $"MUTEX_{appId}";
            _pipeName = $"PIPE_{appId}";
            Debug.WriteLine($"[SingleInstance] Created with AppId={appId}");
            Debug.WriteLine($"[SingleInstance] MutexName={_mutexName}");
            Debug.WriteLine($"[SingleInstance] PipeName={_pipeName}");
        }

        public void Launch(string arguments)
        {
            Debug.WriteLine($"[SingleInstance] Launch called with arguments: {arguments}");

            if (_mutex == null)
            {
                Debug.WriteLine($"[SingleInstance] Creating mutex '{_mutexName}'");
                try
                {
                    _mutex = new Mutex(true, _mutexName, out _isFirstInstance);
                    Debug.WriteLine($"[SingleInstance] Mutex created, IsFirstInstance={_isFirstInstance}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SingleInstance] ERROR creating mutex: {ex.Message}");
                    throw;
                }
            }

            if (_isFirstInstance)
            {
                Debug.WriteLine($"[SingleInstance] This is the FIRST instance");
                // First instance: start pipe server
                _cts = new CancellationTokenSource();
                Debug.WriteLine($"[SingleInstance] Starting pipe server...");
                StartPipeServer(_cts.Token);

                Debug.WriteLine($"[SingleInstance] Invoking Launched event (IsFirstLaunch=true)");
                try
                {
                    Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(arguments, true));
                    Debug.WriteLine($"[SingleInstance] Launched event invoked successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SingleInstance] ERROR in Launched event handler: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[SingleInstance] This is NOT the first instance");
                // Not first instance: send args and exit
                Debug.WriteLine($"[SingleInstance] Sending arguments to first instance and exiting...");
                SendArgsToFirstInstanceAndExit(arguments);
            }
        }

        private void StartPipeServer(CancellationToken ct)
        {
            Debug.WriteLine($"[SingleInstance] StartPipeServer called");
            try
            {
                _server = new PipeHost(_pipeName, AccessLevel.Everyone);
                Debug.WriteLine($"[SingleInstance] PipeHost created");

                _server.MessageReceived += (connection, message) =>
                {
                    Debug.WriteLine($"[SingleInstance] MessageReceived from PID={connection.ProcessId}: {message}");

                    try
                    {
                        Debug.WriteLine($"[SingleInstance] Invoking Launched event (IsFirstLaunch=false)");
                        Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(message, false));
                        Debug.WriteLine($"[SingleInstance] Launched event invoked successfully");

                        // Send acknowledgment back to the client
                        Debug.WriteLine($"[SingleInstance] Sending ACK to client");
                        _server.Send(connection, "ACK");
                        Debug.WriteLine($"[SingleInstance] ACK sent successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SingleInstance] ERROR in Launched event handler: {ex.Message}");
                    }
                };

                Debug.WriteLine($"[SingleInstance] MessageReceived event handler attached");

                // Fire-and-forget async loop
                Task.Run(async () =>
                {
                    Debug.WriteLine($"[SingleInstance] Pipe server task starting...");
                    try
                    {
                        await _server.StartAsync();
                        Debug.WriteLine($"[SingleInstance] Server StartAsync completed, entering wait loop");

                        // Wait until cancellation
                        while (!ct.IsCancellationRequested)
                            await Task.Delay(100, ct);

                        Debug.WriteLine($"[SingleInstance] Pipe server task cancelled");
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"[SingleInstance] Pipe server task cancelled (OperationCanceledException)");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SingleInstance] Pipe server error: {ex.GetType().Name}: {ex.Message}");
                        Debug.WriteLine($"[SingleInstance] StackTrace: {ex.StackTrace}");
                    }
                }, ct);

                Debug.WriteLine($"[SingleInstance] Pipe server task launched");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SingleInstance] ERROR in StartPipeServer: {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine($"[SingleInstance] StackTrace: {ex.StackTrace}");
            }
        }

        private async void SendArgsToFirstInstanceAndExit(string arguments)
        {
            Debug.WriteLine($"[SingleInstance] SendArgsToFirstInstanceAndExit called");
            try
            {
                Debug.WriteLine($"[SingleInstance] Creating PipeClient for '{_pipeName}'");
                using var client = new PipeClient(_pipeName);

                var ackReceived = false;
                var ackTimeout = new CancellationTokenSource(5000); // 5 second timeout

                client.MessageReceived += (msg) =>
                {
                    Debug.WriteLine($"[SingleInstance] Received response: {msg}");
                    if (msg == "ACK")
                    {
                        Debug.WriteLine($"[SingleInstance] ACK received!");
                        ackReceived = true;
                    }
                };

                Debug.WriteLine($"[SingleInstance] Connecting to first instance...");
                await client.ConnectAsync();
                Debug.WriteLine($"[SingleInstance] Connected! Sending arguments...");

                await client.SendAsync(arguments);
                Debug.WriteLine($"[SingleInstance] Arguments sent, waiting for ACK...");

                // Wait for acknowledgment
                while (!ackReceived && !ackTimeout.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, ackTimeout.Token);
                }

                if (ackReceived)
                {
                    Debug.WriteLine($"[SingleInstance] Message acknowledged by first instance");
                }
                else
                {
                    Debug.WriteLine($"[SingleInstance] WARNING: ACK timeout - proceeding anyway");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SingleInstance] Pipe client error: {ex.GetType().Name}: {ex.Message}");
                Debug.WriteLine($"[SingleInstance] StackTrace: {ex.StackTrace}");
            }

            // Exit safely
            Debug.WriteLine($"[SingleInstance] Killing current process...");
            var currentPid = Process.GetCurrentProcess().Id;
            Debug.WriteLine($"[SingleInstance] Current PID: {currentPid}");
            Process.GetCurrentProcess().Kill();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                Debug.WriteLine($"[SingleInstance] Already disposed, skipping");
                return;
            }

            Debug.WriteLine($"[SingleInstance] Disposing SingleInstanceAppService");
            _disposed = true;

            Debug.WriteLine($"[SingleInstance] Cancelling pipe server...");
            _cts?.Cancel();
            _cts?.Dispose();

            Debug.WriteLine($"[SingleInstance] Disposing pipe server...");
            _server?.Dispose();

            if (_isFirstInstance)
            {
                Debug.WriteLine($"[SingleInstance] Releasing mutex (first instance)");
                try
                {
                    _mutex?.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SingleInstance] ERROR releasing mutex: {ex.Message}");
                }
            }

            Debug.WriteLine($"[SingleInstance] Disposing mutex");
            _mutex?.Dispose();

            Debug.WriteLine($"[SingleInstance] SingleInstanceAppService disposed");
        }
    }
}