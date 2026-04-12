// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI.Localizer;
using Rebound.Core.UI.Threading;
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
        await Task.WhenAll(
            InitializePrimarySoftwareAsync(),
            InitializePrimaryHardwareAsync(),
            InitializePrimaryUserAsync()
        ).ConfigureAwait(false);

        UIThread.QueueAction(() => Loaded = true);

        await Task.WhenAll(
            InitializeSecondarySoftwareAsync(),
            InitializeSecondaryHardwareAsync(),
            InitializeSecondaryUserAsync()
        ).ConfigureAwait(false);
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

    private void LiveHardwareFeed_OnUpdate(object? sender, HardwareFeedUpdateEventArgs e)
    {
        UIThread.QueueAction(() =>
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
        UIThread.QueueAction(() =>
        {
            IsSidebarOn = SettingsManager.GetValue("IsSidebarOn", "winver", true);
            IsReboundOn = SettingsManager.GetValue("IsReboundOn", "winver", true);
            ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
            ShowHelloUser = SettingsManager.GetValue("ShowHelloUser", "winver", true);
            ShowTabs = SettingsManager.GetValue("ShowTabs", "winver", true);
            ShowActivationInfo = SettingsManager.GetValue("ShowActivationInfo", "winver", true);
        });
    }

    // Init

    // Primary

    public async Task InitializePrimarySoftwareAsync()
    {
        var osDisplayName = Task.Run(WindowsInformation.GetOSDisplayName);
        var detailedVersion = Task.Run(WindowsInformation.GetDetailedWindowsVersion);
        var osName = Task.Run(WindowsInformation.GetOSName);
        var computerName = Task.Run(WindowsInformation.GetComputerName);

        await Task.WhenAll(osDisplayName, detailedVersion, osName, computerName).ConfigureAwait(false);

        UIThread.QueueAction(() =>
        {
            OSDisplayName = osDisplayName.Result;
            DetailedWindowsVersion = detailedVersion.Result;
            OSName = osName.Result;
            ComputerName = computerName.Result;
        });
    }

    public async Task InitializePrimaryHardwareAsync()
    {
        var installedRam = Task.Run(RAM.GetInstalledRam);
        var usableRam = Task.Run(RAM.GetUsableRam);

        await Task.WhenAll(installedRam, usableRam).ConfigureAwait(false);

        UIThread.QueueAction(() =>
        {
            InstalledRam = installedRam.Result;
            UsableRam = usableRam.Result;
        });
    }

    public async Task InitializePrimaryUserAsync()
    {
        var greetings = LocalizedResource.GetLocalizedStringFromTemplate("HelloUser", UserInformation.GetDisplayName());
        var isAdmin = Task.Run(UserInformation.IsAdmin);
        var legalInfo = LocalizedResource.GetLocalizedStringFromTemplate("LegalInfo", WindowsInformation.GetOSName());

        await Task.WhenAll(isAdmin).ConfigureAwait(false);

        UIThread.QueueAction(() =>
        {
            GreetingsMessage = greetings;
            IsAdmin = isAdmin.Result;
            LegalInfo = legalInfo;
        });
    }

    // Secondary

    public async Task InitializeSecondarySoftwareAsync()
    {
        var activationType = Task.Run(WindowsInformation.GetWindowsActivationType);
        var installedOn = Task.Run(() => WindowsInformation.GetInstalledOnDate().ToString((IFormatProvider?)null));
        var locale = Task.Run(WindowsInformation.GetLocale);
        var localIP = Task.Run(WindowsInformation.GetLocalIP);

        await Task.WhenAll(activationType, installedOn, locale, localIP).ConfigureAwait(false);

        UIThread.QueueAction(() =>
        {
            WindowsActivationInfo = LocalizedResource.GetLocalizedString(activationType.Result switch
            {
                WindowsActivationType.Unlicensed => "ActivationStatusUnlicensed",
                WindowsActivationType.Activated => "ActivationStatusActivated",
                WindowsActivationType.GracePeriod => "ActivationStatusGracePeriod",
                WindowsActivationType.NonGenuine => "ActivationStatusNonGenuine",
                WindowsActivationType.ExtendedGracePeriod => "ActivationStatusExtendedGracePeriod",
                _ => "ActivationStatusUnknown"
            });
            InstalledOnDate = installedOn.Result;
            Locale = locale.Result;
            LocalIP = localIP.Result;
            LoadScale();
        });
    }

    public async Task InitializeSecondaryHardwareAsync()
    {
        var cpuName = Task.Run(CPU.GetName);
        var cpuArch = Task.Run(CPU.GetArchitecture);
        var gpuName = Task.Run(GPU.GetName);
        var pagefileSize = Task.Run(RAM.GetPageFileSize);
        var res = Task.Run(Display.GetDisplayResolution);
        var refreshRate = Task.Run(Display.GetDisplayRefreshRate);
        var winSpace = Task.Run(Storage.GetWindowsDriveOccupiedSpacePercentage);
        var totalSpace = Task.Run(Storage.GetTotalOccupiedSpacePercentage);
        var manufacturer = Task.Run(Device.GetDeviceManufacturer);
        var model = Task.Run(Device.GetDeviceModel);

        await Task.WhenAll(cpuName, cpuArch, gpuName, pagefileSize, res,
            refreshRate, winSpace, totalSpace, manufacturer, model).ConfigureAwait(false);

        var resResult = res.Result;
        var winSpaceResult = winSpace.Result;
        var totalSpaceResult = totalSpace.Result;

        UIThread.QueueAction(() =>
        {
            CpuName = cpuName.Result;
            CpuArchitecture = cpuArch.Result;
            GpuName = gpuName.Result;
            PagefileSize = pagefileSize.Result;
            DisplayResolution = $"{resResult.cx}x{resResult.cy}";
            DisplayRefreshRate = $"{refreshRate.Result} Hz";
            WindowsOccupiedSpace = winSpaceResult;
            WindowsOccupiedSpaceString = ((int)winSpaceResult).ToString((IFormatProvider?)null);
            TotalOccupiedSpace = totalSpaceResult;
            TotalOccupiedSpaceString = ((int)totalSpaceResult).ToString((IFormatProvider?)null);
            DeviceManufacturer = manufacturer.Result;
            DeviceModel = model.Result;
        });
    }

    public async Task InitializeSecondaryUserAsync()
    {
        var isMsAccount = Task.Run(UserInformation.IsMicrosoftAccount);
        var licenseOwners = Task.Run(WindowsInformation.GetLicenseOwners);
        var passwordExpiry = Task.Run(UserInformation.GetPasswordExpiry);

        await Task.WhenAll(isMsAccount, licenseOwners, passwordExpiry).ConfigureAwait(false);

        UIThread.QueueAction(() =>
        {
            IsMicrosoftAccount = isMsAccount.Result;
            LicenseOwners = licenseOwners.Result;
            PasswordExpiryDate = passwordExpiry.Result;
        });
    }
}