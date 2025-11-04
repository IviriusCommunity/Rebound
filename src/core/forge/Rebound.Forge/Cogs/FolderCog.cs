// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Storage;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a folder to the target path recursively
/// </summary>
public partial class FolderCog : ICog
{
    /// <summary>
    /// The full path to the directory
    /// </summary>
    public required string Path { get; set; }

    public required bool AllowPersistence { get; set; }

    public bool Ignorable { get; }

    public FolderCog() { }

    public async Task ApplyAsync()
    {
        DirectoryEx.Create(Path);
    }

    public async Task RemoveAsync()
    {
        if (!AllowPersistence) Directory.Delete(Path);
    }

    public async Task<bool> IsAppliedAsync()
    {
        return Directory.Exists(Path);
    }
}