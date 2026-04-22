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
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

    /// <summary>
    /// The path to the original file.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// The target path to copy the file to.
    /// </summary>
    public required string TargetPath { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog("LauncherCog", "Apply started.");

            // The actual file copy operation (with overwrite)
            FileEx.Copy(Path, TargetPath, true);

            ReboundLogger.WriteToLog("LauncherCog", $"Copied file from {Path} to {TargetPath}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherCog", 
                $"Apply failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog("LauncherCog", "Remove started.");

            if (File.Exists(TargetPath))
            {
                File.Delete(TargetPath);
                ReboundLogger.WriteToLog("LauncherCog", $"Deleted file at {TargetPath}.");
                return new(true, null, true);
            }
            else
            {
                ReboundLogger.WriteToLog("LauncherCog", "No file found to delete.");
                return new(false, "FILE_NOT_FOUND", true, true);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherCog", 
                "Remove failed with exception.", 
                LogMessageSeverity.Error, 
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            bool exists = File.Exists(TargetPath);
            ReboundLogger.WriteToLog("LauncherCog", $"IsApplied check: {TargetPath} exists? {exists}");
            return new(exists ? CogState.Installed : CogState.NotInstalled);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherCog", 
                "IsApplied failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return new(CogState.Unknown, ex.Message);
        }
    }
}