// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Environment;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using Windows.Management.Deployment;
using Windows.Services.Store;

namespace Rebound.Forge.Cogs
{
    /// <summary>
    /// Handles installation and management of Microsoft Store apps by product ID.
    /// </summary>
    internal class StorePackageCog : ICog
    {
        /// <summary>
        /// The Microsoft Store product ID.
        /// Example: 9NBLGGH4NNS1
        /// </summary>
        public required string StoreProductId { get; set; }

        /// <summary>
        /// The full package name associated with the Store app.
        /// Example: Microsoft.MSPaint_2022.2310.1.0_x64__8wekyb3d8bbwe
        /// </summary>
        public required string PackageFullName { get; set; }

        public async void Apply()
        {
            try
            {
                ReboundLogger.Log($"[StorePackageCog] Starting Store install for product ID: {StoreProductId}");

                var user = await WindowsEnvironment.GetCurrentUserAsync().ConfigureAwait(false);
                if (user == null)
                {
                    ReboundLogger.Log("[StorePackageCog] Could not resolve current user for StoreContext.");
                    return;
                }

                var storeContext = StoreContext.GetForUser(user);
                var result = await storeContext.DownloadAndInstallStorePackagesAsync(new List<string> { StoreProductId });

                ReboundLogger.Log($"[StorePackageCog] Store install completed with status: {result.OverallState}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] Installation failed for {StoreProductId}.", ex);
            }
        }

        public async void Remove()
        {
            try
            {
                ReboundLogger.Log($"[StorePackageCog] Attempting to uninstall Store package: {StoreProductId}");

                var user = await WindowsEnvironment.GetCurrentUserAsync().ConfigureAwait(false);
                if (user == null)
                {
                    ReboundLogger.Log("[StorePackageCog] Could not resolve current user for StoreContext.");
                    return;
                }

                var storeContext = StoreContext.GetForUser(user);
                var result = await storeContext.UninstallStorePackageByStoreIdAsync(StoreProductId);

                ReboundLogger.Log($"[StorePackageCog] Uninstall completed with status: {result.Status}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] Uninstall failed for {StoreProductId}.", ex);
            }
        }

        public bool IsApplied()
        {
            try
            {
                var packageManager = new PackageManager();
                var currentSid = WindowsIdentity.GetCurrent().User?.Value;
                var package = packageManager.FindPackageForUser(currentSid!, PackageFullName);

                bool installed = package != null;
                ReboundLogger.Log($"[StorePackageCog] IsApplied check: {(installed ? "Installed" : "Not installed")}");

                return installed;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] IsApplied check failed for {PackageFullName}.", ex);
                return false;
            }
        }
    }
}
