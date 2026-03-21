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

        /// <summary>
        /// Whether font smoothing is enabled. Stored as string "0" or "2".
        /// </summary>
        public static readonly RegistrySetting FontSmoothing = new()
        {
            KeyPath = @"Control Panel\Desktop",
            ValueName = "FontSmoothing"
        };

        /// <summary>
        /// Font smoothing type. 1 = standard antialiasing, 2 = ClearType.
        /// </summary>
        public static readonly RegistrySetting FontSmoothingType = new()
        {
            KeyPath = @"Control Panel\Desktop",
            ValueName = "FontSmoothingType"
        };

        /// <summary>
        /// Font smoothing gamma. Range 1000–2200, default 1400.
        /// </summary>
        public static readonly RegistrySetting FontSmoothingGamma = new()
        {
            KeyPath = @"Control Panel\Desktop",
            ValueName = "FontSmoothingGamma"
        };

        /// <summary>
        /// Subpixel layout for ClearType. 0 = RGB, 1 = BGR.
        /// </summary>
        public static readonly RegistrySetting FontSmoothingOrientation = new()
        {
            KeyPath = @"Control Panel\Desktop",
            ValueName = "FontSmoothingOrientation"
        };

        /// <summary>
        /// ClearType level for WPF/Avalon. Range 0–100, default 100.
        /// </summary>
        public static readonly RegistrySetting AvalonClearTypeLevel = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Avalon.Graphics",
            ValueName = "ClearTypeLevel"
        };

        /// <summary>
        /// Gamma level for WPF/Avalon. Range 1000–2200, default 1400.
        /// </summary>
        public static readonly RegistrySetting AvalonGammaLevel = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Avalon.Graphics",
            ValueName = "GammaLevel"
        };
    }
}
