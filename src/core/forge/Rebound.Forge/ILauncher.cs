// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge;

/// <summary>
/// Interface used for launch operations inside <see cref="Mod"/>.
/// </summary>
public interface ILauncher
{
    /// <summary>
    /// Launches an application.
    /// </summary>
    /// <returns>An object corresponding to the current task.</returns>
    Task LaunchAsync();
}