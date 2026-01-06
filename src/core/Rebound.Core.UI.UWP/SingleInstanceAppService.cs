// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Rebound.Core.Helpers;
using Rebound.Core.IPC;

namespace Rebound.Core.UI
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
        }

        public void Launch(string arguments)
        {
            if (_mutex == null)
            {
                try
                {
                    _mutex = new Mutex(true, _mutexName, out _isFirstInstance);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            if (_isFirstInstance)
            {
                // First instance: start pipe server
                _cts = new CancellationTokenSource();
                StartPipeServer(_cts.Token);

                try
                {
                    Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(arguments, true));
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                // Not first instance: send args and exit
                SendArgsToFirstInstanceAndExit(arguments);
            }
        }

        private void StartPipeServer(CancellationToken ct)
        {
            try
            {
                _server = new PipeHost(_pipeName, AccessLevel.Everyone);

                _server.MessageReceived += (connection, message) =>
                {
                    try
                    {
                        Launched?.Invoke(this, new SingleInstanceLaunchEventArgs(message, false));

                        // Send acknowledgment back to the client
                        _server.Broadcast("ACK");
                    }
                    catch (Exception ex)
                    {

                    }
                };

                // Fire-and-forget async loop
                Task.Run(async () =>
                {
                    try
                    {
                        await _server.StartAsync();

                        // Wait until cancellation
                        while (!ct.IsCancellationRequested)
                            await Task.Delay(100, ct);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                }, ct);
            }
            catch (Exception ex)
            {

            }
        }

        private async void SendArgsToFirstInstanceAndExit(string arguments)
        {
            try
            {
                using var client = new PipeClient(_pipeName);

                var ackReceived = false;
                var ackTimeout = new CancellationTokenSource(5000); // 5 second timeout

                client.MessageReceived += (msg) =>
                {
                    if (msg == "ACK")
                    {
                        ackReceived = true;
                    }
                };

                await client.ConnectAsync();

                await client.SendAsync(arguments);

                // Wait for acknowledgment
                while (!ackReceived && !ackTimeout.Token.IsCancellationRequested)
                {
                    await Task.Delay(10, ackTimeout.Token);
                }

                if (ackReceived)
                {

                }
                else
                {

                }
            }
            catch (Exception ex)
            {
            }

            // Exit safely
            var currentPid = Process.GetCurrentProcess().Id;
            Process.GetCurrentProcess().Kill();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();

            _server?.Dispose();

            if (_isFirstInstance)
            {
                try
                {
                    _mutex?.ReleaseMutex();
                }
                catch (Exception ex)
                {

                }
            }

            _mutex?.Dispose();
        }
    }
}