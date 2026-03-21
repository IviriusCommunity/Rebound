// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using Rebound.Core.Native;
using TerraFX.Interop.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class Device
{
    public static unsafe string GetDeviceManufacturer()
    {
        string? result = null;
        var success = new WmiConnection().ExecuteWmiQuery("SELECT Manufacturer FROM Win32_ComputerSystem", obj =>
        {
            result = WmiConnection.GetString((IWbemClassObject*)obj, "Manufacturer");
        });
        return result ?? $"Unknown (query success: {success})";
    }

    public static string GetDeviceModel()
    {
        string? result = null;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            if (key != null)
            {
                string? product = key.GetValue("SystemProductName") as string;
                string? family = key.GetValue("SystemFamily") as string;

                result =
                    string.IsNullOrWhiteSpace(product) ? family :
                    string.IsNullOrWhiteSpace(family) ? product :
                    $"{product} ({family})";
            }
        }
        catch { }

        return result ?? "Unknown";
    }
}
