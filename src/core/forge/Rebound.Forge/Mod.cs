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

    [ObservableProperty] public partial bool IsLoading { get; set; }
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
    public ObservableCollection<IModItem>? Settings { get; set; } = [];

    /// <summary>
    /// Launcher tasks to do upon calling <see cref="OpenAsync"/>.
    /// </summary>
    public ObservableCollection<ILauncher> Launchers { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    private readonly Lock integrityLock = new();

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
        try
        {
            await Task.Run(async () =>
            {
                // Progress trackers
                int taskCount = Cogs.Count(c => !c.Ignorable);
                int progress = 0;

                // Initialize
                ReboundLogger.Log($"[Mod] {(install ? "Installing" : "Uninstalling")} {Name}...");
                UIThreadQueue.QueueAction(async () =>
                {
                    IsLoading = true;
                });

                // Go through each cog and (un)install it
                UIThreadQueue.QueueAction(async () =>
                {
                    TaskCount = taskCount;
                });

                foreach (var cog in Cogs)
                {
                    if (install)
                        await cog.ApplyAsync().ConfigureAwait(false);
                    else
                        await cog.RemoveAsync().ConfigureAwait(false);

                    await Task.Delay(50).ConfigureAwait(false);

                    if (!cog.Ignorable)
                    {
                        progress++;
                        UIThreadQueue.QueueAction(async () =>
                        {
                            Progress = progress;
                        });
                    }
                }

                // Update mod integrity
                await UpdateIntegrityAsync().ConfigureAwait(false);

                // Log
                ReboundLogger.Log($"[Mod] {(install ? "Installation" : "Uninstallation")} finished for {Name}");
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] {(install ? "InstallAsync" : "UninstallAsync")} failed for {Name}", ex);
        }
        finally
        {
            // Finish progress tracking
            UIThreadQueue.QueueAction(async () =>
            {
                IsLoading = false;
                Progress = 0;
                TaskCount = 0;
            });
        }
    }

    /// <summary>
    /// Launches every object inside <see cref="Launchers"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task OpenAsync()
    {
        foreach (var launcher in Launchers)
        {
            await launcher.LaunchAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Same as <see cref="InstallAsync"/>.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task RepairAsync() => await InstallAsync().ConfigureAwait(true);

    /// <summary>
    /// Updates the <see cref="IsInstalled"/> and <see cref="IsIntact"/> properties.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task<(bool Installed, bool Intact)> UpdateIntegrityAsync()
    {
        var values = await Task.Run(async () =>
        {
            // Progress trackers
            int intactLocal = 0;
            int totalLocal = 0;

            // Thread lock
            lock (integrityLock)
            {
                var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
                totalLocal = nonIgnorableCogs.Count;
            }

            // Get the results
            var results = await Task.WhenAll(Cogs.Where(c => !c.Ignorable).Select(c => c.IsAppliedAsync())).ConfigureAwait(false);
            intactLocal = results.Count(applied => applied);

            // Update properties on the UI thread
            UIThreadQueue.QueueAction(async () =>
            {
                IsInstalled = intactLocal != 0;
                IsIntact = intactLocal == 0 || intactLocal == totalLocal;
            });

            return (intactLocal, totalLocal);
        }).ConfigureAwait(false);

        // Log mod integrity
        ReboundLogger.Log($"[Mod] Updated integrity for {Name}: Installed={(values.intactLocal != 0)}, Intact={(values.intactLocal == 0 || values.intactLocal == values.totalLocal)}, intactItems={values.intactLocal}, totalItems={values.totalLocal}");

        return (values.intactLocal != 0, values.intactLocal == 0 || values.intactLocal == values.totalLocal);
    }

    /// <summary>
    /// Checks the integrity of the current mod.
    /// </summary>
    /// <returns>A value describing the integrity state of the mod.</returns>
    public async Task<ModIntegrity> GetIntegrityAsync()
    {
        if (Cogs == null)
            return ModIntegrity.NotInstalled;

        var nonIgnorableCogs = Cogs.Where(c => !c.Ignorable).ToList();
        int totalItems = nonIgnorableCogs.Count;

        var results = await Task.WhenAll(nonIgnorableCogs.Select(c => c.IsAppliedAsync())).ConfigureAwait(false);
        int intactItems = results.Count(applied => applied);

        return intactItems == totalItems
            ? ModIntegrity.Installed
            : intactItems == 0
                ? ModIntegrity.NotInstalled
                : ModIntegrity.Corrupt;
    }
}