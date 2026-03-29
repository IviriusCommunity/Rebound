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

        /// <summary>
        /// Controls the Direct3D/DXGI debug layer. 0 = app controlled, 1 = force on, 2 = force off.
        /// </summary>
        public static readonly RegistrySetting LoadDebugLayerDlls = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "LoadDebugLayerDlls"
        };

        /// <summary>
        /// Disables conservative resource state tracking in the debug layer.
        /// Stored inverted: 0 = tracking enabled, 1 = tracking disabled.
        /// </summary>
        public static readonly RegistrySetting DisableConservativeResourceStateTracking = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "DisableConservativeResourceStateTracking"
        };

        /// <summary>
        /// GPU slowdown factor percentage applied by the debug layer.
        /// </summary>
        public static readonly RegistrySetting GPUSlowdownFactor = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "GPUSlowdownFactor"
        };

        /// <summary>
        /// Controls GPU-based validation. 0 = app controlled, 1 = force on, 2 = force off.
        /// </summary>
        public static readonly RegistrySetting EnableGpuBasedValidation = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "EnableGpuBasedValidation"
        };

        /// <summary>
        /// Enables PSO create front load for GPU-based validation.
        /// </summary>
        public static readonly RegistrySetting GpuBasedValidationPsoCreateFrontLoad = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "GpuBasedValidationPsoCreateFrontLoad"
        };

        /// <summary>
        /// Shader patch mode for GPU-based validation. 0 = app controlled, 1 = force on, 2 = force off.
        /// </summary>
        public static readonly RegistrySetting GpuBasedValidationShaderPatchMode = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "GpuBasedValidationShaderPatchMode"
        };

        /// <summary>
        /// Disables synchronized command queue validation.
        /// Stored inverted: 0 = validation enabled, 1 = validation disabled.
        /// </summary>
        public static readonly RegistrySetting DisableSynchronizedCommandQueueValidation = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "DisableSynchronizedCommandQueueValidation"
        };

        /// <summary>
        /// Mutes all Direct3D debug output when set to 1.
        /// </summary>
        public static readonly RegistrySetting MuteDebugOutput = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "MuteDebugOutput"
        };

        /// <summary>
        /// Packed bitmask of severity/category flags for message muting.
        /// </summary>
        public static readonly RegistrySetting MuteFlags = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "MuteFlags"
        };

        /// <summary>
        /// Subkey under which custom muted message IDs are stored as value names.
        /// </summary>
        public static readonly RegistrySetting MuteList = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D\MuteList",
            ValueName = string.Empty
        };

        /// <summary>
        /// Enables breaking on API errors.
        /// </summary>
        public static readonly RegistrySetting EnableBreakOnApiError = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "EnableBreakOnApiError"
        };

        /// <summary>
        /// Packed bitmask of severity/category flags for break-on-message.
        /// </summary>
        public static readonly RegistrySetting BreakFlags = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "BreakFlags"
        };

        /// <summary>
        /// Subkey under which custom break message IDs are stored as value names.
        /// </summary>
        public static readonly RegistrySetting BreakList = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D\BreakList",
            ValueName = string.Empty
        };

        /// <summary>
        /// Subkey under which Direct3D scope app paths are stored.
        /// </summary>
        public static readonly RegistrySetting D3DScopeDrivers = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D\Drivers",
            ValueName = string.Empty
        };

        /// <summary>
        /// Maximum Direct3D feature level. 0 = no limit.
        /// Maps: 0xb000 = DX11, 0xb100 = DX11.1, 0xc000 = DX12, 0xc100 = DX12.1.
        /// </summary>
        public static readonly RegistrySetting MaxFeatureLevel = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "MaxFeatureLevel"
        };

        /// <summary>
        /// Prevents the runtime from upgrading the feature level beyond what was requested.
        /// </summary>
        public static readonly RegistrySetting DisableFeatureLevelUpgrade = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "DisableFeatureLevelUpgrade"
        };

        /// <summary>
        /// Forces all Direct3D rendering through the WARP software rasterizer.
        /// </summary>
        public static readonly RegistrySetting ForceWARP = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct3D",
            ValueName = "ForceWARP"
        };

        /// <summary>
        /// Controls the Direct2D debug layer. 0 = app controlled, 1 = force on, 2 = force off.
        /// </summary>
        public static readonly RegistrySetting D2DEnableDebugLayer = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct2D",
            ValueName = "EnableDebugLayer"
        };

        /// <summary>
        /// Debug verbosity level for Direct2D. 0 = low, 1 = medium, 2 = high.
        /// </summary>
        public static readonly RegistrySetting D2DDebugLevel = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct2D",
            ValueName = "DebugLevel"
        };

        /// <summary>
        /// Subkey under which Direct2D scope app paths are stored.
        /// </summary>
        public static readonly RegistrySetting D2DScopeDrivers = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Direct2D\Drivers",
            ValueName = string.Empty
        };
    }
}