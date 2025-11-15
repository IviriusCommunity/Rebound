// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.UI;
using System.Diagnostics;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Services.Store;
using WinRT.Interop;

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
        public string TaskDescription { get => $"Install the package {PackageURI}"; }

        private bool? _expectInstalled;

        /// <inheritdoc/>
        public async Task ApplyAsync()
        {
            try
            {
                _expectInstalled = true;

                ReboundLogger.Log($"[PackageCog] Starting installation from {PackageURI}.");

                var packageManager = new PackageManager();
                //InitializeWithWindow.Initialize(packageManager, Process.GetCurrentProcess().MainWindowHandle);
                var deployment = packageManager.AddPackageByUriAsync(new Uri(PackageURI), new() { AllowUnsigned = true });

                deployment.Progress += (sender, progress) =>
                    ReboundLogger.Log($"[PackageCog] Deployment progress: {progress.percentage}%");

                UIThreadQueue.QueueAction(async () =>
                {
                    await deployment;
                });

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
                _expectInstalled = false;

                ReboundLogger.Log($"[PackageCog] Removing package: {PackageFamilyName}");

                var packageManager = new PackageManager();
                //InitializeWithWindow.Initialize(packageManager, Process.GetCurrentProcess().MainWindowHandle);
                var packages = packageManager.FindPackagesForUser(string.Empty)
                                             .Where(p => p.Id.FamilyName == PackageFamilyName);

                foreach (var package in packages)
                {
                    UIThreadQueue.QueueAction(async () =>
                    {
                        await packageManager.RemovePackageAsync(package.Id.FullName);
                    });

                    //await packageManager.RemovePackageAsync(package.Id.FullName);
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
                var sid = WindowsIdentity.GetCurrent().User?.Value;
                if (sid == null)
                {
                    ReboundLogger.Log("[PackageCog] Could not get current user SID.");
                    return false;
                }

                // If _expectInstalled is null, just wait 1500ms and check once
                if (_expectInstalled == null)
                {
                    await Task.Delay(1500).ConfigureAwait(false);

                    IEnumerable<Package> packages = [];
                    var signal = new TaskCompletionSource<bool>();

                    UIThreadQueue.QueueAction(() =>
                    {
                        try
                        {
                            packages = packageManager.FindPackagesForUser(sid, PackageFamilyName);
                            signal.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            signal.SetException(ex);
                        }
                        return Task.CompletedTask;
                    });

                    await signal.Task.ConfigureAwait(false);
                    bool installed = packages.Any();

                    ReboundLogger.Log($"[PackageCog] IsApplied check (no expectation): {(installed ? "Installed" : "Not installed")}");
                    return installed;
                }

                // Original logic with expectation checking
                const int maxTimeMs = 10_000;
                const int intervalMs = 250;
                int waited = 0;

                while (waited < maxTimeMs)
                {
                    IEnumerable<Package> packages = [];
                    var signal = new TaskCompletionSource<bool>();

                    UIThreadQueue.QueueAction(() =>
                    {
                        try
                        {
                            packages = packageManager.FindPackagesForUser(sid, PackageFamilyName);
                            signal.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            signal.SetException(ex);
                        }
                        return Task.CompletedTask;
                    });

                    await signal.Task.ConfigureAwait(false);
                    bool installed = packages.Any();

                    // Check against expected state
                    if (installed == _expectInstalled)
                    {
                        ReboundLogger.Log(
                            $"[PackageCog] IsApplied check: {(installed ? "Installed" : "Not installed")} (matches expectation)"
                        );
                        return _expectInstalled.Value;
                    }

                    await Task.Delay(intervalMs).ConfigureAwait(false);
                    waited += intervalMs;
                }

                ReboundLogger.Log("[PackageCog] IsApplied check: timeout waiting for expected state");
                return false;
            }
            catch (Exception ex)
            {
                ReboundLogger.Log($"[PackageCog] IsApplied check failed for {PackageFamilyName}.", ex);
                return false;
            }
        }
    }
}
