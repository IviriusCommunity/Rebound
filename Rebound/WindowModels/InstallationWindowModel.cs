using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

namespace Rebound.WindowModels;

public static class InstallationWindowModel
{
    public static void RestartPC()
    {
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = "shutdown /r /t 0",
            Verb = "runas"
        };
        _ = Process.Start(info);
    }
}

public static class SystemLock
{
    [DllImport("user32.dll")]
    private static extern void LockWorkStation();

    public static void Lock()
    {
        LockWorkStation();
    }
}

public static class TaskManager
{
    public static void StopTask(string task)
    {
        try
        {
            // Find the explorer.exe process
            var processes = Process.GetProcessesByName(task);
            foreach (var process in processes)
            {
                // Kill the process
                process.Kill();
                process.WaitForExit(); // Ensure the process has exited
            }
        }
        catch
        {

        }
    }

    public static void StartTask(string task)
    {
        try
        {
            // Start a new explorer.exe process
            Process.Start(task);
        }
        catch
        {

        }
    }
}
