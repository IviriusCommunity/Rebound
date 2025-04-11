using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Helpers.Modding;

public static class ReboundWorkingEnvironment
{
    // Folder
    public static void EnsureFolderIntegrity()
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

    public static void RemoveFolder()
    {
        var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
        var directoryPath = Path.Combine(programFilesPath, "Rebound");

        // Create the directory if it doesn't exist
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    public static bool FolderExists()
    {
        var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
        var directoryPath = Path.Combine(programFilesPath, "Rebound");

        return Directory.Exists(directoryPath);
    }

    // Tasks Folder
    public static void EnsureTasksFolderIntegrity()
    {
        using TaskService ts = new();

        // Specify the path to the task in Task Scheduler
        _ = ts.GetFolder(@"Rebound") ?? ts.RootFolder.CreateFolder(@"Rebound");
    }

    public static void RemoveTasksFolder()
    {
        using TaskService ts = new();

        // Specify the path to the task in Task Scheduler
        if (ts.GetFolder(@"Rebound") != null)
        {
            ts.RootFolder.DeleteFolder(@"Rebound", false);
        }
    }

    public static bool TaskFolderExists()
    {
        using TaskService ts = new();

        return ts.GetFolder(@"Rebound") != null;
    }

    // General methods
    public static void EnableRebound()
    {
        EnsureFolderIntegrity();
        EnsureTasksFolderIntegrity();
    }

    public static void DisableRebound()
    {
        RemoveFolder();
        RemoveTasksFolder();
    }

    public static bool IsReboundEnabled() => FolderExists() && TaskFolderExists();
}