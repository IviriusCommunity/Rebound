// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.UI;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Launches a UWP package application when applied. Ignorable.
/// </summary>
/// <remarks><see cref="IsAppliedAsync"/> will always return <see langword="true"/></remarks>
public class PackageLaunchCog : ICog
{
    /// <summary>
    /// The family name of the package you want to launch. Example: Rebound.Shell_rcz2tbwv5qzb8
    /// </summary>
    public required string PackageFamilyName { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; } = true;

    /// <inheritdoc/>
    public string TaskDescription { get => $"Launch the package {PackageFamilyName}"; }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        await Task.Delay(1500);

        ReboundLogger.Log($"[PackageLaunchCog] Launching package {PackageFamilyName}");

        var packageManager = new PackageManager();
        var package = packageManager.FindPackagesForUser("", PackageFamilyName).FirstOrDefault();

        if (package == null)
        {
            ReboundLogger.Log($"[PackageLaunchCog] Package not found");
            return;
        }

        var apps = await package.GetAppListEntriesAsync();
        if (apps.Count == 0)
        {
            ReboundLogger.Log($"[PackageLaunchCog] No apps found in package");
            return;
        }

        // Launch the first app (or iterate to find the right one)
        await apps[0].LaunchAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {

    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}