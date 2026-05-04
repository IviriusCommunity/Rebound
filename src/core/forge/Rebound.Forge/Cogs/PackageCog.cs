// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.Settings;
using System.Diagnostics;
using System.Security.Principal;
using Windows.Management.Deployment;
using Windows.Services.Store;
using WinRT.Interop;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Defines when the package management operations of this cog should be triggered during the apply/remove lifecycle.
/// </summary>
public enum PackageManagementTriggeredOn
{
    /// <summary>
    /// Only install the package during the apply phase. No action is taken during removal.
    /// </summary>
    Apply,

    /// <summary>
    /// Only remove the package during the remove phase. No action is taken during application.
    /// </summary>
    Remove,

    /// <summary>
    /// Run installation and removal operations during their respective phases.
    /// </summary>
    Both,

    /// <summary>
    /// Defers to the user's configuration to determine whether Store package management should run.
    /// Local packages are always managed regardless of this setting.
    /// The relevant setting is <c>ManageStoreApps</c> under the <c>rebound</c> section, which defaults to <see langword="true"/>.
    /// If disabled, Store packages will not be installed or removed by this cog.
    /// </summary>
    FollowConfiguration
}

/// <summary>
/// Specifies the source location of a package targeted by a <see cref="PackageCog"/>.
/// </summary>
public enum PackageTargetType
{
    /// <summary>
    /// A package located on the local filesystem. Typically an MSIX or Appx file installed directly by path.
    /// </summary>
    Local,

    /// <summary>
    /// A package distributed through the Microsoft Store, identified by a Store product ID.
    /// </summary>
    Store
}

/// <summary>
/// Describes the target package for installation or removal, including its source type and the relevant identifiers.
/// </summary>
/// <param name="TargetType">
/// Whether the package is sourced from the local filesystem or the Microsoft Store.
/// </param>
/// <param name="TargetPath">
/// The filesystem path to the package file. Only used when <paramref name="TargetType"/> is <see cref="PackageTargetType.Local"/>.
/// </param>
/// <param name="StoreProductId">
/// The Microsoft Store product ID. Only used when <paramref name="TargetType"/> is <see cref="PackageTargetType.Store"/>.
/// </param>
/// <param name="PackageFamilyName">
/// The package family name used to identify installed packages for both local and Store targets.
/// Required for status checks and local removal.
/// </param>
public record PackageTarget(
    PackageTargetType TargetType,
    string? TargetPath = null,
    string? StoreProductId = null,
    string? PackageFamilyName = null);

/// <summary>
/// Handles installation, removal, and state tracking of local MSIX/Appx packages and Microsoft Store packages.
/// </summary>
public partial class PackageCog : ObservableObject, ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// Package management always requires elevation.
    /// </remarks>
    public bool RequiresElevation { get; } = true;

    /// <inheritdoc/>
    public string CogDescription { get => $"Manage package {Target.PackageFamilyName} ({Target.TargetType})"; }

    /// <summary>
    /// Describes the package to install or remove, including its source type and relevant identifiers.
    /// </summary>
    public required PackageTarget Target { get; set; }

    /// <summary>
    /// Controls when this cog is allowed to perform package operations.
    /// Defaults to <see cref="PackageManagementTriggeredOn.FollowConfiguration"/> so that
    /// package management respects the user's global preferences out of the box.
    /// </summary>
    public PackageManagementTriggeredOn DoPackageManagementOn { get; set; }
        = PackageManagementTriggeredOn.FollowConfiguration;

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        var ignorable = DoPackageManagementOn switch
        {
            PackageManagementTriggeredOn.Apply => false,
            PackageManagementTriggeredOn.Remove => true,
            PackageManagementTriggeredOn.Both => false,
            PackageManagementTriggeredOn.FollowConfiguration
                // If the target is a store app, follow configuration; otherwise, always manage the package regardless of configuration since it's a local package.
                // The reason: store apps can be installed and uninstalled by users at any moment, local packages are expected to be managed exclusively by this cog,
                // so it doesn't make sense to give users the option to ignore local package management operations since that would lead to a lot of confusion and inconsistency in package states.
                => Target.TargetType == PackageTargetType.Store
                   && !SettingsManager.GetValue("ManageStoreApps", "rebound", true),
            _ => throw new InvalidOperationException($"Unexpected DoPackageManagementOn value: {DoPackageManagementOn}")
        };

        if (ignorable)
            return new(true, null, true, true);

        return Target.TargetType switch
        {
            PackageTargetType.Local => await ApplyLocalAsync(cancellationToken).ConfigureAwait(false),
            PackageTargetType.Store => await ApplyStoreAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unexpected TargetType: {Target.TargetType}")
        };
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        var ignorable = DoPackageManagementOn switch
        {
            PackageManagementTriggeredOn.Apply => true,
            PackageManagementTriggeredOn.Remove => false,
            PackageManagementTriggeredOn.Both => false,
            PackageManagementTriggeredOn.FollowConfiguration
                // Same logic as above
                => Target.TargetType == PackageTargetType.Store
                   && !SettingsManager.GetValue("ManageStoreApps", "rebound", true),
            _ => throw new InvalidOperationException($"Unexpected DoPackageManagementOn value: {DoPackageManagementOn}")
        };

        if (ignorable)
            return new(true, null, true, true);

        return Target.TargetType switch
        {
            PackageTargetType.Local => await RemoveLocalAsync(cancellationToken).ConfigureAwait(false),
            PackageTargetType.Store => await RemoveStoreAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unexpected TargetType: {Target.TargetType}")
        };
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        // FollowConfiguration only suppresses install/remove operations, not status checks.
        // We always want to know whether the package is actually present on the system.
        try
        {
            var pm = new PackageManager();
            var sid = WindowsIdentity.GetCurrent().User?.Value;

            if (sid is null)
            {
                ReboundLogger.WriteToLog(
                    "PackageCog GetStatus", 
                    "Could not resolve current user SID for status check.", 
                    LogMessageSeverity.Error);
                return new(CogState.Unknown, "Missing SID.");
            }

            // Retrieve any of the installed packages for the current user that match the target package family name.
            // If any are found, we consider the package installed.
            var installed = pm
                .FindPackagesForUser(sid, Target.PackageFamilyName)
                .Any();

            ReboundLogger.WriteToLog(
                "PackageCog GetStatus", 
                $"Status check for {Target.PackageFamilyName}: {(installed ? "Installed" : "Not installed")}.");
            return new(installed ? CogState.Installed : CogState.NotInstalled);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "PackageCog GetStatus", 
                $"Status check failed for {Target.PackageFamilyName}.", LogMessageSeverity.Error, ex);
            return new(CogState.Unknown, "An error occurred.");
        }
    }

    #region Local Package Handling

    private async Task<CogOperationResult> ApplyLocalAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "PackageCog ApplyLocal", 
                $"Installing local package from: {Target.TargetPath}.");

            var pm = new PackageManager();
            var op = pm.AddPackageByUriAsync(
                new Uri(Target.TargetPath!), // Technically supports URLs if needed
                new AddPackageOptions { AllowUnsigned = true });

            op.Progress += (_, progress) =>
                ReboundLogger.WriteToLog(
                    "PackageCog ApplyLocal", 
                    $"Local install progress for {Target.PackageFamilyName}: {progress.percentage}%.");

            var result = await op.AsTask(cancellationToken).ConfigureAwait(false);

            if (result.ExtendedErrorCode is not null)
            {
                ReboundLogger.WriteToLog(
                    "PackageCog ApplyLocal",
                    $"Local install failed for {Target.PackageFamilyName}: {result.ErrorText}.",
                    LogMessageSeverity.Error,
                    result.ExtendedErrorCode);
                return new(false, result.ErrorText, false);
            }

            if (!result.IsRegistered)
            {
                ReboundLogger.WriteToLog(
                    "PackageCog ApplyLocal",
                    $"Local package {Target.PackageFamilyName} installed but failed to register.",
                    LogMessageSeverity.Error);
                return new(false, "NOT_REGISTERED", false);
            }

            ReboundLogger.WriteToLog(
                "PackageCog ApplyLocal", 
                $"Local install succeeded for {Target.PackageFamilyName}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "PackageCog ApplyLocal", 
                $"Local install failed for {Target.PackageFamilyName}.", 
                LogMessageSeverity.Error, 
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    private async Task<CogOperationResult> RemoveLocalAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "PackageCog RemoveLocal", 
                $"Uninstalling local package: {Target.PackageFamilyName}.");

            var pm = new PackageManager();
            var sid = WindowsIdentity.GetCurrent().User?.Value;

            if (sid is null)
            {
                ReboundLogger.WriteToLog(
                    "PackageCog RemoveLocal",
                    "Could not resolve current user SID for local removal.", 
                    LogMessageSeverity.Error);
                return new(false, "NO_SID", false);
            }

            var packages = pm.FindPackagesForUser(sid, Target.PackageFamilyName);

            // Remove each package for the current user (there can be multiple for a single family)
            foreach (var package in packages)
            {
                var result = await pm.RemovePackageAsync(package.Id.FullName).AsTask(cancellationToken).ConfigureAwait(false);

                if (result.ExtendedErrorCode is not null)
                {
                    ReboundLogger.WriteToLog(
                        "PackageCog RemoveLocal",
                        $"Local uninstall failed for {Target.PackageFamilyName}: {result.ErrorText}.",
                        LogMessageSeverity.Error,
                        result.ExtendedErrorCode);
                    return new(false, result.ErrorText, false);
                }

                if (result.IsRegistered)
                {
                    ReboundLogger.WriteToLog(
                        "PackageCog RemoveLocal",
                        $"Local package {Target.PackageFamilyName} removal did not fully deregister.",
                        LogMessageSeverity.Error);
                    return new(false, "STILL_REGISTERED", false);
                }
            }

            ReboundLogger.WriteToLog(
                "PackageCog RemoveLocal", 
                $"Local uninstall succeeded for {Target.PackageFamilyName}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "PackageCog RemoveLocal", 
                $"Local uninstall failed for {Target.PackageFamilyName}.", 
                LogMessageSeverity.Error, 
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    #endregion

    #region Store Package Handling

    private async Task<CogOperationResult> ApplyStoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "PackageCog ApplyStore", 
                $"Starting Store install for product ID: {Target.StoreProductId}.");

            // Store context really wants UI for the purchase flow, so we have to initialize it with the current window handle.
            // It's meant for purchase confirmation dialogs and such, but it seems to be required even if the product is already owned and just needs to be installed.
            // Users might see a flashing window specifically for the purchase confirmation prompt regardless of whether they already own the product or not, but unfortunately there's no way around it that I could find.
            // It seems to be a Windows bug. Not surprising.
            var ctx = StoreContext.GetDefault();
            InitializeWithWindow.Initialize(ctx, Process.GetCurrentProcess().MainWindowHandle);

            var purchase = await ctx
                .RequestPurchaseAsync(Target.StoreProductId)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            switch (purchase.Status)
            {
                case StorePurchaseStatus.Succeeded:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Purchase succeeded for {Target.StoreProductId}.");
                    break;
                case StorePurchaseStatus.AlreadyPurchased:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Product {Target.StoreProductId} is already owned. Proceeding with install.");
                    break;
                case StorePurchaseStatus.NotPurchased:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Purchase was not completed for {Target.StoreProductId}.", LogMessageSeverity.Warning);
                    return new(false, "NOT_PURCHASED", false);
                case StorePurchaseStatus.NetworkError:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Network error during purchase of {Target.StoreProductId}.", LogMessageSeverity.Error);
                    return new(false, "NETWORK_ERROR", false);
                case StorePurchaseStatus.ServerError:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Server error during purchase of {Target.StoreProductId}.", LogMessageSeverity.Error);
                    return new(false, "SERVER_ERROR", false);
                default:
                    ReboundLogger.WriteToLog("PackageCog ApplyStore", $"Unexpected purchase status for {Target.StoreProductId}: {purchase.Status}.", LogMessageSeverity.Error);
                    return new(false, "UNKNOWN_PURCHASE_STATUS", false);
            }

            var installOp = ctx.DownloadAndInstallStorePackagesAsync(
                [Target.StoreProductId ?? throw new InvalidOperationException("StoreProductId is null.")]);

            installOp.Progress = (_, progress) =>
                ReboundLogger.WriteToLog(
                    "PackageCog ApplyStore", 
                    $"Store install progress for {Target.StoreProductId}: {progress.TotalDownloadProgress}%.");

            var installResult = await installOp.AsTask(cancellationToken).ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "PackageCog ApplyStore",
                $"Store install result for {Target.StoreProductId}: {installResult.OverallState}.",
                installResult.OverallState == StorePackageUpdateState.Completed ? LogMessageSeverity.Message : LogMessageSeverity.Error);

            return installResult.OverallState == StorePackageUpdateState.Completed
                ? new(true, null, true)
                : new(false, installResult.OverallState.ToString(), false);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "PackageCog ApplyStore", 
                $"Store install failed for {Target.StoreProductId}.",
                LogMessageSeverity.Error, 
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    private async Task<CogOperationResult> RemoveStoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "PackageCog RemoveStore",
                $"Starting Store uninstall for product ID: {Target.StoreProductId}.");

            // StoreContext requires a window handle even for uninstall operations. Why? No idea
            var ctx = StoreContext.GetDefault();
            InitializeWithWindow.Initialize(ctx, Process.GetCurrentProcess().MainWindowHandle);

            var result = await ctx
                .UninstallStorePackageByStoreIdAsync(Target.StoreProductId)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "PackageCog RemoveStore",
                $"Store uninstall result for {Target.StoreProductId}: {result.Status}.",
                result.Status == StoreUninstallStorePackageStatus.Succeeded ? LogMessageSeverity.Message : LogMessageSeverity.Error);

            return result.Status switch
            {
                StoreUninstallStorePackageStatus.Succeeded => new(true, null, true),
                StoreUninstallStorePackageStatus.NetworkError => new(false, "NETWORK_ERROR", false),
                _ => new(false, result.Status.ToString(), false)
            };
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "PackageCog RemoveStore", 
                $"Store uninstall failed for {Target.StoreProductId}.", 
                LogMessageSeverity.Error,
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    #endregion
}