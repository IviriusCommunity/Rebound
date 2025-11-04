// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge;

public enum ModIntegrity
{
    Installed,
    Corrupt,
    NotInstalled
}

public enum InstallationTemplate
{
    Basic,
    Recommended,
    Complete,
    Extras
}

/// <summary>
/// Commom interface for Rebound modding cogs, used to define a set of
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
}