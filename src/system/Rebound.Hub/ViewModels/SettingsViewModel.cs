// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rebound.Hub.ViewModels;

internal partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial bool ShowBlurAndGlow { get; set; }

    [ObservableProperty] public partial bool ManageStoreApps { get; set; }

    public SettingsViewModel()
    {
        UIThreadQueue.QueueAction(() =>
        {
            ShowBlurAndGlow = SettingsManager.GetValue("ShowBlurAndGlow", "rebound", true);
            ManageStoreApps = SettingsManager.GetValue("ManageStoreApps", "rebound", true);
        });
    }

    partial void OnShowBlurAndGlowChanged(bool value)
    {
        UpdateSettings();
    }

    partial void OnManageStoreAppsChanged(bool value)
    {
        UpdateSettings();
    }

    private void UpdateSettings()
    {
        UIThreadQueue.QueueAction(() =>
        {
            SettingsManager.SetValue("ShowBlurAndGlow", "rebound", ShowBlurAndGlow);
            SettingsManager.SetValue("ManageStoreApps", "rebound", ManageStoreApps);
        });
    }
}