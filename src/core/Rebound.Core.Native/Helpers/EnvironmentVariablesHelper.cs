// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native.Helpers;

public static class EnvironmentVariablesHelper
{
    private const string UserSubKey = "Environment";
    private const string SystemSubKey = @"System\CurrentControlSet\Control\Session Manager\Environment";

    public enum EnvironmentScope
    {
        User,
        System
    }

    /// <summary>
    /// Gets a persistent environment variable from the specified scope.
    /// </summary>
    public static unsafe string? GetVariable(string name, EnvironmentScope scope)
    {
        HKEY hKeyRoot = scope == EnvironmentScope.User ? HKEY.HKEY_CURRENT_USER : HKEY.HKEY_LOCAL_MACHINE;
        string subKey = scope == EnvironmentScope.User ? UserSubKey : SystemSubKey;

        using ManagedPtr<char> lpSubKey = subKey;
        using ManagedPtr<char> lpValueName = name;

        HKEY hKey = default;

        // Open the registry key (we all love it when some functions in Windows don't return HRESULT)
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

    /// <summary>
    /// Enumerates all environment variables in the specified scope.
    /// </summary>
    public static unsafe Collection<KeyValuePair<string, string>> EnumerateVariables(EnvironmentScope scope)
    {
        HKEY hKeyRoot = scope == EnvironmentScope.User ? HKEY.HKEY_CURRENT_USER : HKEY.HKEY_LOCAL_MACHINE;
        string subKey = scope == EnvironmentScope.User ? UserSubKey : SystemSubKey;

        using ManagedPtr<char> lpSubKey = subKey;
        HKEY hKey = default;

        if (RegOpenKeyExW(hKeyRoot, lpSubKey, 0, KEY.KEY_READ, &hKey) != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS)
            return new Collection<KeyValuePair<string, string>>();

        var results = new Collection<KeyValuePair<string, string>>();

        try
        {
            uint cValues = 0;
            uint maxNameLen = 0;
            uint maxValueLen = 0;

            if (RegQueryInfoKeyW(hKey, null, null, null, null, null, null, &cValues, &maxNameLen, &maxValueLen, null, null) != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS)
                return results;

            maxNameLen++; // Null terminator
            char* nameBuffer = (char*)NativeMemory.Alloc(maxNameLen * sizeof(char));
            byte* dataBuffer = (byte*)NativeMemory.Alloc(maxValueLen);

            try
            {
                for (uint i = 0; i < cValues; i++)
                {
                    uint nameLen = maxNameLen;
                    uint dataLen = maxValueLen;
                    uint type = 0;

                    if (RegEnumValueW(hKey, i, nameBuffer, &nameLen, null, &type, dataBuffer, &dataLen) == TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS)
                    {
                        string name = new string(nameBuffer, 0, (int)nameLen);
                        string value = new string((char*)dataBuffer, 0, (int)(dataLen / sizeof(char))).TrimEnd('\0');
                        results.Add(new KeyValuePair<string, string>(name, value));
                    }
                }
            }
            finally
            {
                NativeMemory.Free(nameBuffer);
                NativeMemory.Free(dataBuffer);
            }
        }
        finally
        {
            _ = RegCloseKey(hKey);
        }

        return results;
    }

    /// <summary>
    /// Sets or deletes a persistent environment variable and broadcasts the change.
    /// </summary>
    public static unsafe bool SetVariable(string name, string? value, EnvironmentScope scope)
    {
        HKEY hKeyRoot = scope == EnvironmentScope.User ? HKEY.HKEY_CURRENT_USER : HKEY.HKEY_LOCAL_MACHINE;
        string subKey = scope == EnvironmentScope.User ? UserSubKey : SystemSubKey;

        // Elevate privileges warning: Modifying System scope requires administrative access.
        uint samDesired = KEY.KEY_WRITE | KEY.KEY_READ;

        using ManagedPtr<char> lpSubKey = subKey;
        using ManagedPtr<char> lpValueName = name;

        HKEY hKey = default;

        int status = RegOpenKeyExW(hKeyRoot, lpSubKey, 0, samDesired, &hKey);
        if (status != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS) return false;

        try
        {
            if (value == null)
            {
                // Deleting the environment variable
                status = RegDeleteValueW(hKey, lpValueName);
                if (status is not TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS and not TerraFX.Interop.Windows.ERROR.ERROR_FILE_NOT_FOUND)
                    return false;
            }
            else
            {
                // Setting the environment variable
                fixed (char* lpData = value)
                {
                    uint cbData = (uint)((value.Length + 1) * sizeof(char));

                    // Use REG_EXPAND_SZ if it contains variable references like %USERPROFILE%
                    uint type = value.Contains('%', StringComparison.InvariantCultureIgnoreCase) ? REG.REG_EXPAND_SZ : REG.REG_SZ;

                    status = RegSetValueExW(hKey, lpValueName, 0, type, (byte*)lpData, cbData);
                    if (status != TerraFX.Interop.Windows.ERROR.ERROR_SUCCESS) return false;
                }
            }
        }
        finally
        {
            _ = RegCloseKey(hKey);
        }

        // Inform the system of the global environment block updates
        NotifySystemEnvironmentChanged();
        return true;
    }

    /// <summary>
    /// Broadcasts a global message to all top-level windows notifying them of the change.
    /// </summary>
    private static unsafe void NotifySystemEnvironmentChanged()
    {
        // "Environment" string pointer required by WM_SETTINGCHANGE lParam
        string environmentParam = "Environment";

        using ManagedPtr<char> lpEnv = environmentParam;

        // SendMessageTimeoutW blocks up to 5000ms per window hanging on it
        _ = SendMessageTimeoutW(
            HWND.HWND_BROADCAST,
            WM.WM_SETTINGCHANGE,
            0,
            (nint)lpEnv.ObjectPointer,
            SMTO_ABORTIFHUNG,
            5000,
            null
        );
    }
}