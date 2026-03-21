// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Rebound.Forge.Engines;

/// <summary>
/// Helper class for managing registry stored settings inside Windows.
/// </summary>
public static class RegistrySettingsEngine
{
    public static T? GetValue<T>(RegistryHive hive, string keyPath, string valueName, T? defaultValue = default)
    {
        using var key = OpenHive(hive).OpenSubKey(keyPath);
        return key?.GetValue(valueName, defaultValue) is T value ? value : defaultValue;
    }

    public static bool SetValue<T>(RegistryHive hive, string keyPath, string valueName, T value, RegistryValueKind kind = RegistryValueKind.DWord)
    {
        using var key = OpenHive(hive).OpenSubKey(keyPath, writable: true);
        if (key == null) return false;
        key.SetValue(valueName, value!, kind);
        return true;
    }

    public static bool GetBool(RegistryHive hive, string keyPath, string valueName, bool defaultValue = false)
        => (GetValue(hive, keyPath, valueName, defaultValue ? 1 : 0)) != 0;

    public static bool SetBool(RegistryHive hive, string keyPath, string valueName, bool value)
        => SetValue(hive, keyPath, valueName, value ? 1 : 0);

    public static void EnsureKeyExists(RegistryHive hive, string keyPath)
    {
        using var key = OpenHive(hive).OpenSubKey(keyPath, writable: true);
        if (key == null)
            OpenHive(hive).CreateSubKey(keyPath);
    }

    private static RegistryKey OpenHive(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => Registry.LocalMachine,
        RegistryHive.CurrentUser => Registry.CurrentUser,
        _ => Registry.LocalMachine
    };
}