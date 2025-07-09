using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Helpers;

namespace Rebound.Shell.Desktop;

[ObservableObject]
public partial class DesktopViewModel
{
    [ObservableProperty] public partial bool IsLivelyCompatibilityEnabled { get; set; }
    [ObservableProperty] public partial bool ShowClockWidget { get; set; }

    public DesktopViewModel()
    {
        IsLivelyCompatibilityEnabled = SettingsHelper.GetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", false);
        ShowClockWidget = SettingsHelper.GetValue("ShowClockWidget", "rshell.desktop", true);
    }

    partial void OnIsLivelyCompatibilityEnabledChanged(bool value) => SettingsHelper.SetValue("IsLivelyCompatibilityEnabled", "rshell.desktop", value);
    partial void OnShowClockWidgetChanged(bool value) => SettingsHelper.SetValue("ShowClockWidget", "rshell.desktop", value);
}