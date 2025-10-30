using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
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
        ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
    }

    partial void OnShowBlurAndGlowChanged(bool value)
    {
        SettingsManager.SetValue("ShowBlurAndGlow", "rebound", value);
    }
}