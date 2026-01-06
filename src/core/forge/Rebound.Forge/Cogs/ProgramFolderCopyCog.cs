// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents an operation that copies files and directories from a specified source path to a destination path as part
/// of a program folder setup task.
/// </summary>
/// <remarks>Use this class to automate the copying of program folders during application setup or deployment. The
/// source and destination paths must be valid file system locations accessible to the application. This class
/// implements the <see cref="ICog"/> interface, allowing it to participate in a sequence of setup or configuration
/// tasks.</remarks>
public partial class ProgramFolderCopyCog : ICog
{
    /// <summary>
    /// Gets or sets the source path from which files and directories will be copied.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the destination file or directory path where the output will be saved.
    /// </summary>
    /// <remarks>The path must be a valid file system location. Relative or absolute paths are supported,
    /// depending on the application's context. Ensure that the application has write permissions to the specified
    /// location.</remarks>
    public required string DestinationPath { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription { get => $"Copy folder {Path} to {DestinationPath}"; }

    /// <summary>
    /// Creates a new instance of the <see cref="FolderCog"/> class.
    /// </summary>
    public ProgramFolderCopyCog() { }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        DirectoryEx.Copy(Path, DestinationPath);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        Directory.Delete(DestinationPath);
        await Task.Delay(500);
        Directory.Delete(DestinationPath);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return Directory.Exists(DestinationPath);
    }
}