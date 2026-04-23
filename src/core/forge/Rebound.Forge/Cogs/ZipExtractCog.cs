// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.IO.Compression;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents an operation that copies a zip file from a specified source path to a destination folder,
/// then extracts the zip contents and deletes the zip file.
/// </summary>
public partial class ZipExtractCog : ICog
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
    /// Gets or sets the source path of the zip file to copy.
    /// </summary>
    public required string ZipFilePath { get; set; }

    /// <summary>
    /// Gets or sets the destination folder where the zip will be extracted.
    /// </summary>
    public required string DestinationFolder { get; set; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure destination folder exists
            Directory.CreateDirectory(DestinationFolder);

            // Copy the zip file to destination folder
            var destZipPath = Path.Combine(DestinationFolder, Path.GetFileName(ZipFilePath));
            File.Copy(ZipFilePath, destZipPath, overwrite: true);

            // Extract the zip contents into the destination folder (overwrite existing files)
            await ZipFile.ExtractToDirectoryAsync(destZipPath, DestinationFolder, overwriteFiles: true, cancellationToken: cancellationToken).ConfigureAwait(true);

            // Delete the zip file after extraction
            File.Delete(destZipPath);

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("ZipExtractCog", $"Failed to apply ZipExtractCog: {ex.Message}", LogMessageSeverity.Error, ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove the entire extracted destination folder
            if (Directory.Exists(DestinationFolder))
            {
                Directory.Delete(DestinationFolder, recursive: true);
            }
            else
            {
                return new(false, "Directory not found.", true);
            }

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("ZipExtractCog", $"Failed to remove ZipExtractCog: {ex.Message}", LogMessageSeverity.Error, ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            // Consider the task applied if the destination folder exists and contains any files
            var exists = Directory.Exists(DestinationFolder) && Directory.EnumerateFileSystemEntries(DestinationFolder).Any();

            return new(exists ? CogState.Installed : CogState.NotInstalled, exists ? null : "Directory doesn't exist.");
        }
        catch
        {
            return new(CogState.Unknown, "Failed to get status.");
        }
    }
}