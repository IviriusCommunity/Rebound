// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Diagnostics;

namespace Rebound.About.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    // Version information

    public static string DetailedWindowsVersion
    {
        get
        {
            return $"{WindowsInformation.GetDisplayVersion()} ({WindowsInformation.GetCurrentBuildNumber()}.{WindowsInformation.GetUBR()})";
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

    public static string LegalInfo => string.Format(null, Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("LegalInfo"), WindowsInformation.GetOSName());

    // App settings

    public long pagefileSize { get; } = RAM.GetPageFileSize();

    [ObservableProperty] public partial bool IsSidebarOn { get; set; }
    [ObservableProperty] public partial bool IsReboundOn { get; set; }
    [ObservableProperty] public partial bool ShowHelloUser { get; set; }
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }
    [ObservableProperty] public partial bool ShowTabs { get; set; }
    [ObservableProperty] public partial bool ShowActivationInfo { get; set; }
    [ObservableProperty] public partial bool ShowExpandedView { get; set; }
    [ObservableProperty] public partial int CPUUsage { get; set; }
    [ObservableProperty] public partial int MemoryUsage { get; set; }
    [ObservableProperty] public partial int GPUUsage { get; set; }

    private readonly SettingsListener _listener;

    private readonly LiveHardwareFeed _liveHardwareFeed;

    public MainViewModel()
    {
        UpdateSettings();
        _listener = new SettingsListener();
        _listener.SettingChanged += Listener_SettingChanged;
        _liveHardwareFeed = new LiveHardwareFeed();
        _liveHardwareFeed.OnUpdate += LiveHardwareFeed_OnUpdate;
        _liveHardwareFeed.Start();
    }

    private void LiveHardwareFeed_OnUpdate(object? sender, HardwareFeedUpdateEventArgs e)
    {
        UIThreadQueue.QueueAction(() =>
        {
            Debug.WriteLine($"Received hardware update: CPU {e.CPUUsage}%, RAM {e.RAMUsagePercent}%, GPU {e.GPUUsage}%");
            CPUUsage = e.CPUUsage;
            MemoryUsage = e.RAMUsagePercent;
            GPUUsage = e.GPUUsage;
        });
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