// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using Rebound.Forge.Cogs;
using Rebound.Forge.Launchers;
using Rebound.Forge.Mods;
using System.Collections.ObjectModel;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge;

/// <summary>
/// The state in which a Rebound mod is found.
/// </summary>
public enum ModIntegrity
{
    /// <summary>
    /// Every cog of the mod is installed and configured properly.
    /// </summary>
    Installed,

    /// <summary>
    /// One or more cogs of the mod are missing, while the rest are installed.
    /// </summary>
    Corrupt,

    /// <summary>
    /// None of the mod's cogs are installed.
    /// </summary>
    NotInstalled
}

/// <summary>
/// Installation presets for quick configuration.
/// </summary>
public enum InstallationTemplate
{
    /// <summary>
    /// Must be used for every item in <see cref="Catalog.MandatoryMods"/>.
    /// </summary>
    Mandatory,

    /// <summary>
    /// Mods that represent the core Rebound experience.
    /// </summary>
    Basic,

    /// <summary>
    /// Recommended configuration of Rebound mods.
    /// </summary>
    Recommended,

    /// <summary>
    /// The complete set of Rebound mods.
    /// </summary>
    Complete,

    /// <summary>
    /// Includes additional mods that are not essential.
    /// </summary>
    Extras
}

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
/// Event args raised when a cog within this mod requires user confirmation to proceed.
/// The UI should inspect <see cref="CogTypeName"/> to determine the kind of prompt to display,
/// then resolve <see cref="CogArgs"/>.<see cref="ConfirmationPromptEventArgs.UserResponse"/>
/// with the user's decision.
/// </summary>
/// <param name="cogTypeName">
/// The runtime type name of the cog requesting confirmation (e.g. <c>"ProcessKillCog"</c>).
/// The UI pattern-matches on this to build an appropriate prompt.
/// </param>
/// <param name="cogArgs">
/// The original <see cref="ConfirmationPromptEventArgs"/> from the cog, containing the
/// <see cref="TaskCompletionSource{Boolean}"/> the cog is awaiting.
/// </param>
public sealed class ModConfirmationPromptEventArgs(string cogTypeName, ConfirmationPromptEventArgs cogArgs) : EventArgs
{
    /// <summary>
    /// The runtime type name of the cog that raised the confirmation request.
    /// Use this to determine which prompt to display in the UI.
    /// </summary>
    public string CogTypeName { get; } = cogTypeName;

    /// <summary>
    /// The underlying confirmation args from the cog. Resolve
    /// <see cref="ConfirmationPromptEventArgs.UserResponse"/> to signal the user's decision.
    /// </summary>
    public ConfirmationPromptEventArgs CogArgs { get; } = cogArgs;
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
    /// The ID of a mod variant.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The tasks that this mod does in the system. Can be anything from file
    /// operations to registry tweaks.
    /// </summary>
#pragma warning disable CA2227
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
    /// Dependencies for the current mod. Collection consists of <see cref="Mod.Id"/> GUIDs.
    /// </summary>
    public ObservableCollection<Guid> Dependencies { get; set; } = [];
#pragma warning restore CA2227
}

/// <summary>
/// Definition class for a system modification that the Rebound Forge can interpret and
/// install based on given instructions.
/// </summary>
#pragma warning disable CA1716
public partial class Mod : ObservableObject
#pragma warning restore CA1716
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
    public Guid? Id { get; set; }

    /// <summary>
    /// The mod's description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The mod's icon that is displayed in Rebound Hub.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The mod's category.
    /// </summary>
    public ModCategory Category { get; set; }

    /// <summary>
    /// Defines to which installation template the mod belongs.
    /// </summary>
    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    /// <summary>
    /// Variants of the current mod.
    /// </summary>
#pragma warning disable CA2227
    public ObservableCollection<ModVariant> Variants { get; set; } = [];
#pragma warning restore CA2227

    /// <summary>
    /// Raised when a cog within this mod requires user confirmation to proceed with its operation.
    /// The UI should inspect <see cref="ModConfirmationPromptEventArgs.CogTypeName"/> to determine
    /// the appropriate prompt, then resolve <see cref="ModConfirmationPromptEventArgs.CogArgs"/>
    /// with the user's decision.
    /// If no handler is subscribed, the cog's own fallback behavior applies.
    /// </summary>
    public event EventHandler<ModConfirmationPromptEventArgs>? OnConfirmationRequested;

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    // Captured on construction from whatever thread creates Mod (expected to be the UI thread).
    // Used to post property updates back to the STA UI thread without taking a hard dependency
    // on any UI framework type in this backend project.
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    /// <summary>
    /// Creates a new instance of the <see cref="Mod"/> class.
    /// </summary>
    public Mod() { }

    async partial void OnSelectedVariantIndexChanged(int value)
    {
        if (value < 0 || value >= Variants.Count) return;

        var result = await UpdateIntegrityAsync();
        SetOnUiThread(() =>
        {
            IsInstalled = result.Installed;
            IsIntact = result.Intact;
        });
    }

    /// <summary>
    /// Posts an action to the captured UI synchronization context. If no context was captured
    /// (e.g. unit test or non-UI host), the action is invoked inline on the current thread.
    /// </summary>
    private void SetOnUiThread(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
            action();
        else
            _uiContext.Post(_ => action(), null);
    }

    /// <summary>
    /// Triggers <see cref="ICog.ApplyAsync"/> on every <see cref="ICog"/> from the selected variant's cogs.
    /// Reverts already-applied cogs if any non-ignorable cog fails with <see cref="CogOperationResult.SafeToContinue"/> = false.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        await RunTask(true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggers <see cref="ICog.RemoveAsync"/> on every <see cref="ICog"/> from the selected variant's cogs.
    /// </summary>
    [RelayCommand]
    public async Task UninstallAsync()
    {
        await RunTask(false, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the installation or uninstallation process for the currently selected mod variant.
    /// </summary>
    /// <remarks>
    /// During installation, cogs are applied in order. For each cog:
    /// <list type="bullet">
    ///   <item>If the result is ignorable, execution continues to the next cog.</item>
    ///   <item>If the result succeeds, progress is advanced and execution continues.</item>
    ///   <item>If the result fails with <see cref="CogOperationResult.SafeToContinue"/> = true,
    ///         a warning is logged but execution continues.</item>
    ///   <item>If the result fails with <see cref="CogOperationResult.SafeToContinue"/> = false,
    ///         all already-applied cogs are reverted immediately and the operation halts.</item>
    /// </list>
    /// Consent events from <see cref="IConfirmationPromptCog"/> implementations are forwarded
    /// to <see cref="OnConfirmationRequested"/> for the UI to handle.
    /// </remarks>
    private async Task RunTask(bool install, CancellationToken cancellationToken)
    {
        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Resolve which variant to operate on
            if (SelectedVariantIndex < 0 || SelectedVariantIndex >= Variants.Count)
            {
                if (install)
                {
                    ReboundLogger.WriteToLog(
                        "Mod RunTask",
                        $"No variant selected for {Name}, defaulting to first variant for installation.");
                    SetOnUiThread(() => SelectedVariantIndex = 0);
                }
                else
                {
                    ReboundLogger.WriteToLog(
                        "Mod RunTask",
                        $"Invalid variant selection for {Name}, attempting to find installed variant...");

                    int installedVariantIndex = -1;

                    for (int i = 0; i < Variants.Count; i++)
                    {
                        var (installed, _) = await RetrieveIntegrityStatusForModVariantAsync(Variants[i])
                            .ConfigureAwait(false);

                        if (installed)
                        {
                            installedVariantIndex = i;
                            ReboundLogger.WriteToLog(
                                "Mod RunTask",
                                $"Found installed variant at index {i} for {Name}.");
                            break;
                        }
                    }

                    if (installedVariantIndex == -1)
                    {
                        ReboundLogger.WriteToLog(
                            "Mod RunTask",
                            $"Cannot uninstall {Name} - no installed variant found.");
                        return;
                    }

                    SetOnUiThread(() => SelectedVariantIndex = installedVariantIndex);
                }
            }

            var selectedVariant = Variants[SelectedVariantIndex];

            // Handle dependencies / dependents
            if (install)
                await InstallDependenciesAsync(selectedVariant).ConfigureAwait(false);
            else
                await UninstallDependentAsync(selectedVariant).ConfigureAwait(false);

            // Iterate over all variants: apply the selected one, remove all others
            foreach (var variant in Variants)
            {
                if (variant.Cogs is null || variant.Cogs.Count == 0)
                {
                    ReboundLogger.WriteToLog(
                        "Mod RunTask",
                        $"Cannot {(install ? "install" : "uninstall")} {Name} - no cogs defined.");
                    return;
                }

                bool isSelectedVariant = variant.Id == selectedVariant.Id;

                // Pre-calculate total non-ignorable cog count for progress tracking.
                // We check GetStatusAsync upfront for ignorable detection where possible,
                // but for simplicity we count all cogs and skip ignorable results at runtime.
                int totalCogs = variant.Cogs.Count;
                int progress = 0;

                ReboundLogger.WriteToLog(
                    "Mod RunTask",
                    $"{(install ? "Installing" : "Uninstalling")} {Name} (variant: {variant.Name}, cogs: {totalCogs})...");

                SetOnUiThread(() =>
                {
                    IsLoading = true;
                    TaskCount = totalCogs;
                    Progress = 0;
                });

                // Track which cogs have been successfully applied in this pass so we can revert them on failure.
                // Only relevant during installation of the selected variant.
                var appliedCogs = new List<ICog>();

                bool haltedByFailure = false;

                foreach (var cog in variant.Cogs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wire up the consent event before invoking the cog, so any
                    // user confirmation prompt is forwarded up to the Mod level.
                    EventHandler<ConfirmationPromptEventArgs>? cogConsentHandler = null;
                    if (cog is IConfirmationPromptCog confirmationCog)
                    {
                        cogConsentHandler = (_, cogArgs) =>
                        {
                            var modArgs = new ModConfirmationPromptEventArgs(cog.GetType().Name, cogArgs);
                            OnConfirmationRequested?.Invoke(this, modArgs);
                        };
                        confirmationCog.OnConfirmationRequested += cogConsentHandler;
                    }

                    CogOperationResult result;
                    try
                    {
                        result = install && isSelectedVariant
                            ? await cog.ApplyAsync(cancellationToken).ConfigureAwait(false)
                            : await cog.RemoveAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation during install: revert already-applied cogs
                        if (install && isSelectedVariant)
                        {
                            ReboundLogger.WriteToLog(
                                "Mod RunTask",
                                $"Installation of {Name} cancelled. Reverting {appliedCogs.Count} already-applied cog(s).");
                            await RevertAppliedCogsAsync(appliedCogs).ConfigureAwait(false);
                        }
                        throw;
                    }
                    finally
                    {
                        // Always detach the consent handler regardless of outcome
                        if (cog is IConfirmationPromptCog confirmationCog2 && cogConsentHandler is not null)
                            confirmationCog2.OnConfirmationRequested -= cogConsentHandler;
                    }

                    // Ignorable: result can be safely disregarded, continue normally
                    if (result.Ignorable)
                    {
                        ReboundLogger.WriteToLog(
                            "Mod RunTask",
                            $"Cog '{cog.CogName}' result is ignorable. Continuing.");

                        SetOnUiThread(() => Progress = ++progress);
                        continue;
                    }

                    if (result.Success)
                    {
                        // Track for potential rollback on a later failure
                        if (install && isSelectedVariant)
                            appliedCogs.Add(cog);

                        SetOnUiThread(() => Progress = ++progress);

                        ReboundLogger.WriteToLog(
                            "Mod RunTask",
                            $"Cog '{cog.CogName}' succeeded.");
                    }
                    else if (!result.SafeToContinue)
                    {
                        // Hard failure: revert everything applied so far and halt
                        ReboundLogger.WriteToLog(
                            "Mod RunTask",
                            $"Cog '{cog.CogName}' failed with error '{result.Error}' and SafeToContinue=false. Reverting {appliedCogs.Count} cog(s).",
                            LogMessageSeverity.Error);

                        if (install && isSelectedVariant)
                            await RevertAppliedCogsAsync(appliedCogs).ConfigureAwait(false);

                        haltedByFailure = true;
                        break;
                    }
                    else
                    {
                        // Soft failure: log a warning and keep going
                        ReboundLogger.WriteToLog(
                            "Mod RunTask",
                            $"Cog '{cog.CogName}' failed with error '{result.Error}' but SafeToContinue=true. Continuing with warning.",
                            LogMessageSeverity.Warning);

                        SetOnUiThread(() => Progress = ++progress);
                    }
                }

                if (haltedByFailure)
                    break;
            }

            await UpdateIntegrityAsync().ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "Mod RunTask",
                $"{(install ? "Installation" : "Uninstallation")} finished for {Name}.");
        }
        catch (OperationCanceledException)
        {
            ReboundLogger.WriteToLog(
                "Mod RunTask",
                $"Operation was cancelled for {Name}.",
                LogMessageSeverity.Warning);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Mod RunTask",
                $"{(install ? "InstallAsync" : "UninstallAsync")} failed for {Name}.",
                LogMessageSeverity.Error,
                ex);
        }
        finally
        {
            SetOnUiThread(() =>
            {
                IsLoading = false;
                Progress = 0;
                TaskCount = 0;
            });

            _operationLock.Release();
        }
    }

    /// <summary>
    /// Reverts a list of already-applied cogs by calling <see cref="ICog.RemoveAsync"/> on each,
    /// in reverse order. Used as the rollback path on hard failure or cancellation during installation.
    /// Individual revert failures are logged but do not abort the revert loop.
    /// </summary>
    private static async Task RevertAppliedCogsAsync(List<ICog> appliedCogs)
    {
        for (int i = appliedCogs.Count - 1; i >= 0; i--)
        {
            var cog = appliedCogs[i];
            try
            {
                ReboundLogger.WriteToLog(
                    "Mod Revert",
                    $"Reverting cog '{cog.CogName}'...");

                var result = await cog.RemoveAsync(CancellationToken.None).ConfigureAwait(false);

                if (!result.Success)
                {
                    ReboundLogger.WriteToLog(
                        "Mod Revert",
                        $"Revert of cog '{cog.CogName}' failed with error '{result.Error}'.",
                        LogMessageSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                ReboundLogger.WriteToLog(
                    "Mod Revert",
                    $"Revert of cog '{cog.CogName}' threw an exception.",
                    LogMessageSeverity.Error,
                    ex);
            }
        }
    }

    /// <summary>
    /// Launches every launcher object inside the selected variant's launchers.
    /// </summary>
    [RelayCommand]
    public async Task OpenAsync()
    {
        if (SelectedVariantIndex == -1) return;

        var selectedVariant = Variants[SelectedVariantIndex];

        if (selectedVariant.Launchers is null || selectedVariant.Launchers.Count == 0)
        {
            ReboundLogger.WriteToLog("Mod Open", $"Cannot open {Name} - no launchers defined.");
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
                ReboundLogger.WriteToLog("Mod Open", $"Launcher failed for {Name}.", LogMessageSeverity.Error, ex);
            }
        }
    }

    /// <summary>
    /// Re-runs installation to repair a corrupt or partially-installed mod.
    /// </summary>
    [RelayCommand]
    public async Task RepairAsync() => await InstallAsync(CancellationToken.None).ConfigureAwait(false);

    /// <summary>
    /// Queries the integrity of a specific variant by calling <see cref="ICog.GetStatusAsync"/>
    /// on each of its non-ignorable cogs.
    /// </summary>
    private async Task<(bool Installed, bool Intact)> RetrieveIntegrityStatusForModVariantAsync(ModVariant variant)
    {
        SetOnUiThread(() => IsLoading = true);

        try
        {
            // Collect statuses for all cogs, filtering out ignorable ones
            var statusTasks = variant.Cogs
                .Select(c => c.GetStatusAsync())
                .ToList();

            var statuses = await Task.WhenAll(statusTasks).ConfigureAwait(false);

            // Filter to non-ignorable results for integrity calculation
            var meaningful = statuses
                .Where(s => s.State != CogState.Ignorable)
                .ToList();

            int total = meaningful.Count;

            if (total == 0)
            {
                SetOnUiThread(() =>
                {
                    IsInstalled = false;
                    IsIntact = true;
                    IsLoading = false;
                });
                return (false, true);
            }

            int installedCount = meaningful.Count(s => s.State == CogState.Installed);

            bool installed = installedCount > 0;
            bool intact = installedCount == 0 || installedCount == total;

            SetOnUiThread(() =>
            {
                IsInstalled = installed;
                IsIntact = intact;
                IsLoading = false;
            });

            ReboundLogger.WriteToLog(
                "Mod RetrieveIntegrity",
                $"Integrity for {Name} / variant {variant.Name}: Installed={installed}, Intact={intact}, installedCogs={installedCount}, totalCogs={total}.");

            return (installed, intact);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Mod RetrieveIntegrity",
                $"RetrieveIntegrityStatusForModVariantAsync failed for {Name}.",
                LogMessageSeverity.Error,
                ex);

            SetOnUiThread(() => IsLoading = false);
            return (false, false);
        }
    }

    /// <summary>
    /// Installs all dependencies declared by the given variant.
    /// </summary>
    private async Task InstallDependenciesAsync(ModVariant variant)
    {
        if (variant.Dependencies is null || variant.Dependencies.Count == 0)
            return;

        ReboundLogger.WriteToLog(
            "Mod InstallDependencies",
            $"Installing dependencies for {Name} (variant: {variant.Name})...");

        var allMods = GetAllCatalogMods();

        foreach (var dependency in variant.Dependencies)
        {
            try
            {
                var dependencyMod = allMods.FirstOrDefault(m => m.Id == dependency);

                if (dependencyMod is null)
                {
                    ReboundLogger.WriteToLog(
                        "Mod InstallDependencies",
                        $"Dependency '{dependency}' not found for {Name}.",
                        LogMessageSeverity.Warning);
                    continue;
                }

                var (installed, intact) = await dependencyMod.UpdateIntegrityAsync().ConfigureAwait(false);

                if (installed && intact)
                {
                    ReboundLogger.WriteToLog(
                        "Mod InstallDependencies",
                        $"Dependency '{dependency}' is already installed and intact.");
                    continue;
                }

                ReboundLogger.WriteToLog(
                    "Mod InstallDependencies",
                    $"Installing dependency '{dependency}' for {Name}.");

                await dependencyMod.InstallAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReboundLogger.WriteToLog(
                    "Mod InstallDependencies",
                    $"Failed to install dependency '{dependency}' for {Name}.",
                    LogMessageSeverity.Error,
                    ex);
            }
        }

        ReboundLogger.WriteToLog(
            "Mod InstallDependencies",
            $"Finished installing dependencies for {Name} (variant: {variant.Name}).");
    }

    /// <summary>
    /// Uninstalls all mods that declare a dependency on this mod, before this mod is uninstalled.
    /// </summary>
    private async Task UninstallDependentAsync(ModVariant variant)
    {
        if (variant is null) return;

        Guid? currentModId = Id;

        ReboundLogger.WriteToLog(
            "Mod UninstallDependent",
            $"Searching for dependents of {currentModId}...");

        var allMods = GetAllCatalogMods();

        var dependentMods = allMods
            .Where(mod => mod.Variants.Any(v => v.Dependencies.Any(d => d == currentModId)))
            .ToList();

        foreach (var dependentMod in dependentMods)
        {
            try
            {
                if (dependentMod.Id == currentModId)
                    continue;

                ReboundLogger.WriteToLog(
                    "Mod UninstallDependent",
                    $"Uninstalling dependent '{dependentMod.Name}' (ID: {dependentMod.Id})...");

                await dependentMod.UninstallAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReboundLogger.WriteToLog(
                    "Mod UninstallDependent",
                    $"Failed to uninstall dependent '{dependentMod.Id}'.",
                    LogMessageSeverity.Error,
                    ex);
            }
        }

        ReboundLogger.WriteToLog(
            "Mod UninstallDependent",
            $"Finished uninstalling dependents of {currentModId}.");
    }

    /// <summary>
    /// Updates <see cref="IsInstalled"/> and <see cref="IsIntact"/> by querying the integrity
    /// of the currently selected variant (or scanning all variants if none is selected).
    /// </summary>
    [RelayCommand]
    public async Task<(bool Installed, bool Intact)> UpdateIntegrityAsync()
    {
        try
        {
            if (SelectedVariantIndex == -1)
            {
                List<int> corruptIndices = [];

                foreach (var variant in Variants)
                {
                    var (installed, intact) = await RetrieveIntegrityStatusForModVariantAsync(variant)
                        .ConfigureAwait(false);

                    if (installed && !intact)
                        corruptIndices.Add(Variants.IndexOf(variant));

                    if (installed && intact)
                    {
                        SetOnUiThread(() => SelectedVariantIndex = Variants.IndexOf(variant));
                        return (true, true);
                    }
                }

                if (corruptIndices.Count > 0)
                {
                    int firstCorrupt = corruptIndices[0];
                    SetOnUiThread(() => SelectedVariantIndex = firstCorrupt);
                    return (true, false);
                }

                SetOnUiThread(() => SelectedVariantIndex = 0);
                return (false, true);
            }

            var selectedVariant = Variants[SelectedVariantIndex];
            var (installed1, intact1) = await RetrieveIntegrityStatusForModVariantAsync(selectedVariant)
                .ConfigureAwait(false);

            SetOnUiThread(() =>
            {
                IsInstalled = installed1;
                IsIntact = intact1;
                IsLoading = false;
            });

            return (installed1, intact1);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Mod UpdateIntegrity",
                $"UpdateIntegrityAsync failed for {Name}.",
                LogMessageSeverity.Error,
                ex);

            SetOnUiThread(() => IsLoading = false);
            return (false, false);
        }
    }

    /// <summary>
    /// Collects all mods across all catalog lists into a single flat list.
    /// </summary>
    private static List<Mod> GetAllCatalogMods()
    {
        var all = new List<Mod>();
        all.AddRange(Catalog.MandatoryMods);
        all.AddRange(Catalog.Mods);
        all.AddRange(Catalog.SideloadedMods);
        return all;
    }
}