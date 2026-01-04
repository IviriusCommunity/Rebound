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

namespace Rebound.Updater;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int CurrentTaskProgress { get; set; }

    [ObservableProperty]
    public partial string CurrentTaskText { get; set; }

    [ObservableProperty]
    public partial int TotalTasks { get; set; }

    public async Task UpdateAsync()
    {
        TotalTasks = 
            1 + // Rebound Uninstaller
            1; // Rebound Hub
        foreach (var mod in Catalog.MandatoryMods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
                TotalTasks++;
        }
        foreach (var mod in Catalog.Mods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
                TotalTasks++;
        }
        foreach (var mod in Catalog.SideloadedMods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
                TotalTasks++;
        }

        CurrentTaskProgress = 0;
        CurrentTaskText = "Upgrading Rebound...";

        foreach (var mod in Catalog.MandatoryMods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
            {
                CurrentTaskText = $"Upgrading {mod.Name}...";
                await mod.RepairAsync();
                CurrentTaskText = $"Upgraded {mod.Name}";
                CurrentTaskProgress++;
            }
        }
        foreach (var mod in Catalog.Mods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
            {
                CurrentTaskText = $"Upgrading {mod.Name}...";
                await mod.RepairAsync();
                CurrentTaskText = $"Upgraded {mod.Name}";
                CurrentTaskProgress++;
            }
        }
        foreach (var mod in Catalog.SideloadedMods)
        {
            if (mod.IsInstalled || !mod.IsIntact)
            {
                CurrentTaskText = $"Upgrading {mod.Name}...";
                await mod.RepairAsync();
                CurrentTaskText = $"Upgraded {mod.Name}";
                CurrentTaskProgress++;
            }
        }

        CurrentTaskText = $"Upgrading Rebound Hub...";
        await Catalog.ReboundHub.RepairAsync();
        CurrentTaskText = $"Upgraded Rebound Hub";

        CurrentTaskText = $"Upgrading Rebound Uninstaller...";
        await Catalog.Uninstaller.RepairAsync();
        CurrentTaskText = $"Upgraded Rebound Uninstaller";
        CurrentTaskProgress++;
    }
}