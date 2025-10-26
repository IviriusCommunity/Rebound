// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using System;
using System.Security.Principal;
using Windows.Management.Deployment;

namespace Rebound.Forge.Cogs
{
    /// <summary>
    /// Handles the installation, removal, and verification of sideloaded MSIX/AppX packages.
    /// </summary>
    internal class PackageCog : ICog
    {
        /// <summary>
        /// URI to the MSIX or APPX package (supports http, https, and file schemes).
        /// Example: https://example.com/MyApp.msix
        /// </summary>
        public required string PackageURI { get; set; }

        /// <summary>
        /// The package full name (as seen in PackageManager or PowerShell Get-AppxPackage).
        /// Example: Contoso.MyApp_1.0.0.0_x64__8wekyb3d8bbwe
        /// </summary>
        public required string PackageFullName { get; set; }

        public async void Apply()
        {
            try
            {
                ReboundLogger.Log($"[PackageCog] Starting installation from {PackageURI}.");

                var packageManager = new PackageManager();
                var deployment = packageManager.AddPackageByUriAsync(new Uri(PackageURI), new() { AllowUnsigned = true });

                deployment.Progress += (sender, progress) =>
                    ReboundLogger.Log($"[PackageCog] Deployment progress: {progress.percentage}%");

                await deployment;

                ReboundLogger.Log($"[PackageCog] Successfully installed package: {PackageFullName}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] Installation failed for {PackageFullName}.", ex);
            }
        }

        public async void Remove()
        {
            try
            {
                ReboundLogger.Log($"[PackageCog] Removing package: {PackageFullName}");

                var packageManager = new PackageManager();
                await packageManager.RemovePackageAsync(PackageFullName);

                ReboundLogger.Log($"[PackageCog] Successfully removed package: {PackageFullName}");
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] Removal failed for {PackageFullName}.", ex);
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
                ReboundLogger.Log($"[PackageCog] IsApplied check: {(installed ? "Installed" : "Not installed")}");

                return installed;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] IsApplied check failed for {PackageFullName}.", ex);
                return false;
            }
        }
    }
}
