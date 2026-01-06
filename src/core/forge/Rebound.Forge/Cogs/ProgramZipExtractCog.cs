// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.IO.Compression;
using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents an operation that copies a zip file from a specified source path to a destination folder,
/// then extracts the zip contents and deletes the zip file.
/// </summary>
public partial class ProgramZipExtractCog : ICog
{
    /// <summary>
    /// Gets or sets the source path of the zip file to copy.
    /// </summary>
    public required string ZipFilePath { get; set; }

    /// <summary>
    /// Gets or sets the destination folder where the zip will be extracted.
    /// </summary>
    public required string DestinationFolder { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription => $"Copy and extract zip {ZipFilePath} to {DestinationFolder}";

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ProgramZipExtractCog() { }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        // Ensure destination folder exists
        Directory.CreateDirectory(DestinationFolder);

        // Copy the zip file to destination folder
        var destZipPath = Path.Combine(DestinationFolder, Path.GetFileName(ZipFilePath));
        File.Copy(ZipFilePath, destZipPath, overwrite: true);

        // Extract the zip contents into the destination folder (overwrite existing files)
        ZipFile.ExtractToDirectory(destZipPath, DestinationFolder, overwriteFiles: true);

        // Delete the zip file after extraction
        File.Delete(destZipPath);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        // Remove the entire extracted destination folder
        if (Directory.Exists(DestinationFolder))
        {
            Directory.Delete(DestinationFolder, recursive: true);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        // Consider the task applied if the destination folder exists and contains any files
        return Directory.Exists(DestinationFolder) && Directory.EnumerateFileSystemEntries(DestinationFolder).Any();
    }
}