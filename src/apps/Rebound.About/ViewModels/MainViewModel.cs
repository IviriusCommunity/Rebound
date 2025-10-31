// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;

namespace Rebound.About.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    // Version information

    public static string WindowsVersionName
    {
        get => 
            WindowsInformation.GetOSName().Contains("10", StringComparison.InvariantCultureIgnoreCase) ? 
            "Windows 10" :
            WindowsInformation.GetOSName().Contains("Server", StringComparison.InvariantCultureIgnoreCase) ? 
            "Windows Server" : 
            "Windows 11";
    }

    public static string DetailedWindowsVersion
    {
        get
        {
            return string.Format(
                null,
                Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("VersionOSBuild"),
                WindowsInformation.GetDisplayVersion(),
                WindowsInformation.GetCurrentBuildNumber(),
                WindowsInformation.GetUBR());
        }
    }

    public static string CurrentUser
    {
        get
        {
            var fullName = UserInformation.GetDisplayName();
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string template = resourceLoader.GetString("HelloUser");

            // Use InvariantCulture explicitly for string.Format
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, fullName);
        }
    }

    public static string WindowsActivationInfo
    {
        get
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            return resourceLoader.GetString(WindowsInformation.GetWindowsActivationType() switch
            {
                WindowsActivationType.Unlicensed => "ActivationStatusUnlicensed",
                WindowsActivationType.Activated => "ActivationStatusActivated",
                WindowsActivationType.GracePeriod => "ActivationStatusGracePeriod",
                WindowsActivationType.NonGenuine => "ActivationStatusNonGenuine",
                WindowsActivationType.ExtendedGracePeriod => "ActivationStatusExtendedGracePeriod",
                WindowsActivationType.Unknown => "ActivationStatusUnknown",
                _ => "ActivationStatusUnknown"
            });
        }
    }

    public static string WindowsVersionTitle => WindowsInformation.GetOSName();
    public static string LicenseOwners => WindowsInformation.GetLicenseOwners();
    public static string LegalInfo => string.Format(null, Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("LegalInfo"), WindowsInformation.GetOSName());
    public static string CPUName => CPU.GetCPUName();
    public static string GPUName => GPU.GetGPUName();
    public static string RAM => Core.SystemInformation.Hardware.RAM.GetTotalRam();
    public static string UsableRAM => Core.SystemInformation.Hardware.RAM.GetUsableRAM();

    // App settings

    [ObservableProperty] public partial bool IsSidebarOn { get; set; }
    [ObservableProperty] public partial bool IsReboundOn { get; set; }
    [ObservableProperty] public partial bool ShowHelloUser { get; set; }
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }
    [ObservableProperty] public partial bool ShowTabs { get; set; }
    [ObservableProperty] public partial bool ShowActivationInfo { get; set; }

    private readonly SettingsListener _listener;

    public MainViewModel()
    {
        UpdateSettings();
        _listener = new SettingsListener();
        _listener.SettingChanged += Listener_SettingChanged;
    }

    private void Listener_SettingChanged(object? sender, SettingChangedEventArgs e) => UpdateSettings();

    private void UpdateSettings()
    {
        UIThreadQueue.QueueAction(async () =>
        {
            IsSidebarOn = SettingsManager.GetValue("IsSidebarOn", "winver", true);
            IsReboundOn = SettingsManager.GetValue("IsReboundOn", "winver", true);
            ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
            ShowHelloUser = SettingsManager.GetValue("ShowHelloUser", "winver", true);
            ShowTabs = SettingsManager.GetValue("ShowTabs", "winver", true);
            ShowActivationInfo = SettingsManager.GetValue("ShowActivationInfo", "winver", true);
        });
    }
}