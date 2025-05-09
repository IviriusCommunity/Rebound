using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Forge;

public static class ReboundWorkingEnvironment
{
    // Folder
    public static void EnsureFolderIntegrity()
    {
        try
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
        catch
        {

        }
    }

    public static void RemoveFolder()
    {
        try
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
        catch
        {

        }
    }

    public static bool FolderExists()
    {
        try
        {
            var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");

            return Directory.Exists(directoryPath);
        }
        catch
        {
            return false;
        }
    }

    // Tasks Folder
    public static void EnsureTasksFolderIntegrity()
    {
        try
        {
            using TaskService ts = new();

            // Specify the path to the task in Task Scheduler
            _ = ts.GetFolder(@"Rebound") ?? ts.RootFolder.CreateFolder(@"Rebound");
        }
        catch
        {

        }
    }

    public static void RemoveTasksFolder()
    {
        try
        {
            using TaskService ts = new();

            // Specify the path to the task in Task Scheduler
            if (ts.GetFolder(@"Rebound") != null)
            {
                ts.RootFolder.DeleteFolder(@"Rebound", false);
            }
        }
        catch
        {

        }
    }

    public static bool TaskFolderExists()
    {
        try
        {
            using TaskService ts = new();

            return ts.GetFolder(@"Rebound") != null;
        }
        catch
        {
            return false;
        }
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