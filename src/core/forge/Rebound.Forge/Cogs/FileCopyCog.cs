// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Copies a file or folder from one place to another, accounting for missing folders in the path.
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
    public string CogDescription { get => $"Copy {(IsDirectory ? "directory" : "file")} from {Path} to {TargetPath}."; }

    /// <summary>
    /// The path to the original file or directory.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// The target path to copy the file or directory to.
    /// </summary>
    public required string TargetPath { get; set; }

    /// <summary>
    /// Indicates whether the source represents a directory instead of a file.
    /// </summary>
    public required bool IsDirectory { get; set; }

    /// <inheritdoc/>
    public Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FileCopyCog Apply",
                "Apply started.");

            switch (IsDirectory)
            {
                case true:
                    DirectoryEx.Copy(Path, TargetPath);
                    break;
                case false:
                    FileEx.Copy(Path, TargetPath, true);
                    break;
            }

            ReboundLogger.WriteToLog(
                "FileCopyCog Apply",
                $"Copied {(IsDirectory ? "directory" : "file")} from {Path} to {TargetPath}.");

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FileCopyCog Apply", 
                $"Apply failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FileCopyCog Remove", 
                "Remove started.");

            switch (IsDirectory)
            {
                case true:
                    if (Directory.Exists(TargetPath))
                    {
                        Directory.Delete(TargetPath, true);
                        ReboundLogger.WriteToLog(
                            "FileCopyCog Remove", 
                            $"Deleted directory at {TargetPath}.");
                        return Task.FromResult(new CogOperationResult(true, null, true));
                    }
                    else
                    {
                        ReboundLogger.WriteToLog(
                            "FileCopyCog Remove", 
                            "No directory found to delete.");
                        return Task.FromResult(new CogOperationResult(false, "DIRECTORY_NOT_FOUND", true, true));
                    }
                case false:
                    if (File.Exists(TargetPath))
                    {
                        File.Delete(TargetPath);
                        ReboundLogger.WriteToLog(
                            "FileCopyCog Remove",
                            $"Deleted file at {TargetPath}.");
                        return Task.FromResult(new CogOperationResult(true, null, true));
                    }
                    else
                    {
                        ReboundLogger.WriteToLog(
                            "FileCopyCog Remove", 
                            "No file found to delete.");
                        return Task.FromResult(new CogOperationResult(false, "FILE_NOT_FOUND", true, true));
                    }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FileCopyCog Remove", 
                "Remove failed with exception.", 
                LogMessageSeverity.Error, 
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "FileCopyCog GetStatus", 
                "GetStatus started.");

            bool exists = IsDirectory ? 
                Directory.Exists(TargetPath) : 
                File.Exists(TargetPath);

            ReboundLogger.WriteToLog(
                "FileCopyCog GetStatus",
                $"IsApplied check: {TargetPath} exists? {exists}");
            return Task.FromResult(new CogStatus(exists ? CogState.Installed : CogState.NotInstalled));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherCog", 
                "IsApplied failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, ex.Message));
        }
    }
}