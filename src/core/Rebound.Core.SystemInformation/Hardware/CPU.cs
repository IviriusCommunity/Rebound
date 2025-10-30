// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Rebound.Core.SystemInformation.Hardware;

public static class CPU
{
    public static string GetCPUName()
    {
        return Normalizer.NormalizeTrademarkSymbols((Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString()!) ?? "Unknown";
    }
}
