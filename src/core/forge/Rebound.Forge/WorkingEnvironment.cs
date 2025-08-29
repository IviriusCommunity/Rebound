﻿using System;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge;

internal static class WorkingEnvironment
{
    public static readonly string StartMenuFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                     "Programs", "Rebound");

    public static void UpdateVersion()
    {
        try
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");
            var versionFile = Path.Combine(directoryPath, "version.txt");

            File.WriteAllText(versionFile, $"{Core.Helpers.Environment.ReboundVersion.REBOUND_VERSION}");
            ReboundLogger.Log($"[WorkingEnvironment] Updated version.txt to {Core.Helpers.Environment.ReboundVersion.REBOUND_VERSION}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to update version.txt:", ex);
        }
    }

    // Folder
    public static void EnsureFolderIntegrity()
    {
        try
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");

            var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                ReboundLogger.Log($"[WorkingEnvironment] Created directory: {directoryPath}");
            }

            if (!Directory.Exists(startMenuFolder))
            {
                Directory.CreateDirectory(startMenuFolder);
                ReboundLogger.Log($"[WorkingEnvironment] Created start menu folder: {startMenuFolder}");
            }

            File.SetAttributes(directoryPath, FileAttributes.Directory);
            File.SetAttributes(startMenuFolder, FileAttributes.Directory);
            ReboundLogger.Log("[WorkingEnvironment] Folder integrity ensured.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to ensure folder integrity:", ex);
        }
    }

    public static void RemoveFolder()
    {
        try
        {
            var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

            if (Directory.Exists(startMenuFolder))
            {
                Directory.Delete(startMenuFolder, true);
                ReboundLogger.Log($"[WorkingEnvironment] Removed start menu folder: {startMenuFolder}");
            }
            else
            {
                ReboundLogger.Log($"[WorkingEnvironment] Start menu folder does not exist, nothing to remove: {startMenuFolder}");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to remove start menu folder:", ex);
        }
    }

    public static bool FolderExists()
    {
        try
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");
            var versionFile = Path.Combine(directoryPath, "version.txt");

            bool exists = Directory.Exists(directoryPath) && File.Exists(versionFile);
            ReboundLogger.Log($"[WorkingEnvironment] Folder exists check: {directoryPath} → Exists = {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Folder exists check failed:", ex);
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

            // Create and connect ITaskService
            using ComPtr<ITaskService> taskService = default;
            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                HRESULT hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[WorkingEnvironment] TaskScheduler CoCreateInstance failed: 0x{hr.Value:X}");
                    return;
                }
            }

            taskService.Get()->Connect(new(), new(), new(), new());

            // Get root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            HRESULT hrRoot = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hrRoot.Failed)
            {
                ReboundLogger.Log($"[WorkingEnvironment] Failed to get root folder: 0x{hrRoot.Value:X}");
                return;
            }

            // Try to get Rebound folder
            using ComPtr<ITaskFolder> reboundFolder = null;
            HRESULT hrRebound = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

            if (hrRebound.Failed || reboundFolder.Get() is null)
            {
                ReboundLogger.Log("[WorkingEnvironment] Rebound folder not found, creating it...");
                HRESULT hrCreate = rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
                if (hrCreate.Failed)
                {
                    ReboundLogger.Log($"[WorkingEnvironment] Failed to create Rebound folder. HRESULT: 0x{hrCreate.Value:X}");
                }
                else
                {
                    ReboundLogger.Log("[WorkingEnvironment] Successfully created Rebound tasks folder.");
                }
            }
            else
            {
                ReboundLogger.Log("[WorkingEnvironment] Rebound folder already exists, no action needed.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Task Scheduler error:", ex);
        }
    }

    public static unsafe void RemoveTasksFolder()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");

            // Create and connect ITaskService
            using ComPtr<ITaskService> taskService = default;
            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                HRESULT hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[WorkingEnvironment] TaskScheduler CoCreateInstance failed: 0x{hr.Value:X}");
                    return;
                }
            }

            taskService.Get()->Connect(new(), new(), new(), new());

            // Get root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            HRESULT hrRoot = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hrRoot.Failed)
            {
                ReboundLogger.Log($"[WorkingEnvironment] Failed to get root folder: 0x{hrRoot.Value:X}");
                return;
            }

            // Try to get Rebound folder
            using ComPtr<ITaskFolder> reboundFolder = null;
            HRESULT hrRebound = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());
            if (hrRebound.Failed || reboundFolder.Get() is null)
            {
                ReboundLogger.Log($"[WorkingEnvironment] Rebound folder not found, nothing to remove. HRESULT: 0x{hrRebound.Value:X}");
                return;
            }

            // Delete the folder
            HRESULT hrDelete = rootFolder.Get()->DeleteFolder(pszRebound, 0); // 0 = no flags
            if (hrDelete.Failed)
            {
                ReboundLogger.Log($"[WorkingEnvironment] Failed to delete Rebound folder. HRESULT: 0x{hrDelete.Value:X}");
            }
            else
            {
                ReboundLogger.Log("[WorkingEnvironment] Successfully removed Rebound tasks folder.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Task Scheduler error (Remove):", ex);
        }
    }

    public static unsafe bool TaskFolderExists()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");

            // Create and connect ITaskService
            using ComPtr<ITaskService> taskService = default;
            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                HRESULT hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());

                if (hr.Failed || taskService.Get() is null)
                {
                    ReboundLogger.Log($"[WorkingEnvironment] TaskScheduler CoCreateInstance failed: 0x{hr.Value:X}");
                    return false;
                }
            }

            taskService.Get()->Connect(new(), new(), new(), new());

            // Get root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            HRESULT hrRoot = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hrRoot.Failed)
            {
                ReboundLogger.Log($"[WorkingEnvironment] Failed to get root folder: 0x{hrRoot.Value:X}");
                return false;
            }

            // Try to get Rebound folder
            using ComPtr<ITaskFolder> reboundFolder = null;
            HRESULT hrRebound = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

            if (hrRebound.Succeeded && reboundFolder.Get() != null)
            {
                ReboundLogger.Log("[WorkingEnvironment] Rebound tasks folder exists.");
                return true;
            }
            else
            {
                ReboundLogger.Log($"[WorkingEnvironment] Rebound folder does not exist. HRESULT: 0x{hrRebound.Value:X}");
                return false;
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Task Scheduler error (Exists):", ex);
            return false;
        }
    }

    // Mandatory instructions
    public static async void EnsureMandatoryInstructionsIntegrity()
    {
        try
        {
            foreach (var instructions in Catalog.MandatoryMods)
            {
                ReboundLogger.Log("[WorkingEnvironment] Installing mandatory instruction: " + instructions.GetType().Name);
                await instructions.InstallAsync();
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to ensure mandatory instructions integrity:", ex);
        }
    }

    public static async void RemoveMandatoryInstructions()
    {
        try
        {
            foreach (var instructions in Catalog.MandatoryMods)
            {
                ReboundLogger.Log("[WorkingEnvironment] Uninstalling mandatory instruction: " + instructions.GetType().Name);
                await instructions.UninstallAsync();
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to remove mandatory instructions:", ex);
        }
    }

    public static bool MandatoryInstructionsExist()
    {
        try
        {
            foreach (var instructions in Catalog.MandatoryMods)
            {
                var integrity = instructions.GetIntegrity();
                ReboundLogger.Log($"[WorkingEnvironment] Checking integrity for {instructions.GetType().Name}: {integrity}");
                if (integrity != ModIntegrity.Installed)
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to check mandatory instructions integrity:", ex);
            return false;
        }
    }

    // General methods
    public static void EnableRebound()
    {
        ReboundLogger.Log("[WorkingEnvironment] Enabling Rebound...");
        EnsureFolderIntegrity();
        EnsureTasksFolderIntegrity();
        EnsureMandatoryInstructionsIntegrity();
        ReboundLogger.Log("[WorkingEnvironment] Rebound enabled.");
    }

    public static void DisableRebound()
    {
        ReboundLogger.Log("[WorkingEnvironment] Disabling Rebound...");
        RemoveFolder();
        RemoveTasksFolder();
        RemoveMandatoryInstructions();
        ReboundLogger.Log("[WorkingEnvironment] Rebound disabled.");
    }

    public static bool IsReboundEnabled()
    {
        ReboundLogger.Log(FolderExists()
            ? "[WorkingEnvironment] Rebound folder exists."
            : "[WorkingEnvironment] Rebound folder does not exist.");
        ReboundLogger.Log(TaskFolderExists()
            ? "[WorkingEnvironment] Rebound tasks folder exists."
            : "[WorkingEnvironment] Rebound tasks folder does not exist.");
        ReboundLogger.Log(MandatoryInstructionsExist()
            ? "[WorkingEnvironment] All mandatory instructions are installed."
            : "[WorkingEnvironment] Some mandatory instructions are missing.");

        return FolderExists() && TaskFolderExists() && MandatoryInstructionsExist();
    }
}