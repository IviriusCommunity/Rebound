// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge;

/// <summary>
/// The state in which a Rebound mod is found.
/// </summary>
public enum ModIntegrity
{
    /// <summary>
    /// Every cog of the mod is installed and configured properly.
    /// </summary>
    Installed,

    /// <summary>
    /// One or more cogs of the mod are missing, while the rest are installed.
    /// </summary>
    Corrupt,

    /// <summary>
    /// None of the mod's cogs are installed.
    /// </summary>
    NotInstalled
}

/// <summary>
/// Installation presets for quick configuration.
/// </summary>
public enum InstallationTemplate
{
    /// <summary>
    /// Must be used for every item in <see cref="Catalog.MandatoryMods"/>.
    /// </summary>
    Mandatory,

    /// <summary>
    /// Mods that represent the core Rebound experience.
    /// </summary>
    Basic,
    
    /// <summary>
    /// Recommended configuration of Rebound mods.
    /// </summary>
    Recommended,

    /// <summary>
    /// The complete set of Rebound mods.
    /// </summary>
    Complete,

    /// <summary>
    /// Includes additional mods that are not essential.
    /// </summary>
    Extras
}

/// <summary>
/// Common interface for Rebound modding cogs, used to define a set of
/// instructions for each mod to apply
/// </summary>
public interface ICog
{
    /// <summary>
    /// Applies the current cog.
    /// </summary>
    Task ApplyAsync();

    /// <summary>
    /// Removes the current cog.
    /// </summary>
    Task RemoveAsync();

    /// <summary>
    /// Checks if the current cog is applied.
    /// </summary>
    /// <returns>A bool determining if the cog is applied or not.</returns>
    Task<bool> IsAppliedAsync();

    /// <summary>
    /// Gets or sets a value indicating whether the cog can be ignored during installation integrity checks.
    /// When set to <see langword="true"/>, the cog's integrity will be excluded from the
    /// overall integrity calculation of the associated <see cref="Mod"/>.
    /// </summary>
    /// <remarks>
    /// It is recommended to set this property to <see langword="true"/> only if
    /// <see cref="IsAppliedAsync"/> can return either <see langword="true"/> or
    /// <see langword="false"/> regardless of the <see cref="Mod"/>'s state.
    /// For example, mods that serve solely as static actions.
    /// </remarks>
    bool Ignorable { get; }

    /// <summary>
    /// The description of what this cog does. Used for automating the tasks list description instead of
    /// writing what a mod does manually.
    /// </summary>
    string TaskDescription { get; }
}