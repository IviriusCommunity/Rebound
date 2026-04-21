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

namespace Rebound.Forge.Cogs
{
    public enum PackageManagementTriggeredOn
    {
        Apply,
        Remove,
        Both,
        FollowConfiguration
    }

    public enum PackageTargetType
    {
        Local,
        Store
    }

    public record PackageTarget(PackageTargetType TargetType, string? TargetPath = null, string? StoreProductId = null, string? PackageFamilyName = null);

    /// <summary>
    /// Handles installation and management of Microsoft Store apps by product ID.
    /// </summary>
    public partial class PackageCog : ObservableObject, ICog
    {
        /// <inheritdoc/>
        public required string CogName { get; set; }

        /// <inheritdoc/>
        public required Guid CogId { get; set; }

        /// <inheritdoc/>
        public required bool RequiresElevation { get; set; }

        /// <inheritdoc/>
        public required string CogDescription { get; set; }

        /// <summary>
        /// The launch target — either a full executable path or a package family name.
        /// </summary>
        public required PackageTarget Target { get; set; }

        /// <summary>
        /// Whether to launch on apply, remove, or both.
        /// </summary>
        public PackageManagementTriggeredOn DoPackageManagementOn { get; set; } = PackageManagementTriggeredOn.Both;

        public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
        {
            // Check if managing Store apps is enabled in settings. If Rebound shouldn't manage Store apps, mark this cog as ignorable to skip it in the workflow.
            var ignorable = DoPackageManagementOn switch
            {
                PackageManagementTriggeredOn.Apply => false,
                PackageManagementTriggeredOn.Remove => true,
                PackageManagementTriggeredOn.Both => false,
                PackageManagementTriggeredOn.FollowConfiguration => !SettingsManager.GetValue("ManageStoreApps", "rebound", true),
                _ => throw new InvalidOperationException($"Unexpected DoPackageManagementOn value: {DoPackageManagementOn}")
            };

            // If the cog is ignorable based on the trigger configuration, skip the installation logic and return a successful result with ignorable set to true.
            if (ignorable)
                return new(true, null, true, true);

            switch (Target.TargetType)
            {
                case PackageTargetType.Local:
                    {
                        try
                        {
                            ReboundLogger.WriteToLog("PackageCog (Local Package)", $"Starting installation from {Target.TargetPath}.");

                            // Create a PackageManager instance and add the package by URI. This will handle both local and remote packages, and can also accept unsigned packages.
                            var deployment = new PackageManager().AddPackageByUriAsync(
                                new Uri(Target.TargetPath!),
                                new() 
                                { 
                                    AllowUnsigned = true 
                                });

                            // Track progress for logging because yes
                            deployment.Progress += (sender, progress) =>
                                ReboundLogger.WriteToLog("PackageCog Installation (Local Package)", $"Deployment progress: {progress.percentage}%");

                            // Obtain the operation result
                            var installationResult = await deployment.AsTask(cancellationToken).ConfigureAwait(false);

                            // Handle errors
                            if (installationResult.ExtendedErrorCode != null)
                            {
                                ReboundLogger.WriteToLog(
                                    "PackageCog Installation (Local Package)", 
                                    $"Failed to install for {Target.PackageFamilyName}.", 
                                    LogMessageSeverity.Error, 
                                    installationResult.ExtendedErrorCode);

                                return new(false, installationResult.ExtendedErrorCode.ToString(), false);
                            }

                            if (!installationResult.IsRegistered)
                            {
                                ReboundLogger.WriteToLog(
                                    "PackageCog Installation (Local Package)",
                                    $"Failed to register for {Target.PackageFamilyName}.",
                                    LogMessageSeverity.Error,
                                    installationResult.ExtendedErrorCode);

                                return new(false, installationResult.ExtendedErrorCode?.ToString(), false);
                            }

                            // Success state
                            ReboundLogger.WriteToLog("PackageCog Installation (Local Package)", $"Successfully installed package: {Target.PackageFamilyName}");
                            return new(true, null, true);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "PackageCog Installation (Local Package)",
                                $"Installation failed for {Target.PackageFamilyName}.",
                                LogMessageSeverity.Error,
                                ex);
                            return new(true, null, true);
                        }
                    }
                case PackageTargetType.Store:
                    {
                        try
                        {
                            ReboundLogger.WriteToLog("PackageCog Apply (Store Package)", $"Starting Store install for product ID: {Target.StoreProductId}");

                            // Obtaining the Store context for the current user (default)
                            var storeContext = StoreContext.GetDefault();

                            // Required to display purchase UI if the user needs to buy the app or if there are any issues with the license.
                            InitializeWithWindow.Initialize(storeContext, Process.GetCurrentProcess().MainWindowHandle);

                            // Request purchase of the app. This will handle both free and paid apps. For free apps, this will simply acquire the license. For paid apps, this will prompt the user to complete the purchase.
                            var purchaseResult = await storeContext.RequestPurchaseAsync(Target.StoreProductId).AsTask(cancellationToken).ConfigureAwait(true);

                            // Handle purchasing status
                            switch (purchaseResult.Status)
                            {
                                case StorePurchaseStatus.Succeeded:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "PackageCog Purchase (Store Package)",
                                            $"Purchase successful for {Target.StoreProductId}. Proceeding with installation.",
                                            LogMessageSeverity.Message);
                                        break;
                                    }
                                case StorePurchaseStatus.AlreadyPurchased:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "PackageCog Purchase (Store Package)",
                                            $"Product {Target.StoreProductId} is already purchased. Proceeding with installation.",
                                            LogMessageSeverity.Message);
                                        break;
                                    }
                                case StorePurchaseStatus.NotPurchased:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "StorePackageCog Purchase",
                                            $"Purchase canceled or failed for {Target.StoreProductId}. Status: NotPurchased",
                                            LogMessageSeverity.Error);

                                        return new CogOperationResult(false, "NOT_PURCHASED", false);
                                    }
                                case StorePurchaseStatus.NetworkError:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "PackageCog Purchase (Store Package)",
                                            $"Network error during purchase of {Target.StoreProductId}. Status: NetworkError",
                                            LogMessageSeverity.Error);

                                        return new CogOperationResult(false, "NETWORK_ERROR", false);
                                    }
                                case StorePurchaseStatus.ServerError:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "PackageCog Purchase (Store Package)",
                                            $"Server error during purchase of {Target.StoreProductId}. Status: ServerError",
                                            LogMessageSeverity.Error);

                                        return new CogOperationResult(false, "SERVER_ERROR", false);
                                    }
                                default:
                                    {
                                        ReboundLogger.WriteToLog(
                                            "PackageCog Purchase (Store Package)",
                                            $"Unknown purchase status for {Target.StoreProductId}. Status: {purchaseResult.Status}",
                                            LogMessageSeverity.Error);

                                        return new CogOperationResult(false, "UNKNOWN_ERROR", false);
                                    }
                            }

                            var installOperation = storeContext.DownloadAndInstallStorePackagesAsync(
                                new List<string> 
                                { 
                                    Target.StoreProductId ?? throw new InvalidOperationException("StoreProductId is null") 
                                });

                            // Handle Progress updates
                            installOperation.Progress = (info, progress) =>
                            {
                                ReboundLogger.WriteToLog(
                                    "PackageCog Progress (Store Package)",
                                    $"Downloading/Installing {Target.StoreProductId}: {progress.TotalDownloadProgress}%",
                                    LogMessageSeverity.Message);
                            };

                            // Await the completion
                            var result = await installOperation.AsTask(cancellationToken).ConfigureAwait(false);

                            // Handle the final installation state
                            switch (result.OverallState)
                            {
                                case StorePackageUpdateState.Completed:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Install (Store Package)",
                                        $"Successfully installed {Target.StoreProductId}.",
                                        LogMessageSeverity.Message);

                                    return new CogOperationResult(true, null, true);

                                case StorePackageUpdateState.Canceled:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Install (Store Package)",
                                        $"Installation canceled for {Target.StoreProductId}.",
                                        LogMessageSeverity.Warning);

                                    return new CogOperationResult(false, "CANCELLED", false);

                                case StorePackageUpdateState.ErrorWiFiRequired:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Install (Store Package)",
                                        $"WiFi is required to install {Target.StoreProductId}.",
                                        LogMessageSeverity.Error);

                                    return new CogOperationResult(false, "NO_WIFI", false);

                                case StorePackageUpdateState.ErrorLowBattery:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Install (Store Package)",
                                        $"Battery is too low to continue installing {Target.StoreProductId}.",
                                        LogMessageSeverity.Error);

                                    return new CogOperationResult(false, "LOW_BATTERY", false);

                                default:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Install (Store Package)",
                                        $"An unknown error has occured while installing {Target.StoreProductId}. State: {result.OverallState}",
                                        LogMessageSeverity.Error);

                                    return new CogOperationResult(false, "UNKNOWN_ERROR", false);
                            }
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "PackageCog Apply (Store Package)",
                                $"Critical failure during install of {Target.StoreProductId}.",
                                LogMessageSeverity.Error,
                                ex);

                            return new CogOperationResult(false, "EXCEPTION_THROWN", false);
                        }
                    }
                default:
                    throw new InvalidOperationException($"Unexpected TargetType: {Target.TargetType}");
            }
        }

        public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
        {
            // Check if managing Store apps is enabled in settings. If Rebound shouldn't manage Store apps, mark this cog as ignorable to skip it in the workflow.
            var ignorable = DoPackageManagementOn switch
            {
                PackageManagementTriggeredOn.Apply => false,
                PackageManagementTriggeredOn.Remove => true,
                PackageManagementTriggeredOn.Both => false,
                PackageManagementTriggeredOn.FollowConfiguration => !SettingsManager.GetValue("ManageStoreApps", "rebound", true),
                _ => throw new InvalidOperationException($"Unexpected DoPackageManagementOn value: {DoPackageManagementOn}")
            };

            // If the cog is ignorable based on the trigger configuration, skip the installation logic and return a successful result with ignorable set to true.
            if (ignorable)
                return new(true, null, true, true);

            switch (Target.TargetType)
            {
                case PackageTargetType.Local:
                    {
                        try
                        {
                            ReboundLogger.WriteToLog("PackageCog (Local Package)", $"Starting uninstallation from {Target.TargetPath}.");

                            // Create a PackageManager instance and find the package by family name. Then remove it by full name. This will handle both local and remote packages, and can also accept unsigned packages.
                            var packageManager = new PackageManager();
                            var packages = packageManager.FindPackagesForUser(string.Empty)
                                .Where(p => p.Id.FamilyName == Target.PackageFamilyName);

                            // Uninstall each matching package
                            foreach (var package in packages)
                            {
                                var result = await packageManager.RemovePackageAsync(package.Id.FullName).AsTask(cancellationToken).ConfigureAwait(true);

                                if (result.ExtendedErrorCode != null)
                                {
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Uninstallation (Local Package)",
                                        $"Failed to uninstall {Target.PackageFamilyName}.",
                                        LogMessageSeverity.Error,
                                        result.ExtendedErrorCode);
                                    return new(false, result.ExtendedErrorCode.ToString(), false);
                                }

                                if (result.IsRegistered)
                                {
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Uninstallation (Local Package)",
                                        $"Failed to unregister {Target.PackageFamilyName}.",
                                        LogMessageSeverity.Error,
                                        result.ExtendedErrorCode);
                                    return new(false, result.ExtendedErrorCode?.ToString(), false);
                                }
                            }

                            // Success state
                            ReboundLogger.WriteToLog("PackageCog Uninstallation (Local Package)", $"Successfully uninstalled package: {Target.PackageFamilyName}");
                            return new(true, null, true);
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "PackageCog Installation (Local Package)",
                                $"Installation failed for {Target.PackageFamilyName}.",
                                LogMessageSeverity.Error,
                                ex);
                            return new(true, null, true);
                        }
                    }
                case PackageTargetType.Store:
                    {
                        try
                        {
                            ReboundLogger.WriteToLog("PackageCog Remove (Store Package)", $"Starting Store uninstall for product ID: {Target.StoreProductId}");

                            // Obtain the current store context
                            var storeContext = StoreContext.GetDefault();

                            // Initialize with the current window handle to ensure any UI prompts are correctly parented
                            InitializeWithWindow.Initialize(storeContext, Process.GetCurrentProcess().MainWindowHandle);

                            // Attempt to uninstall the package by Store ID. This will handle cases where the package is installed, and will return an appropriate status if it's not found or if there's an error.
                            var result = await storeContext.UninstallStorePackageByStoreIdAsync(Target.StoreProductId).AsTask(cancellationToken).ConfigureAwait(false);

                            // Handle errors and success states based on the returned status
                            switch (result.Status)
                            {
                                case StoreUninstallStorePackageStatus.Succeeded:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Uninstall (Store Package)",
                                        $"Successfully uninstalled {Target.StoreProductId}.",
                                        LogMessageSeverity.Message);

                                    return new CogOperationResult(true, null, true);
                                case StoreUninstallStorePackageStatus.NetworkError:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Uninstall (Store Package)",
                                        $"Network error occurred while trying to uninstall {Target.StoreProductId}.",
                                        LogMessageSeverity.Warning);

                                    return new CogOperationResult(false, "NETWORK_ERROR", false);
                                default:
                                    ReboundLogger.WriteToLog(
                                        "PackageCog Uninstall (Store Package)",
                                        $"An unknown status was returned when trying to uninstall {Target.StoreProductId}. Status: {result.Status}",
                                        LogMessageSeverity.Error);

                                    return new CogOperationResult(false, "UNKNOWN_ERROR", false);
                            }
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.WriteToLog(
                                "PackageCog Remove (Store Package)",
                                $"Critical failure during uninstall of {Target.StoreProductId}.",
                                LogMessageSeverity.Error,
                                ex);

                            return new CogOperationResult(false, "EXCEPTION_THROWN", false);
                        }
                    }
                default:
                    throw new InvalidOperationException($"Unexpected TargetType: {Target.TargetType}");
            }
        }

        public async Task<CogStatus> GetStatusAsync()
        {
            // Check if managing Store apps is enabled in settings. If Rebound shouldn't manage Store apps, mark this cog as ignorable to skip it in the workflow.
            var ignorable = DoPackageManagementOn switch
            {
                PackageManagementTriggeredOn.FollowConfiguration => !SettingsManager.GetValue("ManageStoreApps", "rebound", true),
                _ => false
            };

            // If the cog is ignorable based on the trigger configuration, skip the installation logic and return a successful result with ignorable set to true.
            if (ignorable)
                return new(CogState.Ignorable);

            try
            {
                ReboundLogger.WriteToLog("PackageCog Get Status", $"Starting Store uninstall for product ID: {Target.StoreProductId}");

                // Obtain the current store context
                var packageManager = new PackageManager();
                var currentSid = WindowsIdentity.GetCurrent().User?.Value;
                if (currentSid == null)
                {
                    ReboundLogger.WriteToLog("PackageCog Get Status", "Could not get current user SID.");
                    return new(CogState.Unknown, "An error occurred.");
                }

                // Filter installed packages by PackageFamilyName instead of FullName
                var packages = packageManager.FindPackagesForUser(currentSid, Target.PackageFamilyName);
                bool installed = packages.Any(); // Any version installed is fine

                ReboundLogger.WriteToLog("PackageCog Get Status", $"IsApplied check: {(installed ? "Installed" : "Not installed")}");
                return new CogStatus(installed ? CogState.Installed : CogState.NotInstalled);
            }
            catch (Exception ex)
            {
                ReboundLogger.WriteToLog(
                    "PackageCog Get Status",
                    $"Critical failure during uninstall of {Target.StoreProductId}.",
                    LogMessageSeverity.Error,
                    ex);

                return new CogStatus(CogState.Unknown, "An error occurred.");
            }
        }
    }
}
