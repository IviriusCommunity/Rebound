// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Hub.Cards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.ApplicationModel;
using Windows.System;

namespace Rebound.Hub.ViewModels;

public partial class ReboundService : ObservableObject
{
    public ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Get Started",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/Glyphs/DesktopVerify.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/Glyphs/WinUI.png",
            Link = "https://learn.microsoft.com/en-us/windows/apps/winui/winui3/"
        },
        new LinkCard
        {
            Title = "Windows updates",
            Description = "Rebound does not disable Windows updates so you can enjoy fresh patches and releases.",
            IconPath = "/Assets/Glyphs/Update.ico",
            Link = "https://support.microsoft.com/en-us/windows/install-windows-updates-3c5ae7fc-9fb6-9af1-1984-b5e0412c556a"
        },
        new LinkCard
        {
            Title = "Rebound updates",
            Description = "All Rebound updates are easy to install via the \"Update or Repair all\" option.",
            IconPath = "/Assets/Glyphs/Restart.ico",
            Link = "https://ivirius.com/rebound"
        },
        new LinkCard
        {
            Title = "GitHub",
            Description = "Star the repo and contribute to the project!",
            IconPath = "/Assets/Glyphs/GitHub.png",
            Link = "https://github.com/IviriusCommunity/Rebound"
        }
    ];

    [ObservableProperty] public partial bool IsReboundEnabled { get; set; }
    [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; } = true;
    [ObservableProperty] public partial string VersionText { get; set; } = "";
    [ObservableProperty] public partial string CurrentVersion { get; set; } = "";
    [ObservableProperty] public partial bool HasSideloadedMods { get; set; }

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized = false;

    public async Task LoadModsStatesAsync()
    {
        // Prevent concurrent initialization
        await _initializationLock.WaitAsync().ConfigureAwait(false);

        try
        {
            ReboundLogger.Log("[ReboundService] Starting LoadModsStatesAsync");

            // Validate catalog exists
            if (Catalog.MandatoryMods == null || Catalog.Mods == null)
            {
                ReboundLogger.Log("[ReboundService] ERROR: Catalog not initialized properly");
                UIThreadQueue.QueueAction(() =>
                {
                    IsLoading = false;
                    IsReboundEnabled = false;
                    return Task.CompletedTask;
                });
                return;
            }

            // Run integrity checks for mandatory mods in parallel
            var mandatoryTasks = Catalog.MandatoryMods
                .Select(async mod =>
                {
                    try
                    {
                        var result = await mod.UpdateIntegrityAsync().ConfigureAwait(false);
                        ReboundLogger.Log($"[ReboundService] Integrity check for {mod.Name}: IsInstalled={result.Installed}, IsIntact={result.Intact}");
                        return (mod, result, success: true);
                    }
                    catch (Exception ex)
                    {
                        ReboundLogger.Log($"[ReboundService] Integrity check failed for {mod.Name}", ex);
                        return (mod, (Installed: false, Intact: false), success: false);
                    }
                })
                .ToList();

            var mandatoryResults = await Task.WhenAll(mandatoryTasks).ConfigureAwait(false);

            // Determine enabled state based on all results
            bool enabled = mandatoryResults.All(x => x.success && x.Item2.Installed && x.Item2.Intact);

            if (!enabled)
            {
                foreach (var (mod, r, success) in mandatoryResults.Where(x => !x.success || !x.Item2.Installed || !x.Item2.Intact))
                {
                    ReboundLogger.Log($"[ReboundService] Rebound disabled due to mod {mod.Name}: Installed={r.Installed}, Intact={r.Intact}, Success={success}");
                }
            }

            ReboundLogger.Log($"[ReboundService] Rebound enabled state: {enabled}");

            // Update UI state in a single batch
            UIThreadQueue.QueueAction(() =>
            {
                IsReboundEnabled = enabled;
                HasSideloadedMods = Catalog.SideloadedMods?.Count > 0;
                IsLoading = false;
                return Task.CompletedTask;
            });

            // Now run integrity checks for all other mods in parallel
            var allModTasks = Catalog.Mods
                .Select(async mod =>
                {
                    try
                    {
                        await mod.UpdateIntegrityAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ReboundLogger.Log($"[ReboundService] Integrity check failed for mod {mod.Name}", ex);
                    }
                })
                .ToList();

            await Task.WhenAll(allModTasks).ConfigureAwait(false);

            _isInitialized = true;
            ReboundLogger.Log("[ReboundService] LoadModsStatesAsync completed");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] LoadModsStatesAsync failed", ex);

            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = false;
                IsReboundEnabled = false;
                return Task.CompletedTask;
            });
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            ReboundLogger.Log("[ReboundService] InitializeAsync called before LoadModsStatesAsync completed");
            return;
        }

        try
        {
            if (IsReboundEnabled)
            {
                ReboundLogger.Log("[ReboundService] Running automatic repairs");

                var repairTasks = Catalog.MandatoryMods
                    .Where(mod => !mod.IsIntact)
                    .Select(async mod =>
                    {
                        try
                        {
                            await mod.RepairAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[ReboundService] Repair failed for {mod.Name}", ex);
                        }
                    })
                    .ToList();

                await Task.WhenAll(repairTasks).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] InitializeAsync failed", ex);
        }
    }

    public void CheckForUpdates()
    {
        try
        {
            var version = $"{Package.Current.GetAppInstallerInfo().Version.Major}.{Package.Current.GetAppInstallerInfo().Version.Minor}.{Package.Current.GetAppInstallerInfo().Version.Revision}.{Package.Current.GetAppInstallerInfo().Version.Build}";
            CurrentVersion = version;
            VersionText = $"Current version: {version}  -  New version: {Variables.ReboundVersion}";
            IsUpdateAvailable = Variables.ReboundVersion != version;

            ReboundLogger.Log($"[ReboundService] Version check: Current={version}, Available={Variables.ReboundVersion}, UpdateAvailable={IsUpdateAvailable}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] Failed to check for updates.", ex);
            IsUpdateAvailable = false;
        }
    }

    [RelayCommand]
    public async Task UpdateOrRepairAllAsync()
    {
        try
        {
            ReboundLogger.Log("[ReboundService] Starting UpdateOrRepairAllAsync");

            var tasks = new List<Task>();

            // Add installed mod updates
            if (Catalog.Mods != null)
            {
                tasks.AddRange(Catalog.Mods
                    .Where(m => m.IsInstalled)
                    .Select(async m =>
                    {
                        try
                        {
                            await m.InstallAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[ReboundService] Update failed for {m.Name}", ex);
                        }
                    }));
            }

            // Add mandatory mod updates
            if (Catalog.MandatoryMods != null)
            {
                tasks.AddRange(Catalog.MandatoryMods
                    .Select(async m =>
                    {
                        try
                        {
                            await m.InstallAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[ReboundService] Update failed for mandatory mod {m.Name}", ex);
                        }
                    }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            CheckForUpdates();

            ReboundLogger.Log("[ReboundService] UpdateOrRepairAllAsync completed");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] UpdateOrRepairAllAsync failed", ex);
        }
    }

    [RelayCommand]
    public async Task ViewLogFileAsync()
    {
        try
        {
            await Launcher.LaunchUriAsync(new(Variables.ReboundLogFile));
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] Failed to open log file", ex);
        }
    }

    [RelayCommand]
    public void DeleteLogFile()
    {
        try
        {
            if (File.Exists(Variables.ReboundLogFile))
            {
                File.Delete(Variables.ReboundLogFile);
                ReboundLogger.Log("[ReboundService] Log file deleted");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] Failed to delete log file", ex);
        }
    }

    [RelayCommand]
    public async Task ToggleReboundAsync()
    {
        try
        {
            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = true;
                return Task.CompletedTask;
            });

            if (IsReboundEnabled)
            {
                await DisableReboundAsync().ConfigureAwait(false);
                UIThreadQueue.QueueAction(() =>
                {
                    IsReboundEnabled = false;
                    IsLoading = false;
                    return Task.CompletedTask;
                });
            }
            else
            {
                await EnableRebound().ConfigureAwait(false);
                UIThreadQueue.QueueAction(() =>
                {
                    IsReboundEnabled = true;
                    IsLoading = false;
                    return Task.CompletedTask;
                });
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] ToggleReboundAsync failed", ex);

            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = false;
                return Task.CompletedTask;
            });
        }
    }

    [RelayCommand]
    public async Task EnableRebound()
    {
        try
        {
            ReboundLogger.Log("[ReboundService] Enabling Rebound");

            if (Catalog.MandatoryMods == null)
            {
                ReboundLogger.Log("[ReboundService] ERROR: MandatoryMods is null");
                return;
            }

            var tasks = Catalog.MandatoryMods
                .Select(async mod =>
                {
                    try
                    {
                        await mod.InstallAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ReboundLogger.Log($"[ReboundService] Failed to install mandatory mod {mod.Name}", ex);
                    }
                })
                .ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            ReboundLogger.Log("[ReboundService] Rebound enabled");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] EnableRebound failed", ex);
        }
    }

    [RelayCommand]
    public async Task DisableReboundAsync()
    {
        try
        {
            ReboundLogger.Log("[ReboundService] Disabling Rebound");

            var tasks = new List<Task>();

            // Uninstall regular mods
            if (Catalog.Mods != null)
            {
                tasks.AddRange(Catalog.Mods
                    .Select(async mod =>
                    {
                        try
                        {
                            await mod.UninstallAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[ReboundService] Failed to uninstall mod {mod.Name}", ex);
                        }
                    }));
            }

            // Uninstall mandatory mods
            if (Catalog.MandatoryMods != null)
            {
                tasks.AddRange(Catalog.MandatoryMods
                    .Select(async mod =>
                    {
                        try
                        {
                            await mod.UninstallAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[ReboundService] Failed to uninstall mandatory mod {mod.Name}", ex);
                        }
                    }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            ReboundLogger.Log("[ReboundService] Rebound disabled");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundService] DisableReboundAsync failed", ex);
        }
    }
}