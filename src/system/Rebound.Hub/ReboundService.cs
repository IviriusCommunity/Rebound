// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Forge;
using Rebound.Forge.Engines;
using Rebound.Forge.Mods;
using Rebound.Hub.Cards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Hub.ViewModels;

internal partial class ReboundService : ObservableObject
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
    private bool _isInitialized;

    public async Task LoadModsStatesAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () => IsLoading = true);

            if (Catalog.MandatoryMods == null || Catalog.Mods == null)
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    IsLoading = false;
                    IsReboundEnabled = false;
                });
                return;
            }

            // Run all UpdateIntegrityAsync on UI thread and await them
            var mandatoryResults = await Task.WhenAll(Catalog.MandatoryMods.Select(mod => mod.UpdateIntegrityAsync())).ConfigureAwait(true);

            bool enabled = mandatoryResults.All(r => r.Installed && r.Intact);

            await Task.WhenAll(Catalog.Mods.Select(mod => mod.UpdateIntegrityAsync())).ConfigureAwait(true);

            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                IsReboundEnabled = enabled;
                HasSideloadedMods = Catalog.SideloadedMods?.Count > 0;
                IsLoading = false;
            });

            _isInitialized = true;
        }
        catch
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                IsLoading = false;
                IsReboundEnabled = false;
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
            return;

        try
        {
            if (IsReboundEnabled)
            {
                var repairTasks = Catalog.MandatoryMods
                    .Where(mod => !mod.IsIntact)
                    .Select(async mod =>
                    {
                        try
                        {
                            await mod.RepairAsync().ConfigureAwait(false);
                        }
                        catch
                        {

                        }
                    })
                    .ToList();

                await Task.WhenAll(repairTasks).ConfigureAwait(false);
            }
            foreach (var mod in Catalog.MandatoryMods)
                mod.OnConfirmationRequested += Mod_OnConfirmationRequested;
            foreach (var mod in Catalog.Mods)
                mod.OnConfirmationRequested += Mod_OnConfirmationRequested;
            foreach (var mod in Catalog.SideloadedMods)
                mod.OnConfirmationRequested += Mod_OnConfirmationRequested;
        }
        catch
        {

        }
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            if (await ReboundPresenceEngine.IsReboundInstalledAsync().ConfigureAwait(true))
            {
                var version = await File.ReadAllTextAsync(Variables.ReboundCurrentVersionFile).ConfigureAwait(true);

                var updateAvailable = Variables.ReboundVersion != version;
                var versionText = $"Current version: {version}  -  New version: {Variables.ReboundVersion}";

                CurrentVersion = version;
                IsUpdateAvailable = updateAvailable;
                VersionText = versionText;

                if (updateAvailable)
                {
                    ReboundLogger.WriteToLog(
                        "Rebound Service",
                        $"Update available: {version} -> {Variables.ReboundVersion}");
                }
                else
                {
                    ReboundLogger.WriteToLog(
                        "Rebound Service",
                        "Already on latest version, no update needed.");
                }
            }
            else
            {
                ReboundLogger.WriteToLog(
                    "Rebound Service",
                    "Version file not found, creating new version file...");

                await File.WriteAllTextAsync(Variables.ReboundCurrentVersionFile, Variables.ReboundVersion).ConfigureAwait(true);

                CurrentVersion = Variables.ReboundVersion;
                IsUpdateAvailable = false;

                ReboundLogger.WriteToLog(
                    "Rebound Service",
                    $"Version file created with version: {Variables.ReboundVersion}");
                ReboundLogger.WriteToLog(
                    "Rebound Service",
                    "Treating as fresh install, no update available.");
            }

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Update check completed successfully.");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Update check failed with exception.",
                LogMessageSeverity.Error,
                ex);

            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                IsUpdateAvailable = false;
                VersionText = "Unable to check for updates";
            });

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Set IsUpdateAvailable to false due to error.");
        }
    }

    [RelayCommand]
    public static async Task InstallTemplateAsync(string templStr)
    {
        // Uninstall all mods first
        foreach (var mod in Catalog.Mods)
        {
            await mod.UninstallAsync().ConfigureAwait(true);
        }

        // Parse input to template enum
        var template = templStr switch
        {
            "Basic" => InstallationTemplate.Basic,
            "Recommended" => InstallationTemplate.Recommended,
            "Complete" => InstallationTemplate.Complete,
            "Extras" => InstallationTemplate.Extras,
            _ => InstallationTemplate.Basic
        };

        // Define hierarchy of templates included per selected template
        HashSet<InstallationTemplate> includedTemplates = template switch
        {
            InstallationTemplate.Basic => [InstallationTemplate.Basic],
            InstallationTemplate.Recommended => [InstallationTemplate.Basic, InstallationTemplate.Recommended],
            InstallationTemplate.Complete => [InstallationTemplate.Basic, InstallationTemplate.Recommended, InstallationTemplate.Complete],
            InstallationTemplate.Extras => [InstallationTemplate.Basic, InstallationTemplate.Recommended, InstallationTemplate.Complete, InstallationTemplate.Extras],
            _ => [InstallationTemplate.Basic]
        };

        // Filter mods that match included templates and install them
        foreach (var mod in Catalog.Mods.Where(m => includedTemplates.Contains(m.PreferredInstallationTemplate)))
        {
            await mod.InstallAsync(new()).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    public async Task UpdateOrRepairAllAsync()
    {
        try
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () => IsLoading = true);

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Starting UpdateOrRepairAllAsync");

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
                            await m.UninstallAsync().ConfigureAwait(false);
                            await m.InstallAsync(new()).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "Rebound Service",
                                $"Update failed for {m.Name}",
                                LogMessageSeverity.Error,
                                ex);
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
                            await m.InstallAsync(new()).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "Rebound Service",
                                $"Update failed for mandatory mod {m.Name}",
                                LogMessageSeverity.Error,
                                ex);
                        }
                    }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await File.WriteAllTextAsync(Variables.ReboundCurrentVersionFile, Variables.ReboundVersion).ConfigureAwait(true);
            await CheckForUpdatesAsync().ConfigureAwait(true);

            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                IsReboundEnabled = true;
                IsLoading = false;
            });

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "UpdateOrRepairAllAsync completed");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "UpdateOrRepairAllAsync failed", 
                LogMessageSeverity.Error,
                ex);
        }
    }

    [RelayCommand]
    public static async Task ViewLogFileAsync()
    {
        try
        {
            await Launcher.LaunchUriAsync(new(Variables.ReboundLogFile));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                $"Failed to open log file.",
                LogMessageSeverity.Error,
                ex);
        }
    }

    [RelayCommand]
    public static void DeleteLogFile()
    {
        try
        {
            if (File.Exists(Variables.ReboundLogFile))
            {
                File.Delete(Variables.ReboundLogFile);
                ReboundLogger.WriteToLog(
                    "Rebound Service",
                    "Log file deleted");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                $"Failed to delete log file.",
                LogMessageSeverity.Error,
                ex);
        }
    }

    [RelayCommand]
    public async Task ToggleReboundAsync()
    {
        try
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () => IsLoading = true);

            if (IsReboundEnabled)
            {
                await DisableReboundAsync().ConfigureAwait(false);
                App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    IsReboundEnabled = false;
                    IsLoading = false;
                });
            }
            else
            {
                await EnableRebound().ConfigureAwait(false);
                App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
                {
                    IsReboundEnabled = true;
                    IsLoading = false;
                });
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                $"ToggleReboundAsync failed.",
                LogMessageSeverity.Error,
                ex);
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () => IsLoading = false);
        }
    }

    [RelayCommand]
    public async Task EnableRebound()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Enabling Rebound");

            if (Catalog.MandatoryMods == null)
            {
                ReboundLogger.WriteToLog(
                    "Rebound Service",
                    "ERROR: MandatoryMods is null");
                return;
            }

            var tasks = Catalog.MandatoryMods
                .Select(async mod =>
                {
                    try
                    {
                        await mod.InstallAsync(new()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ReboundLogger.WriteToLog(
                            "Rebound Service",
                            $"Failed to install mandatory mod {mod.Name}",
                            LogMessageSeverity.Error,
                            ex);
                    }
                })
                .ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await LoadModsStatesAsync().ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Rebound enabled");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "EnableRebound failed", 
                LogMessageSeverity.Error,
                ex);
        }
    }

    private void Mod_OnConfirmationRequested(object? sender, ModConfirmationPromptEventArgs e)
    {
        try
        {
            var mod = (Mod?)sender;

            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                var title = e.CogTypeName switch
                {
                    "ProcessLaunchCog" => "Process Launcher",
                    "ProcessKillCog" => "Process Kill",
                    _ => "Cog Operation"
                };

                var description = e.CogTypeName switch
                {
                    "ProcessLaunchCog" => $"Allow {mod?.Name} to launch a process in the background?",
                    "ProcessKillCog" => $"Allow {mod?.Name} to kill a currently running process?",
                    _ => "This mod requires your consent to perform an operation."
                };

                var dialog = new ContentDialog()
                {
                    Title = title,
                    Content = description,
                    PrimaryButtonText = "Allow",
                    CloseButtonText = "Do not allow",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = App.MainWindow?.Content.XamlRoot
                };
                var result = await dialog.ShowAsync();
                e.CogArgs.UserResponse.SetResult(result == ContentDialogResult.Primary);
            });
        }
        catch
        {
            e.CogArgs.UserResponse.SetResult(false);
        }
    }

    [RelayCommand]
    public async Task DisableReboundAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Disabling Rebound");

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
                            ReboundLogger.WriteToLog(
                                "Rebound Service",
                                $"Failed to uninstall mod {mod.Name}",
                                LogMessageSeverity.Error,
                                ex);
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
                            ReboundLogger.WriteToLog(
                                "Rebound Service",
                                $"Failed to uninstall mandatory mod {mod.Name}",
                                LogMessageSeverity.Error,
                                ex);
                        }
                    }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            await LoadModsStatesAsync().ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "Rebound Service",
                "Rebound disabled");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Rebound Service",
                "DisableReboundAsync failed",
                LogMessageSeverity.Error,
                ex);
        }
    }
}