using System;
using System.Diagnostics;
using System.IO;
using Windows.Win32;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge;

public static class ReboundWorkingEnvironment
{
    private static ComPtr<ITaskService> _taskService = default;

    public static void UpdateVersion()
    {
        try
        {
            var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");

            File.WriteAllText(Path.Combine(directoryPath, "version.txt"), $"{Helpers.Environment.ReboundVersion.REBOUND_VERSION}");
        }
        catch
        {

        }
    }

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
            // Get the start menu folder of Rebound
            var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

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

            return Directory.Exists(directoryPath) && File.Exists(Path.Combine(directoryPath, "version.txt"));
        }
        catch
        {
            return false;
        }
    }

    // Tasks Folder
    public static unsafe void EnsureTasksFolderIntegrity()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
            }
            catch (InvalidOperationException)
            {
                rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error: {ex.Message}");
        }
    }

    public static unsafe void RemoveTasksFolder()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr < 0)
                    return;
                else
                    rootFolder.Get()->DeleteFolder(pszRebound, 0); // 0 = no flags
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine($"Task Scheduler error (Remove)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error (Remove): {ex.Message}");
        }
    }

    public static unsafe bool TaskFolderExists()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                return !(hr < 0);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine($"Task Scheduler error (Exists)");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error (Exists): {ex.Message}");
            return false;
        }
    }

    // Mandatory instructions
    public static async void EnsureMandatoryInstructionsIntegrity()
    {
        try
        {
            foreach (var instructions in ReboundTotalInstructions.MandatoryInstructions)
            {
                await instructions.InstallAsync();
            }
        }
        catch
        {

        }
    }

    public static async void RemoveMandatoryInstructions()
    {
        try
        {
            foreach (var instructions in ReboundTotalInstructions.MandatoryInstructions)
            {
                await instructions.UninstallAsync();
            }
        }
        catch
        {

        }
    }

    public static bool MandatoryInstructionsExist()
    {
        try
        {
            foreach (var instructions in ReboundTotalInstructions.MandatoryInstructions)
            {
                if (instructions.GetIntegrity() != ReboundAppIntegrity.Installed)
                {
                    return false;
                }
            }
            return true;
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
        EnsureMandatoryInstructionsIntegrity();
    }

    public static void DisableRebound()
    {
        RemoveFolder();
        RemoveTasksFolder();
        RemoveMandatoryInstructions();
    }

    public static bool IsReboundEnabled() => FolderExists() && TaskFolderExists() && MandatoryInstructionsExist();
}