using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Environment;
using System;

namespace Rebound.About.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    // Version information

    public static string WindowsVersionName
    {
        get => 
            SystemInformation.GetOSName().Contains("10", StringComparison.InvariantCultureIgnoreCase) ? 
            "Windows 10" : 
            SystemInformation.GetOSName().Contains("Server", StringComparison.InvariantCultureIgnoreCase) ? 
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
                SystemInformation.GetDisplayVersion(),
                SystemInformation.GetCurrentBuildNumber(),
                SystemInformation.GetUBR());
        }
    }

    public static string CurrentUser
    {
        get
        {
            var fullName = SystemInformation.GetDisplayName();
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string template = resourceLoader.GetString("HelloUser");

            // Use InvariantCulture explicitly for string.Format
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, fullName);
        }
    }

    public static string WindowsVersionTitle => SystemInformation.GetOSName();
    public static string LicenseOwners => SystemInformation.GetLicenseOwners();
    public static string LegalInfo => string.Format(null, Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("LegalInfo"), SystemInformation.GetOSName());
    public static string CPUName => SystemInformation.GetCPUName();
    public static string GPUName => SystemInformation.GetGPUName();
    public static string RAM => SystemInformation.GetTotalRam();
    public static string UsableRAM => SystemInformation.GetUsableRAM();

    // App settings

    [ObservableProperty] public partial bool IsSidebarOn { get; set; }
    [ObservableProperty] public partial bool IsReboundOn { get; set; }
    [ObservableProperty] public partial bool ShowHelloUser { get; set; }
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }

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
        Program.QueueAction(async () =>
        {
            IsSidebarOn = SettingsHelper.GetValue("IsSidebarOn", "winver", true);
            IsReboundOn = SettingsHelper.GetValue("IsReboundOn", "winver", true);
            ShowBlurAndGlow = SettingsHelper.GetValue("ShowBlurAndGlow", "rebound", true);
            ShowHelloUser = SettingsHelper.GetValue("ShowHelloUser", "winver", true);
        });
    }
}