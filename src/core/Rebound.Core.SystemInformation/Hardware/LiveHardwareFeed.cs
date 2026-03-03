// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.SystemInformation.Software;

namespace Rebound.Core.SystemInformation.Hardware;

public partial class HardwareFeedUpdateEventArgs(int cpuUsage, int ramUsageBytes, int ramUsagePercent, int gpuUsage, TimeSpan uptime) : EventArgs
{
    public int CpuUsage { get; set; } = cpuUsage;
    public int RamUsageBytes { get; set; } = ramUsageBytes;
    public int RamUsagePercent { get; set; } = ramUsagePercent;
    public int GpuUsage { get; set; } = gpuUsage;
    public TimeSpan Uptime { get; set; } = uptime;
}

/// <summary>
/// Monitors hardware usage in real time and returns values for CPU, GPU, and RAM by checking every <see cref="POLLING_INTERVAL"/> miliseconds.
/// Subscribe to the <see cref="OnUpdate"/> event to retrieve the updated values.
/// This service is to be used on a background thread.
/// </summary>
public partial class LiveHardwareFeed : IDisposable
{
    /// <summary>
    /// Sends the current hardware usage numbers.
    /// </summary>
    public event EventHandler<HardwareFeedUpdateEventArgs>? OnUpdate;

    public int CpuUsage { get; private set; }
    public int RamUsageBytes { get; private set; }
    public int RamUsagePercent { get; private set; }
    public int GpuUsage { get; private set; }
    public TimeSpan Uptime { get; private set; }

    /// <summary>
    /// <see langword="true"/> if the service is running. Otherwise <see langword="false"/>.
    /// </summary>
    public bool IsRunning { get; private set; }

    private bool _disposed;
    private System.Timers.Timer? _timer;

    private const int POLLING_INTERVAL = 500;

    public void Start()
    {
        if (IsRunning || _disposed) return;
        _timer = new System.Timers.Timer(POLLING_INTERVAL) { AutoReset = true };
        _timer.Elapsed += (_, _) => Tick();
        _timer.Start();
        IsRunning = true;
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
    }

    private unsafe void Tick()
    {
        CpuUsage = CPU.GetUsage();
        RamUsagePercent = RAM.GetUsage();
        RamUsageBytes = RAM.GetUsageBytes();
        GpuUsage = GPU.GetUsage();
        Uptime = WindowsInformation.GetUptime();
        OnUpdate?.Invoke(this, new HardwareFeedUpdateEventArgs(CpuUsage, RamUsageBytes, RamUsagePercent, GpuUsage, Uptime));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            Stop();

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LiveHardwareFeed() => Dispose(false);
}