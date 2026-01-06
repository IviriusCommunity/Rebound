// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Copies a file from one place to another, accounting for missing folders in the path.
/// </summary>
public class FileCopyCog : ICog
{
    /// <summary>
    /// The path to the original file.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// The target path to copy the file to.
    /// </summary>
    public required string TargetPath { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription { get => $"Copy {Path} to {TargetPath}"; }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        try
        {
            ReboundLogger.Log("[LauncherCog] Apply started.");
            FileEx.Copy(Path, TargetPath);
            ReboundLogger.Log($"[LauncherCog] Copied file from {Path} to {TargetPath}.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] Apply failed with exception.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        try
        {
            ReboundLogger.Log("[LauncherCog] Remove started.");

            if (File.Exists(TargetPath))
            {
                File.Delete(TargetPath);
                ReboundLogger.Log($"[LauncherCog] Deleted file at {TargetPath}.");
            }
            else
            {
                ReboundLogger.Log("[LauncherCog] No file found to delete.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] Remove failed with exception.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        try
        {
            bool exists = File.Exists(TargetPath);
            ReboundLogger.Log($"[LauncherCog] IsApplied check: {TargetPath} exists? {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] IsApplied failed with exception.", ex);
            return false;
        }
    }
}