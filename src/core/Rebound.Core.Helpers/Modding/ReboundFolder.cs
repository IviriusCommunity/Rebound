using System.IO;

namespace Rebound.Helpers.Modding;

public static class ReboundFolder
{
    public static void EnsureIntegrity()
    {
        var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
        var directoryPath = Path.Combine(programFilesPath, "Rebound");

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Set attributes for the directory
        var currentAttributes = File.GetAttributes(directoryPath);

        // Ensure directory attributes are set (optional but ensures directory is recognized)
        if (!currentAttributes.HasFlag(FileAttributes.Directory))
        {
            File.SetAttributes(directoryPath, FileAttributes.Directory);
        }

        File.SetAttributes(directoryPath, currentAttributes | FileAttributes.System | FileAttributes.Hidden);
    }
}
