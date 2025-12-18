// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using Rebound.Forge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Rebound.Uninstaller;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int CurrentTaskProgress { get; set; }

    [ObservableProperty]
    public partial string CurrentTaskText { get; set; }

    [ObservableProperty]
    public partial int TotalTasks { get; set; }

    [ObservableProperty]
    public partial bool ManageStoreApps { get; set; }

    [ObservableProperty]
    public partial bool DeleteAppData { get; set; }

    public MainViewModel()
    {
        ManageStoreApps = SettingsManager.GetValue("ManageStoreApps", "rebound", true);
    }

    partial void OnManageStoreAppsChanged(bool value)
    {
        SettingsManager.SetValue("ManageStoreApps", "rebound", ManageStoreApps);
    }

    public async Task UninstallAsync()
    {
        TotalTasks = Catalog.SideloadedMods.Count + Catalog.Mods.Count + Catalog.MandatoryMods.Count + 1;
        CurrentTaskProgress = 0;
        CurrentTaskText = "Uninstalling Rebound...";

        foreach (var mod in Catalog.SideloadedMods)
        {
            CurrentTaskText = $"Uninstalling {mod.Name}...";
            await mod.UninstallAsync();
            CurrentTaskText = $"Uninstalled {mod.Name}";
            CurrentTaskProgress++;
        }
        foreach (var mod in Catalog.Mods)
        {
            CurrentTaskText = $"Uninstalling {mod.Name}...";
            await mod.UninstallAsync();
            CurrentTaskText = $"Uninstalled {mod.Name}";
            CurrentTaskProgress++;
        }
        foreach (var mod in Catalog.MandatoryMods)
        {
            CurrentTaskText = $"Uninstalling {mod.Name}...";
            await mod.UninstallAsync();
            CurrentTaskText = $"Uninstalled {mod.Name}";
            CurrentTaskProgress++;
        }
        CurrentTaskText = $"Uninstalling Rebound Hub...";
        await Catalog.ReboundHub.UninstallAsync();
        CurrentTaskText = $"Uninstalled Rebound Hub";
        CurrentTaskProgress++;

        if (DeleteAppData)
        {
            Directory.Delete(Variables.ReboundDataFolder, true);
        }
    }
}