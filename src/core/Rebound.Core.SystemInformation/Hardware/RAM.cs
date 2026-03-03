// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class RAM
{
    /// <returns>
    /// The current RAM usage, in percentage. Otherwise -1.
    /// </returns>
    public static unsafe int GetUsage()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(&memStatus))
            return (int)memStatus.dwMemoryLoad;
        return -1;
    }

    /// <returns>
    /// The current RAM usage, in bytes. Otherwise -1.
    /// </returns>
    public static unsafe int GetUsageBytes()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(&memStatus))
            return (int)((memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024 * 1024));
        return -1;
    }

    /// <returns>
    /// The current pagefile size, in bytes.
    /// </returns>
    public static unsafe long GetPageFileSize()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)sizeof(MEMORYSTATUSEX) };
        return GlobalMemoryStatusEx(&memStatus)
            ? (long)(memStatus.ullTotalPageFile - memStatus.ullTotalPhys)
            : 0;
    }

    /// <returns>
    /// The current pagefile usage, in bytes.
    /// </returns>
    public static unsafe long GetPageFileUsage()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)sizeof(MEMORYSTATUSEX) };
        return GlobalMemoryStatusEx(&memStatus)
            ? (long)(memStatus.ullTotalPageFile - memStatus.ullAvailPageFile)
            : 0;
    }

    /// <returns>
    /// The amount of usable RAM, in bytes.
    /// </returns>
    public static unsafe long GetUsableRam()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)sizeof(MEMORYSTATUSEX) };
        return GlobalMemoryStatusEx(&memStatus) ? (long)memStatus.ullTotalPhys : 0;
    }

    /// <returns>
    /// The amount of installed RAM, in bytes.
    /// </returns>
    public static unsafe long GetInstalledRam()
    {
        ulong kb;
        return GetPhysicallyInstalledSystemMemory(&kb) ? (long)kb * 1024 : 0;
    }
}