// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using Rebound.Core.UI;
using System.Collections.ObjectModel;

namespace Rebound.Forge;

/// <summary>
/// The category of a <see cref="Mod"/>.
/// </summary>
public enum ModCategory
{
    /// <summary>
    /// Mods that do not have a defined category.
    /// </summary>
    General,

    /// <summary>
    /// Productivity apps and tools, such as WordPad.
    /// </summary>
    Productivity,

    /// <summary>
    /// System admin tools, such as MMC.
    /// </summary>
    SystemAdministration,

    /// <summary>
    /// Customization mods, usually for aesthetics.
    /// </summary>
    Customization,

    /// <summary>
    /// Various extra tools and utilities that are not essential, but rather quality of life.
    /// </summary>
    Extras,

    /// <summary>
    /// Any mod that is sideloaded inside Rebound. Mustn't be used for primary Rebound mods.
    /// A sideloaded mod can only be in this category.
    /// </summary>
    Sideloaded
}

/// <summary>
/// Definition class for a system modification that the Rebound Forge can interpret and
/// install based on given instructions.
/// </summary>
#pragma warning disable CA1716 // Identifiers should not match keywords
public partial class Mod : ObservableObject
#pragma warning restore CA1716 // Identifiers should not match keywords
{
    [ObservableProperty] public partial bool IsInstalled { get; set; } = false;
    [ObservableProperty] public partial bool IsIntact { get; set; } = true;
    [ObservableProperty] public partial bool IsLoading { get; set; } = false;
    [ObservableProperty] public partial int Progress { get; set; }
    [ObservableProperty] public partial int TaskCount { get; set; }

    /// <summary>
    /// The display name of a mod.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The mod's description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The mod's icon that is displayed in Rebound Hub.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Describes to the user what this mod does behind the scenes. This
    /// will be deprecated soon in favor of dynamically generated installation
    /// steps from the <see cref="Cogs"/>.
    /// </summary>
    [Obsolete("This property is obsolete. Installation steps are now automatically generated from cogs.", true)]
    public string? InstallationSteps { get; set; }

    /// <summary>
    /// The mod's category.
    /// </summary>
    public ModCategory Category { get; set; }

    /// <summary>
    /// Defines to which installation template the mod belongs. Installation
    /// templates allow for installing multiple mods at once.
    /// </summary>
    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    /// <summary>
    /// The tasks that this mod does in the system. Can be anything from file
    /// operations to registry tweaks.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public ObservableCollection<ICog> Cogs { get; set; } = [];

    /// <summary>
    /// The settings that are displayed in the mod's page in Rebound Hub.
    /// </summary>
    public ObservableCollection<IModItem> Settings { get; set; } = [];

    /// <summary>
    /// Launcher tasks to do upon calling <see cref="OpenAsync"/>.
    /// </summary>
    public ObservableCollection<ILauncher> Launchers { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    /// <summary>
    /// Creates a new instance of the <see cref="Mod"/> class.
    /// </summary>
    public Mod() { }

    /// <summary>
    /// Triggers <see cref="ICog.ApplyAsync"/> on every <see cref="ICog"/> from <see cref="Cogs"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task InstallAsync()
    {
        await RunTask(true).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggers <see cref="ICog.RemoveAsync"/> on every <see cref="ICog"/> from <see cref="Cogs"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task UninstallAsync()
    {
        await RunTask(false).ConfigureAwait(false);
    }

    private async Task RunTask(bool install)
    {
        // Prevent concurrent operations on the same mod
        await _operationLock.WaitAsync().ConfigureAwait(false);

        try
        {
            // Validate collections
            if (Cogs == null || Cogs.Count == 0)
            {
                ReboundLogger.Log($"[Mod] Cannot {(install ? "install" : "uninstall")} {Name} - no cogs defined");
                return;
            }

            // Progress trackers
            int taskCount = Cogs.Count(c => !c.Ignorable);
            int progress = 0;

            // Initialize
            ReboundLogger.Log($"[Mod] {(install ? "Installing" : "Uninstalling")} {Name}...");
            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = true;
                TaskCount = taskCount;
                Progress = 0;
                return Task.CompletedTask;
            });

            // Go through each cog and (un)install it
            foreach (var cog in Cogs)
            {
                try
                {
                    if (install)
                        await cog.ApplyAsync().ConfigureAwait(false);
                    else
                        await cog.RemoveAsync().ConfigureAwait(false);

                    if (!cog.Ignorable)
                    {
                        progress++;
                        UIThreadQueue.QueueAction(() =>
                        {
                            Progress = progress;
                            return Task.CompletedTask;
                        });
                    }
                }
                catch (Exception ex)
                {
                    ReboundLogger.Log($"[Mod] Cog operation failed for {Name}", ex);
                    // Continue with other cogs even if one fails
                }
            }

            // Update mod integrity
            await UpdateIntegrityAsync().ConfigureAwait(false);

            // Log
            ReboundLogger.Log($"[Mod] {(install ? "Installation" : "Uninstallation")} finished for {Name}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] {(install ? "InstallAsync" : "UninstallAsync")} failed for {Name}", ex);
        }
        finally
        {
            // Finish progress tracking
            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = false;
                Progress = 0;
                TaskCount = 0;
                return Task.CompletedTask;
            });

            _operationLock.Release();
        }
    }

    /// <summary>
    /// Launches every object inside <see cref="Launchers"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task OpenAsync()
    {
        if (Launchers == null || Launchers.Count == 0)
        {
            ReboundLogger.Log($"[Mod] Cannot open {Name} - no launchers defined");
            return;
        }

        foreach (var launcher in Launchers)
        {
            try
            {
                await launcher.LaunchAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[Mod] Launcher failed for {Name}", ex);
            }
        }
    }

    /// <summary>
    /// Same as <see cref="InstallAsync"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task RepairAsync() => await InstallAsync().ConfigureAwait(false);

    /// <summary>
    /// Updates the <see cref="IsInstalled"/> and <see cref="IsIntact"/> properties.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task<(bool Installed, bool Intact)> UpdateIntegrityAsync()
    {
        // Validate collections
        if (Cogs == null || Cogs.Count == 0)
        {
            UIThreadQueue.QueueAction(() =>
            {
                IsInstalled = false;
                IsIntact = true;
                return Task.CompletedTask;
            });

            return (false, true);
        }

        try
        {
            // Set loading state
            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = true;
                return Task.CompletedTask;
            });

            // Get non-ignorable cogs
            var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
            int totalLocal = nonIgnorableCogs.Count;

            if (totalLocal == 0)
            {
                UIThreadQueue.QueueAction(() =>
                {
                    IsInstalled = false;
                    IsIntact = true;
                    IsLoading = false;
                    return Task.CompletedTask;
                });

                return (false, true);
            }

            // Get the results
            var results = await Task.WhenAll(nonIgnorableCogs.Select(c => c.IsAppliedAsync())).ConfigureAwait(false);
            int intactLocal = results.Count(applied => applied);

            bool installed = intactLocal != 0;
            bool intact = intactLocal == 0 || intactLocal == totalLocal;

            // Update properties on the UI thread in a single batch
            UIThreadQueue.QueueAction(() =>
            {
                IsInstalled = installed;
                IsIntact = intact;
                IsLoading = false;
                return Task.CompletedTask;
            });

            // Log mod integrity
            ReboundLogger.Log($"[Mod] Updated integrity for {Name}: Installed={installed}, Intact={intact}, intactItems={intactLocal}, totalItems={totalLocal}");

            return (installed, intact);
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] UpdateIntegrityAsync failed for {Name}", ex);

            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = false;
                return Task.CompletedTask;
            });

            return (false, false);
        }
    }

    /// <summary>
    /// Checks the integrity of the current mod.
    /// </summary>
    /// <returns>A value describing the integrity state of the mod.</returns>
    public async Task<ModIntegrity> GetIntegrityAsync()
    {
        if (Cogs == null || Cogs.Count == 0)
            return ModIntegrity.NotInstalled;

        try
        {
            var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
            int totalItems = nonIgnorableCogs.Count;

            if (totalItems == 0)
                return ModIntegrity.NotInstalled;

            var results = await Task.WhenAll(nonIgnorableCogs.Select(c => c.IsAppliedAsync())).ConfigureAwait(false);
            int intactItems = results.Count(applied => applied);

            return intactItems == totalItems
                ? ModIntegrity.Installed
                : intactItems == 0
                    ? ModIntegrity.NotInstalled
                    : ModIntegrity.Corrupt;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] GetIntegrityAsync failed for {Name}", ex);
            return ModIntegrity.NotInstalled;
        }
    }
}