// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a folder to the target path recursively.
/// </summary>
public partial class FolderCog : ICog
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
    /// The full path to the directory.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Determines if the folder should be deleted or not when <see cref="RemoveAsync"/> is called.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="Ignorable"/>. It is recommended to set <see cref="Ignorable"/>
    /// to <see langword="true"/> if this is also set to <see langword="true"/>.
    /// </remarks>
    public required bool PersistAfterRemoving { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog("FolderCog", $"Creating folder at {Path}...");

            DirectoryEx.Create(Path);

            ReboundLogger.WriteToLog("FolderCog", $"Folder created at {Path}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog",
                $"Failed to create folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog("FolderCog", $"Deleting folder at {Path}...");

            // Persistence is enabled, skip deletion
            if (PersistAfterRemoving)
            {
                ReboundLogger.WriteToLog("FolderCog", $"Skipping deletion of folder at {Path} due to PersistAfterRemoving being set to true.");
                return new(false, null, true, true);
            }

            Directory.Delete(Path);

            ReboundLogger.WriteToLog("FolderCog", $"Folder deleted at {Path}.");
            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog",
                $"Failed to delete folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog("FolderCog", $"Checking status of folder at {Path}...");

            bool exists = Directory.Exists(Path);
            ReboundLogger.WriteToLog("FolderCog", $"Folder at {Path} {(exists ? "exists" : "does not exist")}.");
            return new(exists ? CogState.Installed : CogState.NotInstalled);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "FolderCog",
                $"Failed to check status of folder at {Path}.",
                LogMessageSeverity.Error,
                ex);

            return new CogStatus(CogState.Unknown, ex.Message);
        }
    }
}