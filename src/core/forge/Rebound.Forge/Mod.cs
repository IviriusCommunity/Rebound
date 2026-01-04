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
    /// Mandatory mods (the ones from <see cref="Catalog.MandatoryMods"/>).
    /// </summary>
    Mandatory,

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
/// Represents a variant of a <see cref="Mod"/>.
/// </summary>
public partial class ModVariant : ObservableObject
{
    /// <summary>
    /// The display name of a mod variant.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The ID of a mod variant. Example: Rebound.About.Default
    /// </summary>
    public string? Id { get; set; }

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
    /// Launcher tasks to do upon opening the mod.
    /// </summary>
    public ObservableCollection<ILauncher> Launchers { get; set; } = [];

    /// <summary>
    /// Dependencies for the current mod. Collection consists of <see cref="Mod.Id"/> strings.
    /// </summary>
    public ObservableCollection<string> Dependencies { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
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
    [ObservableProperty] public partial int SelectedVariantIndex { get; set; } = -1;

    /// <summary>
    /// The display name of a mod.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The ID of a mod. Example: Rebound.About
    /// </summary>
    public string? Id { get; set; }

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
    /// Variants of the current mod.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public ObservableCollection<ModVariant> Variants { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    /// <summary>
    /// Creates a new instance of the <see cref="Mod"/> class.
    /// </summary>
    public Mod() { }

    async partial void OnSelectedVariantIndexChanged(int value)
    {
        if (value < 0 || value >= Variants.Count) return;

        var result = await UpdateIntegrityAsync();
        UIThreadQueue.QueueAction(() =>
        {
            IsInstalled = result.Installed;
            IsIntact = result.Intact;
        });
    }

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

    /// <summary>
    /// Performs the installation or uninstallation process for the currently selected mod variant, including handling
    /// dependencies and updating progress state.
    /// </summary>
    /// <remarks>This method manages progress tracking and logging throughout the operation. It ensures that
    /// dependencies are installed before installation and dependents are uninstalled before uninstallation. The method
    /// is thread-safe and serializes operations using an internal lock. If an error occurs during the process, it is
    /// logged and the operation continues with remaining items.</remarks>
    /// <param name="install">true to install the selected mod variant and its dependencies; false to uninstall the selected mod variant and
    /// its dependents.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunTask(bool install)
    {
        await _operationLock.WaitAsync().ConfigureAwait(false);

        try
        {
            // Validate variant selection
            if (SelectedVariantIndex < 0 || SelectedVariantIndex >= Variants.Count)
            {
                if (install)
                {
                    // For installation, default to the first variant if none selected
                    ReboundLogger.Log($"[Mod] No variant selected for {Name}, defaulting to first variant for installation");
                    SelectedVariantIndex = 0;
                }
                else
                {
                    // For uninstallation, find which variant is installed
                    ReboundLogger.Log($"[Mod] Invalid variant selection for {Name}, attempting to find installed variant...");

                    int installedVariantIndex = -1;
                    for (int i = 0; i < Variants.Count; i++)
                    {
                        var variant = Variants[i];
                        var (installed, _) = await RetrieveIntegrityStatusForModVariant(variant).ConfigureAwait(false);

                        if (installed)
                        {
                            installedVariantIndex = i;
                            ReboundLogger.Log($"[Mod] Found installed variant at index {i} for {Name}");
                            break;
                        }
                    }

                    // If no installed variant found, return
                    if (installedVariantIndex == -1)
                    {
                        ReboundLogger.Log($"[Mod] Cannot uninstall {Name} - no installed variant found");
                        return;
                    }

                    SelectedVariantIndex = installedVariantIndex;
                }
            }

            // Obtain the currently selected variant
            var selectedVariant = Variants[SelectedVariantIndex];

            // Install dependencies before proceeding with installation
            if (install)
            {
                await InstallDependenciesAsync(selectedVariant).ConfigureAwait(false);
            }
            else
            {
                // Uninstall dependents before uninstalling this mod
                await UninstallDependentAsync(selectedVariant).ConfigureAwait(false);
            }

            // Handle every variant
            foreach (var variant in Variants)
            {
                // Validate collections
                if (variant.Cogs == null || variant.Cogs.Count == 0)
                {
                    ReboundLogger.Log($"[Mod] Cannot {(install ? "install" : "uninstall")} {Name} - no cogs defined");
                    return;
                }

                // Progress trackers
                int taskCount1 = variant.Cogs.Count(c => !c.Ignorable);
                int progress1 = 0;

                // Initialize
                ReboundLogger.Log($"[Mod] {(install ? "Installing" : "Uninstalling")} {Name}...");
                UIThreadQueue.QueueAction(() =>
                {
                    IsLoading = true;
                    TaskCount = taskCount1;
                    Progress = 0;
                });

                // Go through each cog and modify it accordingly
                foreach (var cog in variant.Cogs)
                {
                    try
                    {
                        if (install && variant.Id == selectedVariant.Id)
                            await cog.ApplyAsync().ConfigureAwait(false);
                        else
                            await cog.RemoveAsync().ConfigureAwait(false);

                        if (!cog.Ignorable)
                        {
                            progress1++;
                            int currentProgress = progress1; // Capture for closure
                            UIThreadQueue.QueueAction(() =>
                            {
                                Progress = currentProgress;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ReboundLogger.Log($"[Mod] Cog operation failed for {Name}", ex);
                        // Continue with other cogs even if one fails
                    }
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
        if (SelectedVariantIndex == -1) return;

        // Obtain the currently selected variant
        var selectedVariant = Variants[SelectedVariantIndex];

        if (selectedVariant.Launchers == null || selectedVariant.Launchers.Count == 0)
        {
            ReboundLogger.Log($"[Mod] Cannot open {Name} - no launchers defined");
            return;
        }

        foreach (var launcher in selectedVariant.Launchers)
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

    private async Task <(bool Installed, bool Intact)> RetrieveIntegrityStatusForModVariant(ModVariant variant)
    {
        // Set loading state
        UIThreadQueue.QueueAction(() =>
        {
            IsLoading = true;
        });

        // Get non-ignorable cogs
        var nonIgnorableCogs = variant.Cogs.Where(c => !c.Ignorable).ToList();
        int totalLocal = nonIgnorableCogs.Count;

        if (totalLocal == 0)
        {
            UIThreadQueue.QueueAction(() =>
            {
                IsInstalled = false;
                IsIntact = true;
                IsLoading = false;
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
        });

        // Log mod integrity
        ReboundLogger.Log($"[Mod] Updated integrity for {Name} and variant {variant.Name}: Installed={installed}, Intact={intact}, intactItems={intactLocal}, totalItems={totalLocal}");

        return (installed, intact);
    }

    /// <summary>
    /// Installs dependencies for a given variant.
    /// </summary>
    /// <param name="variant">The variant whose dependencies need to be installed.</param>
    /// <returns>A task corresponding to the action.</returns>
    private async Task InstallDependenciesAsync(ModVariant variant)
    {
        if (variant.Dependencies == null || variant.Dependencies.Count == 0)
            return;

        ReboundLogger.Log($"[Mod] Installing dependencies for {Name} (Variant: {variant.Name})...");

        // Collect all mods from the full catalog to search dependencies
        var allMods = new List<Mod>();
        allMods.AddRange(Catalog.MandatoryMods);
        allMods.AddRange(Catalog.Mods);
        allMods.AddRange(Catalog.SideloadedMods);

        foreach (var dependency in variant.Dependencies)
        {
            try
            {
                // Find the dependency mod by ID across the entire catalog
                var dependencyMod = allMods.FirstOrDefault(m => m.Id == dependency);

                if (dependencyMod == null)
                {
                    ReboundLogger.Log($"[Mod] Dependency '{dependency}' not found for {Name}");
                    continue;
                }

                // Check if the dependency is already installed and intact
                var (installed, intact) = await dependencyMod.UpdateIntegrityAsync().ConfigureAwait(false);

                if (installed && intact)
                {
                    ReboundLogger.Log($"[Mod] Dependency '{dependency}' is already installed and intact");
                    continue;
                }

                // Install the dependency if not installed or not intact
                ReboundLogger.Log($"[Mod] Installing dependency '{dependency}' for {Name}");
                await dependencyMod.InstallAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[Mod] Failed to install dependency '{dependency}' for {Name}", ex);
                // Continue with other dependencies even if one fails
            }
        }

        ReboundLogger.Log($"[Mod] Finished installing dependencies for {Name} (Variant: {variant.Name})");
    }

    /// <summary>
    /// Uninstalls all mods that depend on the specified mod variant.
    /// </summary>
    /// <remarks>This method attempts to uninstall each mod that declares a dependency on the given variant.
    /// If a dependent mod is not installed or cannot be found, it is skipped. Exceptions encountered while uninstalling
    /// individual dependents are logged, and the operation continues with remaining dependents.</remarks>
    /// <param name="variant">The mod variant whose dependent mods will be uninstalled. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task UninstallDependentAsync(ModVariant variant)
    {
        if (variant == null) return;

        string currentModId = this.Id;

        ReboundLogger.Log($"[Mod] Searching for dependents of {currentModId}...");

        // Gather all mods from all catalogs to search dependencies
        var allMods = new List<Mod>();
        allMods.AddRange(Catalog.MandatoryMods);
        allMods.AddRange(Catalog.Mods);
        allMods.AddRange(Catalog.SideloadedMods);

        // Find mods that depend on the current mod ID
        var dependentMods = allMods
            .Where(mod => mod.Variants.Any(v => v.Dependencies.Any(d => d == currentModId)))
            .ToList();

        foreach (var dependentMod in dependentMods)
        {
            try
            {
                // Avoid uninstalling self again in case of weird circular dependencies
                if (dependentMod.Id == currentModId)
                    continue;

                ReboundLogger.Log($"[Mod] Found dependent mod '{dependentMod.Name}' (ID: {dependentMod.Id}). Uninstalling...");

                // Recursively uninstall dependent mod
                await dependentMod.UninstallAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[Mod] Failed to uninstall dependent mod '{dependentMod.Id}'", ex);
                // Continue with others even if one fails
            }
        }

        ReboundLogger.Log($"[Mod] Finished uninstalling dependents of {currentModId}");
    }

    /// <summary>
    /// Updates the <see cref="IsInstalled"/> and <see cref="IsIntact"/> properties.
    /// </summary>
    /// <returns>A task corresponding to the action.</returns>
    [RelayCommand]
    public async Task<(bool Installed, bool Intact)> UpdateIntegrityAsync()
    {
        try
        {
            // If no variant is selected, try to find one that is installed and intact
            if (SelectedVariantIndex == -1)
            {
                List<int> corruptModIndexList = [];

                // Iterate through all variants to find any installed one
                foreach (var variant in Variants)
                {
                    var (installed, intact) = await RetrieveIntegrityStatusForModVariant(variant);

                    // If the mod is installed but not intact, log it for later
                    if (installed && !intact)
                    {
                        corruptModIndexList.Add(Variants.IndexOf(variant));
                    }

                    // Select this variant if it's installed and intact
                    if (installed && intact)
                    {
                        SelectedVariantIndex = Variants.IndexOf(variant);
                        return (true, true);
                    }
                }

                // If no intact variant was found, but there are corrupt ones, select the first corrupt one
                if (corruptModIndexList.Count > 0)
                {
                    SelectedVariantIndex = corruptModIndexList[0];
                    return (true, false);
                }

                // Fallback: select the first variant and mark the mod as uninstalled
                SelectedVariantIndex = 0;
                return (false, true);
            }

            // Else, obtain the currently selected variant
            var selectedVariant = Variants[SelectedVariantIndex];

            var (installed1, intact1) = await RetrieveIntegrityStatusForModVariant(selectedVariant);

            // Update properties on the UI thread in a single batch
            UIThreadQueue.QueueAction(() =>
            {
                IsInstalled = installed1;
                IsIntact = intact1;
                IsLoading = false;
            });

            return (installed1, intact1);
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] UpdateIntegrityAsync failed for {Name}", ex);

            UIThreadQueue.QueueAction(() =>
            {
                IsLoading = false;
            });

            return (false, false);
        }
    }
}