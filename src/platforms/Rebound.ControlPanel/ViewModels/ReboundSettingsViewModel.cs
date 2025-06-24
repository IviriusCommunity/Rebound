using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Helpers;

namespace Rebound.ControlPanel.ViewModels;

public partial class ReboundSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowBlurAndGlow { get; set; }

    [ObservableProperty]
    public partial bool FetchMode { get; set; }

    public ReboundSettingsViewModel()
    {
        ShowBlurAndGlow = SettingsHelper.GetValue("ShowBlurAndGlow", "rebound", true);
        FetchMode = SettingsHelper.GetValue("FetchMode", "rebound", false);
    }

    partial void OnShowBlurAndGlowChanged(bool value)
    {
        SettingsHelper.SetValue("ShowBlurAndGlow", "rebound", value);
    }

    partial void OnFetchModeChanged(bool value)
    {
        SettingsHelper.SetValue("FetchMode", "rebound", value);
    }
}