// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rebound.Installer;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int CurrentTaskProgress { get; set; }

    [ObservableProperty]
    public partial string CurrentTaskText { get; set; }

    [ObservableProperty]
    public partial int TotalTasks { get; set; }

    [ObservableProperty]
    public partial int SelectedAction { get; set; } = 0;

    [ObservableProperty]
    public partial string CurrentPage { get; set; } = "Main";

    [ObservableProperty]
    public partial bool DeleteAppData { get; set; }

    [ObservableProperty]
    public partial bool Success { get; set; }

    [ObservableProperty]
    public partial bool IsOldReboundInstalled { get; set; } = false;

    public readonly string oldReboundHubFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "ReboundHub");

    public MainViewModel()
    {
        if (Directory.Exists(oldReboundHubFolder))
        {
            IsOldReboundInstalled = true;
            SelectedAction = 3;
        }
    }

    public async Task RunActionAsync()
    {
        switch (SelectedAction + 1)
        {
            case 1:
                // Install Rebound
                {
                    TotalTasks = 
                        Catalog.MandatoryMods.Count + 
                        1; // Rebound Hub
                    CurrentTaskProgress = 0;
                    CurrentTaskText = "Installing Rebound...";

                    DistributionEngine.InstallReboundHubCertificate();

                    foreach (var mod in Catalog.MandatoryMods)
                    {
                        CurrentTaskText = $"Installing {mod.Name}...";
                        try
                        {
                            await mod.InstallAsync();
                            CurrentTaskText = $"Installed {mod.Name}";
                        }
                        catch (Exception ex)
                        {
                            CurrentTaskText = $"Failed to install {mod.Name}: {ex.Message}";
                            ReboundLogger.Log("[ReboundInstaller] Failed to install mod " + mod.Name, ex);
                        }
                        CurrentTaskProgress++;
                    }
                    CurrentTaskText = $"Installing Rebound Hub...";
                    await Catalog.ReboundHub.InstallAsync();
                    CurrentTaskText = $"Installed Rebound Hub";
                    CurrentTaskProgress++;
                }
                break;
            case 2:
                // Install Rebound Hub
                {
                    TotalTasks = 1; // Rebound Hub
                    CurrentTaskProgress = 0;
                    CurrentTaskText = $"Installing Rebound Hub...";
                    await Catalog.ReboundHub.InstallAsync();
                    CurrentTaskText = $"Installed Rebound Hub";
                    CurrentTaskProgress++;
                }
                break;
            case 3:
                // Upgrade or repair
                {
                    TotalTasks = 1; // Rebound Hub
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
                    CurrentTaskText = "Upgrading/repairing Rebound...";

                    foreach (var mod in Catalog.MandatoryMods)
                    {
                        if (mod.IsInstalled || !mod.IsIntact)
                        {
                            CurrentTaskText = $"Upgrading/repairing {mod.Name}...";
                            await mod.RepairAsync();
                            CurrentTaskText = $"Upgraded/repaired {mod.Name}";
                            CurrentTaskProgress++;
                        }
                    }
                    foreach (var mod in Catalog.Mods)
                    {
                        if (mod.IsInstalled || !mod.IsIntact)
                        {
                            CurrentTaskText = $"Upgrading/repairing {mod.Name}...";
                            await mod.RepairAsync();
                            CurrentTaskText = $"Upgraded/repaired {mod.Name}";
                            CurrentTaskProgress++;
                        }
                    }
                    foreach (var mod in Catalog.SideloadedMods)
                    {
                        if (mod.IsInstalled || !mod.IsIntact)
                        {
                            CurrentTaskText = $"Upgrading/repairing {mod.Name}...";
                            await mod.RepairAsync();
                            CurrentTaskText = $"Upgraded/repaired {mod.Name}";
                            CurrentTaskProgress++;
                        }
                    }

                    CurrentTaskText = $"Upgrading/repairing Rebound Hub...";
                    await Catalog.ReboundHub.RepairAsync();
                    CurrentTaskText = $"Upgraded/repaired Rebound Hub";
                    CurrentTaskProgress++;
                }
                break;
            case 4:
                // Upgrade from Rebound v0.0.10
                {
                    TotalTasks =
                        Catalog.MandatoryMods.Count +
                        1 + // Deleting old Rebound
                        1; // Rebound Hub
                    CurrentTaskProgress = 0;
                    CurrentTaskText = "Installing Rebound...";

                    CurrentTaskText = $"Removing old Rebound installation...";
                    LegacyReboundRemover.DeleteOldRebound();
                    CurrentTaskText = $"Removed old Rebound installation";
                    CurrentTaskProgress++;

                    DistributionEngine.InstallReboundHubCertificate();

                    foreach (var mod in Catalog.MandatoryMods)
                    {
                        CurrentTaskText = $"Installing {mod.Name}...";
                        await mod.InstallAsync();
                        CurrentTaskText = $"Installed {mod.Name}";
                        CurrentTaskProgress++;
                    }

                    CurrentTaskText = $"Installing Rebound Hub...";
                    await Catalog.ReboundHub.InstallAsync();
                    CurrentTaskText = $"Installed Rebound Hub";
                    CurrentTaskProgress++;
                }
                break;
            case 5:
                // Uninstall Rebound v0.0.10
                {
                    TotalTasks = 1; // Deleting old Rebound
                    CurrentTaskProgress = 0;
                    CurrentTaskText = "Uninstalling Rebound v0.0.10...";

                    CurrentTaskText = $"Removing old Rebound installation...";
                    LegacyReboundRemover.DeleteOldRebound();
                    CurrentTaskText = $"Removed old Rebound installation";
                    CurrentTaskProgress++;
                }
                break;
            case 6:
                // Uninstall
                {
                    TotalTasks = 1; // Rebound Hub
                    foreach (var mod in Catalog.MandatoryMods)
                    {
                        TotalTasks++;
                    }
                    foreach (var mod in Catalog.Mods)
                    {
                        TotalTasks++;
                    }
                    foreach (var mod in Catalog.SideloadedMods)
                    {
                        TotalTasks++;
                    }

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
                break;
            default:
                break;
        }
        Success = true;
    }
}
