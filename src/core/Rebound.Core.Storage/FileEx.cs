// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Storage;

public static class FileEx
{
    /// <summary>
    /// Copies a file to a destination, creating the destination folder if it does not exist.
    /// </summary>
    /// <param name="source">Path to the original file</param>
    /// <param name="destination">Target path for the file to be copied to</param>
    public static void Copy(string source, string destination)
    {
        var destinationFolderPath = Path.GetDirectoryName(destination);
        DirectoryEx.Create(destinationFolderPath!);
        File.Copy(source, destination);
    }
}