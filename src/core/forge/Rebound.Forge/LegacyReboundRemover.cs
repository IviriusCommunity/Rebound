// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Rebound.Forge;

public static class LegacyReboundRemover
{
    private static readonly string[] ProcessesToKill = new[]
    {
        "Rebound.Shell",
        "Rebound.ServiceHost",
        "Rebound.Hub",
        "Rebound.DiskCleanup",
        "Rebound.UserAccountControlSettings",
        "Rebound.About"
    };

    public static void DeleteOldRebound()
    {
        KillProcesses();

        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound"));
        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"));
        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Rebound"));
        TryDeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Rebound Hub.lnk"));
    }

    private static void KillProcesses()
    {
        foreach (var processName in ProcessesToKill)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var proc in processes)
                {
                    proc.Kill(true); // true = kill entire process tree
                    proc.WaitForExit(5000); // wait up to 5 seconds for exit
                }
            }
            catch (Exception ex)
            {
                // Optional: log or handle failures to kill processes
                Debug.WriteLine($"Failed to kill process {processName}: {ex.Message}");
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            // Optional: log or handle delete failure
            Debug.WriteLine($"Failed to delete directory {path}: {ex.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            // Optional: log or handle delete failure
            Debug.WriteLine($"Failed to delete file {path}: {ex.Message}");
        }
    }
}
