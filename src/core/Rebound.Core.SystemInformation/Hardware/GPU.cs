// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Rebound.Core.SystemInformation.Hardware;

public static class GPU
{
    public static string GetGPUName()
    {
        string? gpuName = null;

        using (var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video"))
        {
            foreach (var subKeyName in videoKey?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var subKey = videoKey?.OpenSubKey($@"{subKeyName}\0000");
                var name = subKey?.GetValue("DriverDesc")?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    gpuName = Normalizer.NormalizeTrademarkSymbols(name);
                    break;
                }
            }
        }

        return gpuName ?? "Unknown";
    }
}