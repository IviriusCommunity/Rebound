// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class CPU
{
    private static bool _firstCpuRead = true;
    private static FILETIME _prevIdleTime, _prevKernelTime, _prevUserTime;

    /// <summary>
    /// Retrieves the CPU architecture of the current process using RuntimeInformation.ProcessArchitecture 
    /// and maps it to a human-readable string.
    /// </summary>
    /// <returns>
    /// A string representing the CPU architecture of the current process. Possible values include "x64", "x86",
    /// "ARM64", "ARM", "WASM", "S390x", "LoongArch64", "ARMv6", "PPC64LE", or "Unknown" if the architecture cannot be determined.
    /// </returns>
    public static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "ARM64",
            Architecture.Arm => "ARM",
            Architecture.Wasm => "WASM",
            Architecture.S390x => "S390x",
            Architecture.LoongArch64 => "LoongArch64",
            Architecture.Armv6 => "ARMv6",
            Architecture.Ppc64le => "PPC64LE",
            _ => "Unknown"
        };
    }

    /// <returns>
    /// The current CPU usage, in percentage.
    /// </returns>
    public static unsafe int GetUsage()
    {
        FILETIME idle, kernel, user;
        if (!GetSystemTimes(&idle, &kernel, &user)) return 0;

        if (_firstCpuRead)
        {
            (_prevIdleTime, _prevKernelTime, _prevUserTime) = (idle, kernel, user);
            _firstCpuRead = false;
            return 0;
        }

        ulong idleDelta = NativeMethods.FileTimeToUlong(idle) - NativeMethods.FileTimeToUlong(_prevIdleTime);
        ulong kernelDelta = NativeMethods.FileTimeToUlong(kernel) - NativeMethods.FileTimeToUlong(_prevKernelTime);
        ulong userDelta = NativeMethods.FileTimeToUlong(user) - NativeMethods.FileTimeToUlong(_prevUserTime);
        ulong total = kernelDelta + userDelta;

        (_prevIdleTime, _prevKernelTime, _prevUserTime) = (idle, kernel, user);

        return total == 0 ? 0 : Math.Clamp((int)((total - idleDelta) * 100 / total), 0, 100);
    }

    /// <returns>
    /// The current CPU name. If none, Unknown.
    /// </returns>
    public static string GetName()
    {
        return Normalizer.NormalizeTrademarkSymbols((Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "Unknown") ?? "Unknown").ToString()!) ?? "Unknown";
    }
}