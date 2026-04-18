// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Forge.Cogs;

public enum CogState
{
    NotInstalled,
    Installed,
    PartiallyInstalled,
    Ignorable,
    Unknown
}

public record CogStatus(CogState State, string? Message = null);

public record CogOperationResult(bool Success, string? Error, bool SafeToContinue, bool Ignorable = false);

/// <summary>
/// Common interface for Rebound modding cogs, used to define a set of
/// instructions for each mod to apply
/// </summary>
public interface ICog
{
    /// <summary>
    /// Applies the current cog.
    /// </summary>
    Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the current cog.
    /// </summary>
    Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current cog is applied.
    /// </summary>
    /// <returns>A bool determining if the cog is applied or not.</returns>
    Task<CogStatus> GetStatusAsync(); // Post edit note: returns bool for yes, and uhh... ig operation result for no? idk it doesn't make sense tbh

    string CogName { get; }

    /// <summary>
    /// The description of what this cog does. Used for automating the tasks list description instead of
    /// writing what a mod does manually.
    /// </summary>
    string CogDescription { get; }

    Guid CogId { get; }
    bool RequiresElevation { get; }
}