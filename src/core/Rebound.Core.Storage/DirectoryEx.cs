// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Storage;

public static class DirectoryEx
{
    private static readonly char[] separator = ['\\', '/'];

    /// <summary>
    /// Creates all directories and subdirectories in the specified path if they do not already exist.
    /// </summary>
    /// <param name="path">The path of the directory to be created</param>
    public static void Create(string path)
    {
        var parts = path?.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList()!;

        for (int i = 0; i < parts.Count; i++)
        {
            string currentPath = Path.Combine(parts.Take(i + 1).ToArray());
            if (!Directory.Exists(currentPath))
            {
                Directory.CreateDirectory(currentPath);
                File.SetAttributes(currentPath, FileAttributes.Directory);
            }
        }
    }
}