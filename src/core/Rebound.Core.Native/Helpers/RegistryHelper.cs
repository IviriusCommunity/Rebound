// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native.Helpers;

public static class RegistryHelper
{
    public static unsafe string GetString(int HKEYType, string subKey, string ValueName)
    {
        var hKeyRoot = HKEYType switch
        {
            0 => HKEY.HKEY_CLASSES_ROOT,
            1 => HKEY.HKEY_CURRENT_USER,
            2 => HKEY.HKEY_LOCAL_MACHINE,
            3 => HKEY.HKEY_USERS,
            4 => HKEY.HKEY_CURRENT_CONFIG,
            _ => HKEY.HKEY_LOCAL_MACHINE, // using HKEY_LOCAL_MACHINE as default case as that's where most important registry values are stored
        };

        using ManagedPtr<char> lpSubKey = subKey;
        using ManagedPtr<char> lpValueName = ValueName;
        HKEY hKey = default;
        var status = RegOpenKeyExW(hKeyRoot, lpSubKey, 0, KEY.KEY_READ, &hKey);
        if (status != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS)
            return null;
        try
        {
            uint type = 0;
            uint cbData = 0;

            // Query first for buffer size requirements
            status = RegQueryValueExW(hKey, lpValueName, null, &type, null, &cbData);

            if (status == TerraFX.Interop.Windows.ERROR.ERROR_FILE_NOT_FOUND)
                return null;
            if (status is not TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS and not TerraFX.Interop.Windows.ERROR.ERROR_MORE_DATA)
                return null;

            // Allocate buffer on native heap (cbData is in bytes)
            byte* pBuffer = (byte*)NativeMemory.Alloc(cbData);
            try
            {
                status = RegQueryValueExW(hKey, lpValueName, null, &type, pBuffer, &cbData);
                if (status != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS) return null;

                // Marshal string depending on data type (REG_SZ or REG_EXPAND_SZ)
                return new string((char*)pBuffer, 0, (int)(cbData / sizeof(char))).TrimEnd('\0');
            }
            finally
            {
                NativeMemory.Free(pBuffer);
            }
        }
        finally
        {
            _ = RegCloseKey(hKey);
        }
    }
}
