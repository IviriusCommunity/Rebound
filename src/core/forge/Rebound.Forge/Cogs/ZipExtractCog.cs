// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.IO.Compression;

#pragma warning disable CA1031 // Do not catch general exception types

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
    public string CogDescription { get => $"Extract zip file '{ZipFilePath}' to '{DestinationFolder}'."; }

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
            ReboundLogger.WriteToLog(
                "ZipExtractCog Apply",
                "Apply started for ZipExtractCog with zip file: " + ZipFilePath);

            // Ensure destination folder exists
            Directory.CreateDirectory(DestinationFolder);

            // Copy the zip file to destination folder
            var destZipPath = Path.Combine(DestinationFolder, Path.GetFileName(ZipFilePath));
            File.Copy(ZipFilePath, destZipPath, overwrite: true);

            // Extract the zip contents into the destination folder (overwrite existing files)
            await ZipFile.ExtractToDirectoryAsync(destZipPath, DestinationFolder, overwriteFiles: true, cancellationToken: cancellationToken).ConfigureAwait(true);

            // Delete the zip file after extraction
            File.Delete(destZipPath);

            ReboundLogger.WriteToLog(
                "ZipExtractCog Apply",
                "Apply completed successfully for ZipExtractCog with zip file: " + ZipFilePath);

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ZipExtractCog Apply", 
                $"Failed to apply ZipExtractCog.",
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
            ReboundLogger.WriteToLog(
                "ZipExtractCog Remove",
                "Remove started for ZipExtractCog with destination folder: " + DestinationFolder);

            // Remove the entire extracted destination folder
            if (Directory.Exists(DestinationFolder))
                Directory.Delete(DestinationFolder, recursive: true);
            else
            {
                ReboundLogger.WriteToLog(
                    "ZipExtractCog Remove",
                    $"Directory not found during removal of ZipExtractCog: {DestinationFolder}",
                    LogMessageSeverity.Warning
                );
                return new(false, "Directory not found.", true);
            }

            ReboundLogger.WriteToLog(
                "ZipExtractCog Remove",
                "Remove completed successfully for ZipExtractCog with destination folder: " + DestinationFolder);

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ZipExtractCog Remove",
                $"Failed to remove ZipExtractCog.",
                LogMessageSeverity.Error,
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
        // Some ZIP extract tasks simply don't require persistence.
        => new(CogState.Ignorable);
}