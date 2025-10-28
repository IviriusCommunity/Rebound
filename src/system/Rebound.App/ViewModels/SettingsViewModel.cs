using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rebound.Hub.ViewModels;

internal partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }

    public SettingsViewModel()
    {
        ShowBlurAndGlow = SettingsHelper.GetValue("ShowBlurAndGlow", "rebound", true);
    }

    partial void OnShowBlurAndGlowChanged(bool value)
    {
        SettingsHelper.SetValue("ShowBlurAndGlow", "rebound", value);
    }
}