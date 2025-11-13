// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;

namespace Rebound.Forge.Launchers;

/// <summary>
/// Launcher class used to launch a package.
/// </summary>
public class PackageLauncher : ILauncher
{
    /// <summary>
    /// The package family name.
    /// </summary>
    public required string PackageFamilyName { get; set; }

    /// <inheritdoc/>
    public async Task LaunchAsync()
    {
        ReboundLogger.Log($"[PackageLauncher] Launching package {PackageFamilyName}");

        var packageManager = new PackageManager();
        var package = packageManager.FindPackagesForUser("", PackageFamilyName).FirstOrDefault();

        if (package == null)
        {
            ReboundLogger.Log($"[PackageLauncher] Package not found");
            return;
        }

        var apps = await package.GetAppListEntriesAsync();
        if (apps.Count == 0)
        {
            ReboundLogger.Log($"[PackageLauncher] No apps found in package");
            return;
        }

        // Launch the first app (or iterate to find the right one)
        await apps[0].LaunchAsync();
    }
}