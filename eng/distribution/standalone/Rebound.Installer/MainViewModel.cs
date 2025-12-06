using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Forge;
using Windows.Win32;

namespace Rebound.Installer;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial double Progress { get; set; } = 0;

    [ObservableProperty]
    public partial string Status { get; set; } = "Processing...";

    [ObservableProperty]
    public partial string Title { get; set; } = "Initializing...";

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsError { get; set; } = false;

    [ObservableProperty]
    public partial double Steps { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = false;

    [ObservableProperty]
    public partial bool IsOldReboundInstalled { get; set; } = false;

    public readonly string oldReboundHubFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "ReboundHub");

    public MainViewModel()
    {
        if (Directory.Exists(oldReboundHubFolder))
        {
            IsOldReboundInstalled = false;
        }
    }

    public async Task InstallAsync(bool repair)
    {
        /*// Kill processes
        await KillAllProcesses();

        // Init
        IsIndeterminate = true;
        Title = repair ? "Repairing..." : "Installing...";

        // Calculating steps
        Steps =
            20 + // Initial
            await GetItemCountForFolderAsync(_dataPath) + // All items
            1; // Shortcut

        if (!repair)
        {
            Steps +=
            100 + // .NET runtiume
            100; // WARuntime
        }

        // Initial
        IsIndeterminate = false;
        Progress += 20;

        await Task.Delay(100);

        if (!repair)
        {
            // .NET runtime
            Status = "Installing .NET runtime...";
            await InstallDotNetRuntimeAsync();
            Progress += 100;

            // WARuntime
            Status = "Installing Windows App Runtime...";
            await InstallWARuntimeAsync();
            Progress += 100;
        }

        // Copying files
        Status = "Copying files...";
        try
        {
            foreach (var folder in Directory.EnumerateDirectories(_dataPath)) // Rebound
            {
                var relativePath = Path.GetRelativePath(_dataPath, folder);
                if (relativePath is not "rhub")
                {
                    var targetPath = Path.Combine(_reboundInstallationPath, relativePath);
                    if (!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                        File.SetAttributes(targetPath, FileAttributes.Directory);
                    }
                    await CopyFolderAsync(folder, targetPath);
                }
            }
            var hubFolder = Directory.GetDirectories(_dataPath, "rhub", SearchOption.TopDirectoryOnly).FirstOrDefault();
            var hubTargetPath = Path.Combine(_reboundInstallationPath, _reboundHubInstallationPath);
            if (!Directory.Exists(hubTargetPath))
            {
                Directory.CreateDirectory(hubTargetPath);
                File.SetAttributes(hubTargetPath, FileAttributes.Directory);
            }
            await CopyFolderAsync(hubFolder, hubTargetPath);
        }
        catch
        {

        }

        // Creating start menu shortcut
        Status = "Creating start menu shortcut...";
        try
        {
            await InstallShortcutAsync();
        }
        catch
        {

        }

        foreach (var instruction in ReboundTotalInstructions.MandatoryInstructions)
        {
            Process.Start(instruction.EntryExecutable);
        }*/
    }

    public async Task RemoveAsync()
    {
        /*// Kill processes
        await DeleteReboundModAsync();

        // Init
        Title = "Uninstalling...";

        // Initial
        Progress = 0;
        IsIndeterminate = true;
        Status = "Deleting Rebound Hub...";

        try
        {
            // Delete shortcut
            var shortcutPath = Path.Combine(_startMenuPath, $"Rebound Hub.lnk");
            await Task.Run(() => File.Delete(shortcutPath));

            // Delete Rebound Hub
            await Task.Run(() => Directory.Delete(_reboundHubInstallationPath, true));
        }
        catch (Exception ex)
        {
            Status = $"Couldn't delete Rebound Hub. {ex.Message}";
            IsError = true;
            ErrorMessage = ex.Message;
            return;
        }

        Process.GetCurrentProcess().Kill();*/
    }

    private async Task InstallShortcutAsync()
    {
        /*try
        {
            unsafe
            {
                var targetPath = Path.Combine(_reboundHubInstallationPath, "Rebound Hub.exe");

                var shortcutPath = Path.Combine(_startMenuPath, $"Rebound Hub.lnk");

                //ReboundWorkingEnvironment.EnsureFolderIntegrity();

                var startMenuFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    "Programs", "Rebound");

                var clsidShellLink = CLSID.CLSID_ShellLink;
                var iidIShellLinkW = Windows.Win32.UI.Shell.IShellLinkW.IID_Guid;
                var iidIPersistFile = IID.IID_IPersistFile;

                Windows.Win32.UI.Shell.IShellLinkW* pShellLink;

                Windows.Win32.Foundation.HRESULT hr = PInvoke.CoCreateInstance(
                    clsidShellLink,
                    null,
                    Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                    &iidIShellLinkW,
                    (void**)&pShellLink);

                if (hr.Failed || pShellLink is null)
                    return;

                fixed (char* exePath = targetPath)
                {
                    pShellLink->SetPath(new Windows.Win32.Foundation.PCWSTR(exePath));
                }

                string workingDir = Path.GetDirectoryName(targetPath)!;
                fixed (char* workingDirPtr = workingDir)
                {
                    pShellLink->SetWorkingDirectory(new Windows.Win32.Foundation.PCWSTR(workingDirPtr));
                }

                Windows.Win32.System.Com.IPersistFile* pPersistFile;
                hr = ((Windows.Win32.System.Com.IUnknown*)pShellLink)->QueryInterface(iidIPersistFile, (void**)&pPersistFile);

                if (hr.Succeeded && pPersistFile is not null)
                {
                    fixed (char* shortcutPathPtr = shortcutPath)
                    {
                        pPersistFile->Save(new Windows.Win32.Foundation.PCWSTR(shortcutPathPtr), true);
                    }

                    pPersistFile->Release();
                }

                pShellLink->Release();
            }
        }
        catch (Exception ex)
        {
            Status = $"Couldn't install the Rebound Hub shortcut. {ex.Message}";
            await Task.Delay(2000);
        }*/
    }

    private async Task InstallDotNetRuntimeAsync()
    {
        /*try
        {
            var runtimeTemp = Path.Combine(_dataPath, "dotNET9Runtime.exe");

            var psi = new ProcessStartInfo
            {
                FileName = runtimeTemp,
                Arguments = "/quiet /norestart",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process != null)
                await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsError = true;
            Status = "Couldn't install .NET 9.0 Runtime.";
            await Task.Delay(5000);
        }*/
    }

    private async Task InstallWARuntimeAsync()
    {
        /*try
        {
            var runtimeTemp = Path.Combine(_dataPath, "WindowsAppRuntime.exe");

            var psi = new ProcessStartInfo
            {
                FileName = runtimeTemp,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process != null)
                await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsError = true;
            Status = "Couldn't install Windows App Runtime.";
            await Task.Delay(5000);
        }*/
    }

    public async Task ExtractToPathAsync(string zipFilePath, string targetPath)
    {
        /*if (string.IsNullOrEmpty(zipFilePath) || string.IsNullOrEmpty(targetPath) || !File.Exists(zipFilePath))
        {
            Status = "Invalid zip file path or target path.";
            IsError = true;
            ErrorMessage = "Invalid zip file path or target path.";
            return;
        }

        try
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, targetPath, true));
        }
        catch (Exception ex)
        {
            Status = "Extraction failed.";
            IsError = true;
            ErrorMessage = ex.Message;
        }*/
    }

    public static async Task<double> GetItemCountForFolderAsync(string folderPath)
    {
        /*if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return 0;

        try
        {
            return await Task.Run(() => Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).ToList().Count);
        }
        catch
        {
            return 0;
        }*/
        return 0;
    }

    public async Task CopyFolderAsync(string path, string targetPath)
    {
        /*if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(targetPath) || !Directory.Exists(path))
        {
            return;
        }

        var allFiles = await Task.Run(() => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToList());

        foreach (var filePath in allFiles)
        {
            var relativePath = Path.GetRelativePath(path, filePath);
            var destFile = Path.Combine(targetPath, relativePath);
            var destDir = Path.GetDirectoryName(destFile);

            if (!Directory.Exists(destDir) && destDir != null)
            {
                Directory.CreateDirectory(destDir);
                File.SetAttributes(destDir, FileAttributes.Directory);
            }

            await Task.Run(() => File.Copy(filePath, destFile, true));
            await Task.Delay(2);

            Progress++;
            Status = $"Copying {filePath} to {destFile}...";
        }*/
    }

    public async Task DeleteReboundModAsync()
    {
        /*foreach (var instruction in ReboundTotalInstructions.AppInstructions)
        {
            await instruction.UninstallAsync();
        }
        foreach (var instruction in ReboundTotalInstructions.MandatoryInstructions)
        {
            await instruction.UninstallAsync();
        }*/
    }

    public async Task KillAllProcesses()
    {
        /*foreach (var instruction in ReboundTotalInstructions.AppInstructions)
        {
            foreach (var process in Process.GetProcessesByName(instruction.ProcessName))
            {
                process.Kill();
            }
        }
        foreach (var instruction in ReboundTotalInstructions.MandatoryInstructions)
        {
            foreach (var process in Process.GetProcessesByName(instruction.ProcessName))
            {
                process.Kill();
            }
        }*/
    }
}