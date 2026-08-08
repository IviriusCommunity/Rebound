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

        /// <summary>
        /// CPU scheduling preference key (applications vs background services).
        /// </summary>
        public static readonly RegistrySetting ProcessorScheduling = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl",
            ValueName = "Win32PrioritySeparation"
        };

        /// <summary>
        /// Pagefile management registry key.
        /// </summary>
        public static readonly RegistrySetting AutomaticManagedPagefile = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            ValueName = "AutomaticManagedPagefile"
        };

        /// <summary>
        /// Paging files.
        /// </summary>
        public static readonly RegistrySetting PagingFiles = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            ValueName = "PagingFiles"
        };

        /// <summary>
        /// Bootloader timeout (in seconds).
        /// </summary>
        public static readonly RegistrySetting BootloaderTimeout = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\BootControl",
            ValueName = "BootTimeOut"
        };

        /// <summary>
        /// Controls whether the boot GUI is displayed. 0 = show GUI, 1 = no GUI.
        /// </summary>
        public static readonly RegistrySetting NoGuiBoot = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\BootControl",
            ValueName = "NoGuiBoot"
        };
        
        /// <summary>
        /// Registry setting for enabling boot logging.
        /// </summary>
        public static readonly RegistrySetting BootLog = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\BootControl",
            ValueName = "BootLog"
        };

        /// <summary>
        /// Registry setting for enabling base video mode during boot.
        /// </summary>
        public static readonly RegistrySetting BaseVideo = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\BootControl",
            ValueName = "BaseVideo"
        };

        /// <summary>
        /// Registry setting for enabling OS boot information display during boot.
        /// </summary>
        public static readonly RegistrySetting OsBootInfo = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\BootControl",
            ValueName = "OsBootInformation"
        };

        /// <summary>
        /// Registry setting for controlling automatic reboot after a system crash.
        /// </summary>
        public static readonly RegistrySetting AutoReboot = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\CrashControl",
            ValueName = "AutoReboot"
        };

        /// <summary>
        /// Registry setting for controlling whether a system crash is logged in the event log.
        /// </summary>
        public static readonly RegistrySetting LogEvent = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\CrashControl",
            ValueName = "LogEvent"
        };

        /// <summary>
        /// Registry setting for controlling whether a system crash generates a memory dump.
        /// </summary>
        public static readonly RegistrySetting CrashDumpEnabled = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\CrashControl",
            ValueName = "CrashDumpEnabled"
        };

        /// <summary>
        /// Registry setting for controlling the directory where minidumps are stored after a system crash.
        /// </summary>
        public static readonly RegistrySetting MinidumpDir = new()
        {
            KeyPath = @"SYSTEM\CurrentControlSet\Control\CrashControl",
            ValueName = "MinidumpDir"
        };

        /// <summary>
        /// Controls whether automatic updates are completely disabled (1) or enabled (0).
        /// </summary>
        public static readonly RegistrySetting NoAutoUpdate = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "NoAutoUpdate"
        };

        /// <summary>
        /// Controls automatic update behavior options:
        /// 2 = Notify for download, 3 = Auto download/notify install, 4 = Auto download and schedule install.
        /// </summary>
        public static readonly RegistrySetting AUOptions = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "AUOptions"
        };

        /// <summary>
        /// Controls whether recommended non-security updates are included along with essential updates.
        /// </summary>
        public static readonly RegistrySetting IncludeRecommendedUpdates = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
            ValueName = "IncludeRecommendedUpdates"
        };

        /// <summary>
        /// Controls whether feature updates are deferred/blocked (1 = Defer feature upgrades).
        /// </summary>
        public static readonly RegistrySetting DeferFeatureUpdates = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DeferFeatureUpdates"
        };

        /// <summary>
        /// Specifies the deferral period in days for feature updates (up to 365 days).
        /// </summary>
        public static readonly RegistrySetting DeferFeatureUpdatesPeriodInDays = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "DeferFeatureUpdatesPeriodInDays"
        };

        /// <summary>
        /// Indicates whether the "Enable optional updates" policy is configured.
        /// When enabled, <see cref="AllowOptionalContent"/> determines the behavior.
        /// </summary>
        public static readonly RegistrySetting SetAllowOptionalContent = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "SetAllowOptionalContent"
        };

        /// <summary>
        /// Controls whether Windows automatically receives optional updates.
        /// 0 = User chooses (default)
        /// 1 = Automatically receive optional updates
        /// 2 = Automatically receive optional updates excluding controlled feature rollouts (CFRs)
        /// </summary>
        public static readonly RegistrySetting AllowOptionalContent = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "AllowOptionalContent"
        };

        /// <summary>
        /// Removes access to Windows Update features in the Settings app.
        /// </summary>
        public static readonly RegistrySetting SetDisableUXWUAccess = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "SetDisableUXWUAccess"
        };

        /// <summary>
        /// Excludes driver updates from Windows Update quality updates.
        /// </summary>
        public static readonly RegistrySetting ExcludeWUDriversInQualityUpdate = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
            ValueName = "ExcludeWUDriversInQualityUpdate"
        };

        /// <summary>
        /// Disables the Settings agentic search experience.
        /// </summary>
        public static readonly RegistrySetting DisableSettingsAgent = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "DisableSettingsAgent"
        };

        /// <summary>
        /// Disables Cocreator in Microsoft Paint.
        /// </summary>
        public static readonly RegistrySetting DisableCocreator = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint",
            ValueName = "DisableCocreator"
        };

        /// <summary>
        /// Disables Generative Fill in Microsoft Paint.
        /// </summary>
        public static readonly RegistrySetting DisableGenerativeFill = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint",
            ValueName = "DisableGenerativeFill"
        };

        /// <summary>
        /// Disables Image Creator in Microsoft Paint.
        /// </summary>
        public static readonly RegistrySetting DisableImageCreator = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint",
            ValueName = "DisableImageCreator"
        };

        /// <summary>
        /// Disables AI features in Notepad.
        /// </summary>
        public static readonly RegistrySetting DisableAIFeaturesInNotepad = new()
        {
            KeyPath = @"SOFTWARE\Policies\WindowsNotepad",
            ValueName = "DisableAIFeatures"
        };

        /// <summary>
        /// Allows the Recall optional component to be enabled.
        /// </summary>
        public static readonly RegistrySetting AllowRecallEnablement = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "AllowRecallEnablement"
        };

        /// <summary>
        /// Disables saving snapshots for Recall.
        /// </summary>
        public static readonly RegistrySetting DisableAIDataAnalysis = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "DisableAIDataAnalysis"
        };

        /// <summary>
        /// Disables Click to Do.
        /// </summary>
        public static readonly RegistrySetting DisableClickToDo = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
            ValueName = "DisableClickToDo"
        };

        /// <summary>
        /// Configures Windows SmartScreen.
        /// </summary>
        public static readonly RegistrySetting EnableSmartScreen = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "EnableSmartScreen"
        };

        /// <summary>
        /// Disables the Get Started experience.
        /// </summary>
        public static readonly RegistrySetting DisableGetStarted = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableGetStarted"
        };

        /// <summary>
        /// Configures the maximum allowed sudo mode.
        /// </summary>
        public static readonly RegistrySetting EnableSudo = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Sudo",
            ValueName = "Enabled"
        };

        /// <summary>
        /// PowerShell execution policy setting. Possible values: "Restricted", "AllSigned", "RemoteSigned", "Unrestricted", "Bypass".
        /// </summary>
        public static readonly RegistrySetting PowerShellExecutionPolicy = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\PowerShell",
            ValueName = "ExecutionPolicy"
        };

        /// <summary>
        /// Controls whether Windows Script Host is enabled or disabled. 1 = enabled, 0 = disabled.
        /// </summary>
        public static readonly RegistrySetting WindowsScriptHostEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows Script Host\Settings",
            ValueName = "Enabled"
        };

        /// <summary>
        /// Controls whether Windows saves zone information for downloaded files. 1 = save zone information, 0 = do not save.
        /// </summary>
        public static readonly RegistrySetting SaveZoneInformation = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments",
            ValueName = "SaveZoneInformation"
        };

        public static readonly RegistrySetting AllowDevelopmentWithoutDevLicense = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
            ValueName = "AllowDevelopmentWithoutDevLicense"
        };

        public static readonly RegistrySetting AllowAllTrustedApps = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
            ValueName = "AllowAllTrustedApps"
        };

        public static readonly RegistrySetting EnableLUA = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableLUA"
        };

        public static readonly RegistrySetting ConsentPromptBehaviorAdmin = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "ConsentPromptBehaviorAdmin"
        };

        public static readonly RegistrySetting PromptOnSecureDesktop = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "PromptOnSecureDesktop"
        };

        public static readonly RegistrySetting ConsentPromptBehaviorUser = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "ConsentPromptBehaviorUser"
        };

        public static readonly RegistrySetting EnableInstallerDetection = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableInstallerDetection"
        };

        public static readonly RegistrySetting EnableSecureUIAPaths = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "EnableSecureUIAPaths"
        };

        public static readonly RegistrySetting ValidateAdminCodeSignatures = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "ValidateAdminCodeSignatures"
        };

        public static readonly RegistrySetting FilterAdministratorToken = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            ValueName = "FilterAdministratorToken"
        };

        /// <summary>
        /// Controls Windows diagnostic data (telemetry). 0 = Security (Enterprise), 1 = Required, 3 = Optional.
        /// </summary>
        public static readonly RegistrySetting AllowTelemetry = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            ValueName = "AllowTelemetry"
        };

        /// <summary>
        /// Controls tailored experiences using diagnostic data. 1 = enabled, 0 = disabled.
        /// </summary>
        public static readonly RegistrySetting TailoredExperiences = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
            ValueName = "DisableTailoredExperiencesWithDiagnosticData"
        };

        /// <summary>
        /// Controls the advertising ID. 1 = disabled, 0 = enabled.
        /// </summary>
        public static readonly RegistrySetting AdvertisingId = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
            ValueName = "Enabled"
        };

        /// <summary>
        /// Controls Activity History collection. 1 = enabled, 0 = disabled.
        /// </summary>
        public static readonly RegistrySetting PublishUserActivities = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
            ValueName = "PublishUserActivities"
        };

        public static readonly RegistrySetting WindowsCeipEnabled = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\SQMClient\Windows",
            ValueName = "CEIPEnable"
        };

        public static readonly RegistrySetting WindowsErrorReportingDisabled = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
            ValueName = "Disabled"
        };

        public static readonly RegistrySetting OnlineSpeechRecognition = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\InputPersonalization",
            ValueName = "AllowInputPersonalization"
        };

        public static readonly RegistrySetting LocationServices = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
            ValueName = "DisableLocation"
        };

        public static readonly RegistrySetting ApplicationTelemetryEnabled = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\AppCompat",
            ValueName = "AITEnable"
        };

        public static readonly RegistrySetting AllowLinguisticDataCollection = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput",
            ValueName = "AllowLinguisticDataCollection"
        };

        public static readonly RegistrySetting HandwritingPersonalization = new()
        {
            KeyPath = @"Software\Policies\Microsoft\InputPersonalization",
            ValueName = "RestrictImplicitTextCollection"
        };

        public static readonly RegistrySetting TrackApplicationLaunches = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
            ValueName = "Start_TrackProgs"
        };

        public static readonly RegistrySetting DisableApplicationUsageTracking = new()
        {
            KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\EdgeUI",
            ValueName = "DisableMFUTracking"
        };

        public static readonly RegistrySetting RestrictImplicitTextCollection = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\InputPersonalization",
            ValueName = "RestrictImplicitTextCollection"
        };

        public static readonly RegistrySetting RestrictImplicitInkCollection = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\InputPersonalization",
            ValueName = "RestrictImplicitInkCollection"
        };

        public static readonly RegistrySetting AllowOnlineTips = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
            ValueName = "AllowOnlineTips"
        };

        public static readonly RegistrySetting SoftLandingEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SoftLandingEnabled"
        };

        public static readonly RegistrySetting LockScreenTipsEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-338387Enabled"
        };

        public static readonly RegistrySetting WindowsTipsEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-310093Enabled"
        };

        public static readonly RegistrySetting WelcomeExperienceEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-310093Enabled"
        };

        public static readonly RegistrySetting ScoobeSystemSettingEnabled = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement",
            ValueName = "ScoobeSystemSettingEnabled"
        };

        public static readonly RegistrySetting SuggestedContentSettings = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-338393Enabled"
        };

        public static readonly RegistrySetting SuggestedContentSettings2 = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-353694Enabled"
        };

        public static readonly RegistrySetting SuggestedContentSettings3 = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-353696Enabled"
        };

        public static readonly RegistrySetting WindowsTipsAndSuggestions = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SystemPaneSuggestionsEnabled"
        };

        public static readonly RegistrySetting MicrosoftPromotionalNotifications = new()
        {
            KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
            ValueName = "SubscribedContent-338389Enabled"
        };
    }
}