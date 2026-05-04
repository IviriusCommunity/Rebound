// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Forge.Engines;

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
    public Task LaunchAsync()
        => Task.FromResult(() => ApplicationLaunchEngine.LaunchApp(PackageFamilyName));
}