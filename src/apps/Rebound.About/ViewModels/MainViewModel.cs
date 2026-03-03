// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Core.UI.UWP;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rebound.About.ViewModels;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal partial class MainViewModel : ObservableObject
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly SettingsListener _listener;

    private readonly LiveHardwareFeed _liveHardwareFeed;

    [ObservableProperty] public partial bool Loaded { get; set; } = false;

    public MainViewModel()
    {
        UpdateSettings();
        _listener = new SettingsListener();
        _listener.SettingChanged += Listener_SettingChanged;
        _liveHardwareFeed = new LiveHardwareFeed();
        _liveHardwareFeed.OnUpdate += LiveHardwareFeed_OnUpdate;
        _liveHardwareFeed.Start();
    }

    public async Task InitializeAsync()
    {
        await InitializeHardwareInformationAsync().ConfigureAwait(false);
        await Task.Yield();
        await InitializeSoftwareInformationAsync().ConfigureAwait(false);
        await Task.Yield();
        await InitializeUserInformationAsync().ConfigureAwait(false);
        UIThreadQueue.QueueAction(() => Loaded = true);
    }

    // Hardware information
    [ObservableProperty] public partial int CpuUsage { get; set; } = 0;
    [ObservableProperty] public partial string CpuName { get; set; } = "Loading...";
    [ObservableProperty] public partial string CpuArchitecture { get; set; } = "Loading...";

    [ObservableProperty] public partial string GpuName { get; private set; } = "Loading...";
    [ObservableProperty] public partial int GpuUsage { get; private set; } = 0;

    [ObservableProperty] public partial long InstalledRam { get; private set; } = 0;
    [ObservableProperty] public partial long UsableRam { get; private set; } = 0;
    [ObservableProperty] public partial int RamUsage { get; private set; } = 0;

    [ObservableProperty] public partial long PagefileSize { get; private set; } = 0;

    [ObservableProperty] public partial string DisplayResolution { get; private set; } = "Loading...";
    [ObservableProperty] public partial string DisplayRefreshRate { get; private set; } = "Loading...";

    [ObservableProperty] public partial double WindowsOccupiedSpace { get; private set; } = 0;
    [ObservableProperty] public partial double TotalOccupiedSpace { get; private set; } = 0;
    [ObservableProperty] public partial string WindowsOccupiedSpaceString { get; private set; } = "Loading...";
    [ObservableProperty] public partial string TotalOccupiedSpaceString { get; private set; } = "Loading...";

    [ObservableProperty] public partial string DeviceManufacturer { get; private set; } = "Loading...";
    [ObservableProperty] public partial string DeviceModel { get; private set; } = "Loading...";
    [ObservableProperty] public partial string Uptime { get; private set; } = "Loading...";

    public async Task InitializeHardwareInformationAsync()
    {
        UIThreadQueue.QueueAction(() => { CpuName = CPU.GetName(); CpuArchitecture = CPU.GetArchitecture(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { GpuName = GPU.GetName(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { InstalledRam = RAM.GetInstalledRam(); UsableRam = RAM.GetUsableRam(); PagefileSize = RAM.GetPageFileSize(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { var res = Display.GetDisplayResolution(); DisplayResolution = $"{res.cx}x{res.cy}"; DisplayRefreshRate = $"{Display.GetDisplayRefreshRate()} Hz"; });
        await Task.Yield();
        UIThreadQueue.QueueAction(() =>
        {
            var w = Storage.GetWindowsDriveOccupiedSpacePercentage();
            WindowsOccupiedSpace = w;
            WindowsOccupiedSpaceString = ((int)w).ToString((IFormatProvider?)null);
            var t = Storage.GetTotalOccupiedSpacePercentage();
            TotalOccupiedSpace = t;
            TotalOccupiedSpaceString = ((int)t).ToString((IFormatProvider?)null);
        });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { DeviceManufacturer = Device.GetDeviceManufacturer(); DeviceModel = Device.GetDeviceModel(); });
    }

    private void LiveHardwareFeed_OnUpdate(object? sender, HardwareFeedUpdateEventArgs e)
    {
        UIThreadQueue.QueueAction(() =>
        {
            CpuUsage = e.CpuUsage;
            RamUsage = e.RamUsagePercent;
            GpuUsage = e.GpuUsage;
            Uptime = $"{(int)e.Uptime.TotalDays}d {e.Uptime.Hours}h {e.Uptime.Minutes}m";
        });
    }

    // Software information
    [ObservableProperty] public partial string WindowsActivationInfo { get; private set; } = "Loading...";
    [ObservableProperty] public partial string OSDisplayName { get; private set; } = "Loading...";
    [ObservableProperty] public partial string DetailedWindowsVersion { get; private set; } = "Loading...";
    [ObservableProperty] public partial string OSName { get; private set; } = "Loading...";
    [ObservableProperty] public partial string InstalledOnDate { get; private set; } = "Loading...";
    [ObservableProperty] public partial string ComputerName { get; private set; } = "Loading...";
    [ObservableProperty] public partial string Locale { get; private set; } = "Loading...";
    [ObservableProperty] public partial string LocalIP { get; private set; } = "Loading...";
    [ObservableProperty] public partial string Scale { get; private set; } = "Loading...";

    public async Task InitializeSoftwareInformationAsync()
    {
        UIThreadQueue.QueueAction(() =>
        {
            WindowsActivationInfo = LocalizedResource.GetLocalizedString(WindowsInformation.GetWindowsActivationType() switch
            {
                WindowsActivationType.Unlicensed => "ActivationStatusUnlicensed",
                WindowsActivationType.Activated => "ActivationStatusActivated",
                WindowsActivationType.GracePeriod => "ActivationStatusGracePeriod",
                WindowsActivationType.NonGenuine => "ActivationStatusNonGenuine",
                WindowsActivationType.ExtendedGracePeriod => "ActivationStatusExtendedGracePeriod",
                WindowsActivationType.Unknown => "ActivationStatusUnknown",
                _ => "ActivationStatusUnknown"
            });
        });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { OSDisplayName = WindowsInformation.GetOSDisplayName(); DetailedWindowsVersion = WindowsInformation.GetDetailedWindowsVersion(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { OSName = WindowsInformation.GetOSName(); InstalledOnDate = WindowsInformation.GetInstalledOnDate().ToString((IFormatProvider?)null); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { ComputerName = WindowsInformation.GetComputerName(); Locale = WindowsInformation.GetLocale(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { LocalIP = WindowsInformation.GetLocalIP(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { LoadScale(); });
    }

    private unsafe void LoadScale()
    {
        Scale = (Display.GetScale(new((void*)Process.GetCurrentProcess().MainWindowHandle)) * 100).ToString((IFormatProvider?)null) + "%";
    }

    // User information
    [ObservableProperty] public partial string GreetingsMessage { get; private set; } = "Loading...";
    [ObservableProperty] public partial bool IsAdmin { get; private set; } = false;
    [ObservableProperty] public partial bool IsMicrosoftAccount { get; private set; } = false;
    [ObservableProperty] public partial string LegalInfo { get; private set; } = "Loading...";
    [ObservableProperty] public partial string LicenseOwners { get; private set; } = "Loading...";
    [ObservableProperty] public partial string PasswordExpiryDate { get; private set; } = "Loading...";

    public async Task InitializeUserInformationAsync()
    {
        UIThreadQueue.QueueAction(() => { GreetingsMessage = LocalizedResource.GetLocalizedStringFromTemplate("HelloUser", UserInformation.GetDisplayName()); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { IsAdmin = UserInformation.IsAdmin(); IsMicrosoftAccount = UserInformation.IsMicrosoftAccount(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { LegalInfo = LocalizedResource.GetLocalizedStringFromTemplate("LegalInfo", WindowsInformation.GetOSName()); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { LicenseOwners = WindowsInformation.GetLicenseOwners(); });
        await Task.Yield();
        UIThreadQueue.QueueAction(() => { PasswordExpiryDate = UserInformation.GetPasswordExpiry(); });
    }

    // App settings
    [ObservableProperty] public partial bool IsSidebarOn { get; set; }
    [ObservableProperty] public partial bool IsReboundOn { get; set; }
    [ObservableProperty] public partial bool ShowHelloUser { get; set; }
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }
    [ObservableProperty] public partial bool ShowTabs { get; set; }
    [ObservableProperty] public partial bool ShowActivationInfo { get; set; }
    [ObservableProperty] public partial bool ShowExpandedView { get; set; }

    private void Listener_SettingChanged(object? sender, SettingChangedEventArgs e) => UpdateSettings();

    private void UpdateSettings()
    {
        UIThreadQueue.QueueAction(() =>
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