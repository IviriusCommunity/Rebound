// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;
using Windows.Services.Store;
using Windows.UI.Core;
using WinRT;
using WinRT.Interop;

namespace Rebound.Forge.Cogs
{
    /// <summary>
    /// Handles installation and management of Microsoft Store apps by product ID.
    /// </summary>
    public class StorePackageCog : ICog
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
        public required string PackageFamilyName { get; set; }

        public async Task ApplyAsync()
        {
            try
            {
                ReboundLogger.Log($"[StorePackageCog] Starting Store install for product ID: {StoreProductId}");

                var storeContext = StoreContext.GetDefault();
                InitializeWithWindow.Initialize(storeContext, Process.GetCurrentProcess().MainWindowHandle);
                var result = await storeContext.DownloadAndInstallStorePackagesAsync(new List<string> { StoreProductId });

                ReboundLogger.Log($"[StorePackageCog] Store install completed with status: {result.OverallState}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] Installation failed for {StoreProductId}.", ex);
            }
        }

        public async Task RemoveAsync()
        {
            try
            {
                ReboundLogger.Log($"[StorePackageCog] Attempting to uninstall Store package: {StoreProductId}");

                var storeContext = StoreContext.GetDefault();
                InitializeWithWindow.Initialize(storeContext, Process.GetCurrentProcess().MainWindowHandle);
                var result = await storeContext.UninstallStorePackageByStoreIdAsync(StoreProductId);

                ReboundLogger.Log($"[StorePackageCog] Uninstall completed with status: {result.Status}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] Uninstall failed for {StoreProductId}.", ex);
            }
        }

        public async Task<bool> IsAppliedAsync()
        {
            try
            {
                var packageManager = new PackageManager();
                var currentSid = WindowsIdentity.GetCurrent().User?.Value;
                if (currentSid == null)
                {
                    ReboundLogger.Log("[StorePackageCog] Could not get current user SID.");
                    return false;
                }

                // Filter installed packages by PackageFamilyName instead of FullName
                var packages = packageManager.FindPackagesForUser(currentSid, PackageFamilyName);
                bool installed = packages.Any(); // Any version installed is fine

                ReboundLogger.Log($"[StorePackageCog] IsApplied check: {(installed ? "Installed" : "Not installed")}");
                return installed;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[StorePackageCog] IsApplied check failed for {PackageFamilyName}.", ex);
                return false;
            }
        }
    }
}
