using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Helpers;

namespace Rebound.Shell.Desktop;

[ObservableObject]
public partial class DesktopViewModel
{
    // Settings
    [ObservableProperty] public partial bool IsLivelyCompatibilityEnabled { get; set; }
    [ObservableProperty] public partial bool ShowClockWidget { get; set; }
    [ObservableProperty] public partial bool ShowDesktopIcons { get; set; } = true;
    [ObservableProperty] public partial bool UseMicaMenus { get; set; } = true;
    [ObservableProperty] public partial bool UseRosePine { get; set; }
    [ObservableProperty] public partial bool ShowInfoBar { get; set; }
    [ObservableProperty] public partial bool ShowCalendarWidget { get; set; }
    [ObservableProperty] public partial bool ShowCPUAndRAMWidget { get; set; }

    // Properties
    [ObservableProperty] public partial string CurrentTime { get; set; } = "";
    [ObservableProperty] public partial string CurrentDate { get; set; } = "";
    [ObservableProperty] public partial string CurrentDay { get; set; } = "";
    [ObservableProperty] public partial string CurrentDayOfMonth { get; set; } = "";
    [ObservableProperty] public partial string CurrentMonthAndYear { get; set; } = "";
    [ObservableProperty] public partial string CPUUsage { get; set; } = "";
    [ObservableProperty] public partial string RAMUsage { get; set; } = "";

    public DesktopViewModel()
    {
        IsLivelyCompatibilityEnabled = SettingsHelper.GetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", false);
        ShowClockWidget = SettingsHelper.GetValue("ShowClockWidget", "rshell.desktop", true);
        ShowDesktopIcons = SettingsHelper.GetValue("ShowDesktopIcons", "rshell.desktop", true);
        UseMicaMenus = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false);
        UseRosePine = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false);
        ShowInfoBar = SettingsHelper.GetValue("ShowInfoBar", "rshell.desktop", false);
        ShowCalendarWidget = SettingsHelper.GetValue("ShowCalendarWidget", "rshell.desktop", false);
        ShowCPUAndRAMWidget = SettingsHelper.GetValue("ShowCPUAndRAMWidget", "rshell.desktop", false);
    }

    partial void OnIsLivelyCompatibilityEnabledChanged(bool value) => SettingsHelper.SetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", value);
    partial void OnShowClockWidgetChanged(bool value) => SettingsHelper.SetValue("ShowClockWidget", "rshell.desktop", value);
    partial void OnShowDesktopIconsChanged(bool value) => SettingsHelper.SetValue("ShowDesktopIcons", "rshell.desktop", value);
    partial void OnUseMicaMenusChanged(bool value) => SettingsHelper.SetValue("UseMicaMenus", "rshell.desktop", value);
    partial void OnUseRosePineChanged(bool value) => SettingsHelper.SetValue("UseRosePine", "rshell.desktop", value);
    partial void OnShowInfoBarChanged(bool value) => SettingsHelper.SetValue("ShowInfoBar", "rshell.desktop", value);
    partial void OnShowCalendarWidgetChanged(bool value) => SettingsHelper.SetValue("ShowCalendarWidget", "rshell.desktop", value);
    partial void OnShowCPUAndRAMWidgetChanged(bool value) => SettingsHelper.SetValue("ShowCPUAndRAMWidget", "rshell.desktop", value);
}