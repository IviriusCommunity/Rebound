using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Helpers;

namespace Rebound.Shell.Desktop;

[ObservableObject]
public partial class DesktopViewModel
{
    [ObservableProperty] public partial bool IsLivelyCompatibilityEnabled { get; set; }
    [ObservableProperty] public partial bool ShowClockWidget { get; set; }
    [ObservableProperty] public partial bool ShowDesktopIcons { get; set; } = true;
    [ObservableProperty] public partial bool UseMicaMenus { get; set; } = true;

    public DesktopViewModel()
    {
        IsLivelyCompatibilityEnabled = SettingsHelper.GetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", false);
        ShowClockWidget = SettingsHelper.GetValue("ShowClockWidget", "rshell.desktop", true);
        ShowDesktopIcons = SettingsHelper.GetValue("ShowDesktopIcons", "rshell.desktop", true);
        UseMicaMenus = SettingsHelper.GetValue("UseMicaMenus", "rshell.desktop", false);
    }

    partial void OnIsLivelyCompatibilityEnabledChanged(bool value) => SettingsHelper.SetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", value);
    partial void OnShowClockWidgetChanged(bool value) => SettingsHelper.SetValue("ShowClockWidget", "rshell.desktop", value);
    partial void OnShowDesktopIconsChanged(bool value) => SettingsHelper.SetValue("ShowDesktopIcons", "rshell.desktop", value);
    partial void OnUseMicaMenusChanged(bool value) => SettingsHelper.SetValue("UseMicaMenus", "rshell.desktop", value);
}