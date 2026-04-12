// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Windows;
using Rebound.Core.Native.Wrappers;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class GPU
{
    /// <returns>
    /// The current GPU usage, in percentage.
    /// </returns>
    public static unsafe int GetUsage()
    {
        double maxUsage = 0;

        WmiConnection.Shared.ExecuteWmiQuery(
            "SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine WHERE Name LIKE '%engtype_3D%'",
            ptr =>
            {
                var usage = WmiConnection.GetDouble((IWbemClassObject*)ptr, "UtilizationPercentage");
                if (usage > maxUsage) maxUsage = usage;
            });

        return (int)Math.Clamp(maxUsage, 0, 100);
    }

    /// <summary>
    /// Queries the current GPU name via registry.
    /// </summary>
    /// <returns>
    /// The current GPU name. If none, Unknown.
    /// </returns>
    public static unsafe string GetName()
    {
        using ComPtr<IDXGIFactory> factory = default;
        using ComPtr<IDXGIAdapter> adapter = default;
        using ManagedPtr<Guid> iid = IID.IID_IDXGIFactory;

        DirectX.CreateDXGIFactory(iid, (void**)&factory);
        factory.Get()->EnumAdapters(0, adapter.GetAddressOf());
        DXGI_ADAPTER_DESC desc;
        adapter.Get()->GetDesc(&desc);
        return Normalizer.NormalizeTrademarkSymbols(new string(&desc.Description.e0));
    }
}