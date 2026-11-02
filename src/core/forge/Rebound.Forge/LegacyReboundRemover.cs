// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;

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

    public static bool OldReboundExists()
        => Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"));

    public static void DeleteOldRebound()
    {
        KillProcesses();

        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound"));
        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"));
        TryDeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Rebound"));
        TryDeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Rebound Hub.lnk"));
        DeleteImageFileExecutionOptions();
    }

    private static void DeleteImageFileExecutionOptions()
    {
        string[] executables =
        [
            "charmap.exe",
            "control.exe",
            "useraccountcontrolsettings.exe",
            "winver.exe"
        ];

        const string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath, writable: true);

        if (key is null)
            return;

        foreach (var executable in executables)
        {
            try
            {
                key.DeleteSubKeyTree(executable, throwOnMissingSubKey: false);
            }
            catch { }
        }
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
            catch { }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {

        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {

        }
    }
}