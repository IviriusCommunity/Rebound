// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI.Localizer;
using System.IO;

namespace Rebound.About.ViewModels;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal partial class MainViewModel : ObservableObject
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    public readonly SettingsListener _listener;

    public readonly LiveHardwareFeed _liveHardwareFeed;

    [ObservableProperty] public partial bool Loaded { get; set; } = false;

    [ObservableProperty] public partial bool IsReboundInstalled { get; set; }

    [ObservableProperty] public partial string ReboundText { get; set; } = "Loading...";

    [ObservableProperty] public partial bool ShowCustomBranding { get; set; } = false;

    public MainViewModel()
    {
        IsReboundInstalled = File.Exists(Variables.ReboundCurrentVersionPath);
        UpdateSettings();
        _listener = new SettingsListener();
        _liveHardwareFeed = new LiveHardwareFeed();
    }

    // Hardware information
    [ObservableProperty] public partial int CpuUsage { get; set; } = 0;
    [ObservableProperty] public partial string CpuName { get; set; } = "Loading...";
    [ObservableProperty] public partial string CpuArchitecture { get; set; } = "Loading...";

    [ObservableProperty] public partial string GpuName { get; set; } = "Loading...";
    [ObservableProperty] public partial int GpuUsage { get; set; } = 0;

    [ObservableProperty] public partial long InstalledRam { get; set; } = 0;
    [ObservableProperty] public partial long UsableRam { get; set; } = 0;
    [ObservableProperty] public partial int RamUsage { get; set; } = 0;

    [ObservableProperty] public partial long PagefileSize { get; set; } = 0;

    [ObservableProperty] public partial string DisplayResolution { get; set; } = "Loading...";
    [ObservableProperty] public partial string DisplayRefreshRate { get; set; } = "Loading...";

    [ObservableProperty] public partial double WindowsOccupiedSpace { get; set; } = 0;
    [ObservableProperty] public partial double TotalOccupiedSpace { get; set; } = 0;
    [ObservableProperty] public partial string WindowsOccupiedSpaceString { get; set; } = "Loading...";
    [ObservableProperty] public partial string TotalOccupiedSpaceString { get; set; } = "Loading...";

    [ObservableProperty] public partial string DeviceManufacturer { get; set; } = "Loading...";
    [ObservableProperty] public partial string DeviceModel { get; set; } = "Loading...";
    [ObservableProperty] public partial string MotherboardModel { get; set; } = "Loading...";
    [ObservableProperty] public partial string Uptime { get; set; } = "Loading...";

    // Software information
    [ObservableProperty] public partial string WindowsActivationInfo { get; set; } = "Loading...";
    [ObservableProperty] public partial string OSDisplayName { get; set; } = "Loading...";
    [ObservableProperty] public partial string DetailedWindowsVersion { get; set; } = "Loading...";
    [ObservableProperty] public partial string OSName { get; set; } = "Loading...";
    [ObservableProperty] public partial string InstalledOnDate { get; set; } = "Loading...";
    [ObservableProperty] public partial string ComputerName { get; set; } = "Loading...";
    [ObservableProperty] public partial string Locale { get; set; } = "Loading...";
    [ObservableProperty] public partial string LocalIP { get; set; } = "Loading...";
    [ObservableProperty] public partial string Scale { get; set; } = "Loading...";

    // User information
    [ObservableProperty] public partial string GreetingsMessage { get; set; } = "Loading...";
    [ObservableProperty] public partial bool IsAdmin { get; set; } = false;
    [ObservableProperty] public partial bool IsMicrosoftAccount { get; set; } = false;
    [ObservableProperty] public partial string LegalInfo { get; set; } = "Loading...";
    [ObservableProperty] public partial string LicenseOwners { get; set; } = "Loading...";
    [ObservableProperty] public partial string PasswordExpiryDate { get; set; } = "Loading...";

    // App settings
    [ObservableProperty] public partial bool IsSidebarOn { get; set; }
    [ObservableProperty] public partial bool IsReboundOn { get; set; }
    [ObservableProperty] public partial bool ShowHelloUser { get; set; }
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }
    [ObservableProperty] public partial bool ShowTabs { get; set; }
    [ObservableProperty] public partial bool ShowActivationInfo { get; set; }
    [ObservableProperty] public partial bool ShowExpandedView { get; set; }

    public void UpdateSettings()
    {
        IsSidebarOn = SettingsManager.GetValue("IsSidebarOn", "winver", true);
        IsReboundOn = SettingsManager.GetValue("IsReboundOn", "winver", true);
        ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
        ShowHelloUser = SettingsManager.GetValue("ShowHelloUser", "winver", true);
        ShowTabs = SettingsManager.GetValue("ShowTabs", "winver", true);
        ShowActivationInfo = SettingsManager.GetValue("ShowActivationInfo", "winver", true);
    }

    // Init

    public void InitializePrimarySoftware()
    {
        OSDisplayName = WindowsInformation.GetOSDisplayName();
        DetailedWindowsVersion = WindowsInformation.GetDetailedWindowsVersion();
        OSName = WindowsInformation.GetOSName();
        ComputerName = WindowsInformation.GetComputerName();
        ReboundText = IsReboundInstalled
            ? LocalizedResource.GetLocalizedString("ReboundInstalledText")
            : LocalizedResource.GetLocalizedString("ReboundNotInstalledText");
    }

    public void InitializePrimaryHardware()
    {
        InstalledRam = RAM.GetInstalledRam();
        UsableRam = RAM.GetUsableRam();
    }

    public void InitializePrimaryUser()
    {
        GreetingsMessage = LocalizedResource.GetLocalizedStringFromTemplate("HelloUser", UserInformation.GetDisplayName());
        IsAdmin = UserInformation.IsAdmin();
        LegalInfo = LocalizedResource.GetLocalizedStringFromTemplate("LegalInfo", WindowsInformation.GetOSName());
    }
}