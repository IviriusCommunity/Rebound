using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Forge;

public static class ReboundWorkingEnvironment
{
    // Folder
    public static void EnsureFolderIntegrity()
    {
        var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
        var directoryPath = Path.Combine(programFilesPath, "Rebound");

        // Get the start menu folder of Rebound
        var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (!Directory.Exists(startMenuFolder))
        {
            Directory.CreateDirectory(startMenuFolder);
        }

        File.SetAttributes(directoryPath, FileAttributes.Directory);
        File.SetAttributes(startMenuFolder, FileAttributes.Directory);
    }

    public static void RemoveFolder()
    {
        var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
        var directoryPath = Path.Combine(programFilesPath, "Rebound");

        // Get the start menu folder of Rebound
        var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

        // Create the directory if it doesn't exist
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }

        // Create the directory if it doesn't exist
        if (Directory.Exists(startMenuFolder))
        {
            Directory.Delete(startMenuFolder, true);
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