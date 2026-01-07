using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;

namespace Rebound.ControlPanel.ViewModels;

public partial class ReboundSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowBlurAndGlow { get; set; }

    [ObservableProperty]
    public partial bool FetchMode { get; set; }

    [ObservableProperty]
    public partial bool AllowDesktopFeature { get; set; }

    [ObservableProperty]
    public partial bool ShowBranding { get; set; }

    public ReboundSettingsViewModel()
    {
        ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
        FetchMode = SettingsManager.GetValue("FetchMode", "rebound", false);
        AllowDesktopFeature = SettingsManager.GetValue("AllowDesktopFeature", "rebound", false);
        ShowBranding = SettingsManager.GetValue("ShowBranding", "rebound", true);
    }

    partial void OnShowBlurAndGlowChanged(bool value)
    {
        SettingsManager.SetValue("ShowBlurAndGlow", "rebound", value);
    }

    partial void OnFetchModeChanged(bool value)
    {
        SettingsManager.SetValue("FetchMode", "rebound", value);
    }

    partial void OnAllowDesktopFeatureChanged(bool value)
    {
        SettingsManager.SetValue("AllowDesktopFeature", "rebound", value);
    }

    partial void OnShowBrandingChanged(bool value)
    {
        SettingsManager.SetValue("ShowBranding", "rebound", value);
    }
}