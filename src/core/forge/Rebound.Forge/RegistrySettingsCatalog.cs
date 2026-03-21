// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge
{
    /// <summary>
    /// A registry setting mapping.
    /// </summary>
    public struct RegistrySetting
    {
        /// <summary>
        /// Path to the registry key.
        /// </summary>
        public string KeyPath { get; set; }

        /// <summary>
        /// The name of the registry value.
        /// </summary>
        public string ValueName { get; set; }
    }

    /// <summary>
    /// Provides a catalog of predefined Windows settings in the registry.
    /// </summary>
    public static class RegistrySettingsCatalog
    {
        /// <summary>
        /// Controls whether Windows automatically downloads device metadata and OEM apps
        /// from the Windows Metadata and Internet Services (WMIS) server for connected devices.
        /// When enabled, Windows will silently install manufacturer apps (e.g. Lenovo Vantage)
        /// upon connecting hardware. Disable to prevent unsolicited OEM software installation.
        /// </summary>
        public static readonly RegistrySetting InstallOemApps = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata", 
            ValueName = "PreventDeviceMetadataFromNetwork"
        };
    }
}
