// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Rebound.Core.SystemInformation.Hardware;

public static class RAM
{
    public static unsafe string GetTotalRam()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{roundedGb} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static unsafe string GetUsableRAM()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{totalGb:F2} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}