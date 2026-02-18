// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public partial class HardwareFeedUpdateEventArgs : EventArgs
{
    public HardwareFeedUpdateEventArgs(int cpuUsage, int ramUsageMB, int ramUsagePercent, int gpuUsage)
    {
        CPUUsage = cpuUsage;
        RAMUsageMB = ramUsageMB;
        RAMUsagePercent = ramUsagePercent;
        GPUUsage = gpuUsage;
    }

    public int CPUUsage { get; set; }
    public int RAMUsageMB { get; set; }
    public int RAMUsagePercent { get; set; }
    public int GPUUsage { get; set; }
}

public partial class LiveHardwareFeed : IDisposable
{
    public event EventHandler<HardwareFeedUpdateEventArgs>? OnUpdate;

    public int CPUUsage { get; private set; }
    public int RAMUsageMB { get; private set; }
    public int RAMUsagePercent { get; private set; }
    public int GPUUsage { get; private set; }
    public bool IsRunning { get; private set; }

    private System.Timers.Timer? _updateTimer;
    private readonly object _lock = new();
    private bool _disposed;

    // For CPU usage tracking
    private FILETIME _prevIdleTime;
    private FILETIME _prevKernelTime;
    private FILETIME _prevUserTime;
    private bool _firstCpuRead = true;

    // COM Security constants (not in TerraFX)
    private const uint RPC_C_AUTHN_WINNT = 10;
    private const uint RPC_C_AUTHZ_NONE = 0;
    private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
    private const uint EOAC_NONE = 0;

    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning || _disposed)
                return;

            _updateTimer = new System.Timers.Timer(1000); // Update every second
            _updateTimer.Elapsed += UpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            IsRunning = true;

            // Prime the CPU counter
            _firstCpuRead = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRunning)
                return;

            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _updateTimer = null;
            IsRunning = false;
        }
    }

    private unsafe void UpdateTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            // Update CPU usage
            CPUUsage = GetCPUUsage();

            // Update RAM usage
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(&memStatus))
            {
                RAMUsagePercent = (int)memStatus.dwMemoryLoad;
                var usedMemoryBytes = memStatus.ullTotalPhys - memStatus.ullAvailPhys;
                RAMUsageMB = (int)(usedMemoryBytes / (1024 * 1024));
            }

            // Update GPU usage
            GPUUsage = GetGPUUsage();

            // Raise event
            OnUpdate?.Invoke(this, new HardwareFeedUpdateEventArgs(CPUUsage, RAMUsageMB, RAMUsagePercent, GPUUsage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating hardware feed: {ex.Message}");
        }
    }

    private unsafe int GetCPUUsage()
    {
        try
        {
            FILETIME idleTime;
            FILETIME kernelTime;
            FILETIME userTime;
            if (!GetSystemTimes(&idleTime, &kernelTime, &userTime))
                return 0;

            if (_firstCpuRead)
            {
                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;
                _firstCpuRead = false;
                return 0;
            }

            // Convert FILETIME to ulong for proper arithmetic
            ulong idle = FileTimeToUlong(idleTime) - FileTimeToUlong(_prevIdleTime);
            ulong kernel = FileTimeToUlong(kernelTime) - FileTimeToUlong(_prevKernelTime);
            ulong user = FileTimeToUlong(userTime) - FileTimeToUlong(_prevUserTime);

            ulong total = kernel + user;

            if (total == 0)
                return 0;

            int cpuUsage = (int)((total - idle) * 100 / total);

            _prevIdleTime = idleTime;
            _prevKernelTime = kernelTime;
            _prevUserTime = userTime;

            return Math.Clamp(cpuUsage, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    private static ulong FileTimeToUlong(FILETIME ft)
    {
        return ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
    }

    private unsafe int GetGPUUsage()
    {
        HRESULT hr;
        int result = 0;
        ComPtr<IWbemLocator> pLocator = default;
        ComPtr<IWbemServices> pServices = default;
        ComPtr<IEnumWbemClassObject> pEnumerator = default;

        try
        {
            var clsid = CLSID.CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;

            hr = CoCreateInstance(&clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)pLocator.GetAddressOf());
            if (FAILED(hr))
                return 0;

            fixed (char* pNamespace = "ROOT\\CIMV2")
            {
                hr = pLocator.Get()->ConnectServer(pNamespace, null, null, null, 0, null, null, pServices.GetAddressOf());
            }
            if (FAILED(hr))
                return 0;

            hr = CoSetProxyBlanket((IUnknown*)pServices.Get(), RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                                   RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, null, EOAC_NONE);
            if (FAILED(hr))
                return 0;

            // Query GPU performance counters
            fixed (char* pLanguage = "WQL")
            fixed (char* pQuery = "SELECT Name, UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine WHERE Name LIKE '%engtype_3D%'")
            {
                hr = pServices.Get()->ExecQuery(pLanguage, pQuery,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY | WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null, pEnumerator.GetAddressOf());
            }
            if (FAILED(hr))
                return 0;

            IWbemClassObject* pObj = null;
            uint returned = 0;
            double maxUsage = 0;

            fixed (char* pProperty = "UtilizationPercentage")
            {
                while (pEnumerator.Get()->Next((int)WBEM_TIMEOUT_TYPE.WBEM_INFINITE, 1, &pObj, &returned) == S.S_OK)
                {
                    VARIANT vtUsage;
                    VariantInit(&vtUsage);
                    pObj->Get(pProperty, 0, &vtUsage, null, null);

                    if (vtUsage.vt == (ushort)VARENUM.VT_BSTR)
                    {
                        var bstr = vtUsage.Anonymous.Anonymous.Anonymous.bstrVal;
                        if (bstr != null)
                        {
                            var usageStr = new string(bstr);
                            if (double.TryParse(usageStr, out double usageVal))
                            {
                                if (usageVal > maxUsage)
                                    maxUsage = usageVal;
                            }
                        }
                    }
                    else if (vtUsage.vt == (ushort)VARENUM.VT_R8)
                    {
                        double usage = vtUsage.Anonymous.Anonymous.Anonymous.dblVal;
                        if (usage > maxUsage)
                            maxUsage = usage;
                    }
                    else if (vtUsage.vt == (ushort)VARENUM.VT_R4)
                    {
                        double usage = vtUsage.Anonymous.Anonymous.Anonymous.fltVal;
                        if (usage > maxUsage)
                            maxUsage = usage;
                    }

                    VariantClear(&vtUsage);
                    pObj->Release();
                }
            }

            Debug.WriteLine(maxUsage);
            result = (int)Math.Clamp(maxUsage, 0, 100);
        }
        catch
        {
            result = 0;
        }
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Dispose();
            if (pServices.Get() is not null) pServices.Dispose();
            if (pLocator.Get() is not null) pLocator.Dispose();
        }

        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Stop();
        }

        _disposed = true;
    }

    ~LiveHardwareFeed()
    {
        Dispose(false);
    }
}