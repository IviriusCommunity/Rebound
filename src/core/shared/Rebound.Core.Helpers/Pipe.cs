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
using Rebound.Forge;
using Windows.Win32;

namespace Rebound.Core.Helpers;

public class TrustedPipeServer
{
    private readonly string _pipeName;
    private readonly ConcurrentDictionary<int, NamedPipeServerStream> _connectedClients = new();

    public TrustedPipeServer(string pipeName)
    {
        _pipeName = pipeName;
    }

    private async Task<bool> IsTrustedClient(string? exePath)
    {
#if DEBUG
        // In debug builds, trust all clients for easier development/testing
        return true;
#else
        if (string.IsNullOrEmpty(exePath))
            return false;

        List<string> trustedPaths = [];

        foreach (var instruction in Catalog.Mods)
        {
            trustedPaths.Add(instruction.EntryExecutable);
        }

        foreach (var trustedPath in trustedPaths)
        {
            if (string.Equals(trustedPath, exePath, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
#endif
    }

    public event Func<string, Task>? MessageReceived;

    public async Task StartAsync()
    {
        while (true)
        {
            var pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone",
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            var pipeServer = NamedPipeServerStreamAcl.Create(
                _pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                0, 0, pipeSecurity);

            await pipeServer.WaitForConnectionAsync();

            if (!TryGetClientProcess(pipeServer, out var clientProcess))
            {
                pipeServer.Dispose();
                continue;
            }

            var exePath = clientProcess?.MainModule?.FileName;
            Debug.WriteLine($"Client connected: {clientProcess?.ProcessName} ({exePath})");

            var isTrusted = await IsTrustedClient(exePath);
            if (!isTrusted)
            {
                Debug.WriteLine("Unauthorized client rejected: " + exePath);
                pipeServer.Dispose();
                continue;
            }

            _ = HandleClientAsync(pipeServer, clientProcess);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, Process? clientProcess)
    {
        var buffer = new byte[4096];
        try
        {
            while (pipe.IsConnected)
            {
                int clientId = clientProcess.Id;
                _connectedClients[clientId] = pipe;

                int bytesRead = await pipe.ReadAsync(buffer);
                if (bytesRead == 0) break;

                var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.WriteLine($"Received from {clientProcess?.ProcessName}: {received}");

                if (MessageReceived != null)
                {
                    await MessageReceived.Invoke(received);
                }

                // Optional: echo back or remove this if not needed
                // byte[] response = Encoding.UTF8.GetBytes(received);
                // await pipe.WriteAsync(response);
                // await pipe.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            int clientId = clientProcess.Id;
            _connectedClients.TryRemove(clientId, out _);
            Debug.WriteLine($"Client {clientProcess?.ProcessName} disconnected with error: {ex.Message}");
        }
        finally
        {
            pipe.Dispose();
        }
    }

    public async Task BroadcastMessageAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var pipe in _connectedClients.Values)
        {
            if (pipe.IsConnected)
            {
                try
                {
                    await pipe.WriteAsync(bytes, 0, bytes.Length);
                    await pipe.FlushAsync();
                }
                catch (IOException)
                {
                    // Handle disconnected client, maybe remove from collection
                }
            }
        }
    }

    private bool TryGetClientProcess(NamedPipeServerStream pipe, out Process? process)
    {
        process = null;
        if (pipe.SafePipeHandle.IsInvalid)
            return false;

        if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint pid))
            return false;

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
}

public class ReboundPipeClient
{
    private NamedPipeClientStream? _pipeClient;
    private readonly byte[] _buffer = new byte[1024];
    private bool _listening;
    private Func<string, Task>? _onMessageReceived;
    private CancellationTokenSource? _cts;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _pipeClient = new NamedPipeClientStream(".", "REBOUND_SERVICE_HOST", PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string message)
    {
        if (_pipeClient?.IsConnected == true)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await _pipeClient.WriteAsync(messageBytes).ConfigureAwait(false);
            await _pipeClient.FlushAsync().ConfigureAwait(false);
        }
    }

    public void StartListening(Func<string, Task> onMessageReceived)
    {
        if (_listening) return;

        _listening = true;
        _onMessageReceived = onMessageReceived;
        _cts = new CancellationTokenSource();

        _ = Task.Run(() => ListenLoopAsync(_cts.Token));
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                try
                {
                    await ConnectAsync(cancellationToken).ConfigureAwait(false);
                    Console.WriteLine("Pipe connected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to connect pipe: " + ex.Message);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    continue;
                }
            }

            try
            {
                while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _pipeClient.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        // Server closed pipe, reconnect
                        break;
                    }

                    var received = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                    if (_onMessageReceived != null)
                        await _onMessageReceived(received).ConfigureAwait(false);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Pipe disconnected or error: " + ex.Message);
                // On IOException, break to reconnect
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, exit loop
                break;
            }

            // Cleanup and reconnect loop
            _pipeClient?.Dispose();
            _pipeClient = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Reconnecting pipe in 1s...");
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        _listening = false;
    }

    public void Close()
    {
        _cts?.Cancel();
        _pipeClient?.Dispose();
        _pipeClient = null;
        _listening = false;
    }
}