// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a folder to the target path recursively.
/// </summary>
public partial class FolderCog : ICog
{
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
    public required bool AllowPersistence { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="FolderCog"/> class.
    /// </summary>
    public FolderCog() { }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        DirectoryEx.Create(Path);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        if (!AllowPersistence) Directory.Delete(Path);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return Directory.Exists(Path);
    }
}