// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Storage;

public static class DirectoryEx
{
    private static readonly char[] separator = ['\\', '/'];

    /// <summary>
    /// Copies all files and subdirectories from the specified source directory to the specified destination directory.
    /// Existing files in the destination are overwritten.
    /// </summary>
    /// <remarks>All files and subdirectories within the source directory are recursively copied to the
    /// destination directory. File attributes and timestamps are not preserved. If a file with the same name exists in
    /// the destination, it will be overwritten.</remarks>
    /// <param name="sourceDir">The path of the directory to copy from. Must refer to an existing directory.</param>
    /// <param name="destDir">The path of the directory to copy to. If the directory does not exist, it will be created.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceDir"/> or <paramref name="destDir"/> is null, empty, or consists only of
    /// white-space characters.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="sourceDir"/> does not refer to an existing directory.</exception>
    public static void Copy(string sourceDir, string destDir)
    {
        if (string.IsNullOrWhiteSpace(sourceDir))
            throw new ArgumentNullException(nameof(sourceDir));

        if (string.IsNullOrWhiteSpace(destDir))
            throw new ArgumentNullException(nameof(destDir));

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        // Ensure root destination exists
        Create(destDir);

        // Copy all files in current directory
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(destDir, fileName);
            FileEx.Copy(file, destPath);
        }

        // Recurse into subdirectories
        foreach (var subdir in Directory.GetDirectories(sourceDir))
        {
            var subdirName = Path.GetFileName(subdir);
            var destSubdir = Path.Combine(destDir, subdirName);
            Copy(subdir, destSubdir);
        }
    }

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