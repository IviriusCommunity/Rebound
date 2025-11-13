// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.UI;
using System.Security.Principal;
using Windows.Management.Deployment;

namespace Rebound.Forge.Cogs
{
    /// <summary>
    /// Handles the installation, removal, and verification of sideloaded MSIX/AppX packages.
    /// </summary>
    public class PackageCog : ICog
    {
        /// <summary>
        /// URI to the MSIX or APPX package (supports http, https, and file schemes).
        /// Example: https://example.com/MyApp.msix
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        public required string PackageURI { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// The package family name (as seen in PackageManager or PowerShell Get-AppxPackage).
        /// </summary>
        public required string PackageFamilyName { get; set; }

        /// <inheritdoc/>
        public bool Ignorable { get; }

        /// <inheritdoc/>
        public async Task ApplyAsync()
        {
            try
            {
                ReboundLogger.Log($"[PackageCog] Starting installation from {PackageURI}.");

                var packageManager = new PackageManager();
                var deployment = packageManager.AddPackageByUriAsync(new Uri(PackageURI), new() { AllowUnsigned = true });

                deployment.Progress += (sender, progress) =>
                    ReboundLogger.Log($"[PackageCog] Deployment progress: {progress.percentage}%");

                await deployment;

                ReboundLogger.Log($"[PackageCog] Successfully installed package: {PackageFamilyName}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] Installation failed for {PackageFamilyName}.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync()
        {
            try
            {
                ReboundLogger.Log($"[PackageCog] Removing package: {PackageFamilyName}");

                var packageManager = new PackageManager();
                var packages = packageManager.FindPackagesForUser(string.Empty)
                                             .Where(p => p.Id.FamilyName == PackageFamilyName);

                foreach (var package in packages)
                {
                    await packageManager.RemovePackageAsync(package.Id.FullName);
                }

                ReboundLogger.Log($"[PackageCog] Successfully removed package: {PackageFamilyName}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] Removal failed for {PackageFamilyName}.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsAppliedAsync()
        {
            try
            {
                var packageManager = new PackageManager();
                var currentSid = WindowsIdentity.GetCurrent().User?.Value;
                if (currentSid == null)
                {
                    ReboundLogger.Log("[PackageCog] Could not get current user SID.");
                    return false;
                }

                // Filter installed packages by PackageFamilyName instead of FullName
                var packages = packageManager.FindPackagesForUser(currentSid, PackageFamilyName);
                bool installed = packages.Any(); // Any version installed is fine

                ReboundLogger.Log($"[PackageCog] IsApplied check: {(installed ? "Installed" : "Not installed")}");
                return installed;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] IsApplied check failed for {PackageFamilyName}.", ex);
                return false;
            }
        }
    }
}
