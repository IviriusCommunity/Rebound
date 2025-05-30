using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Helpers.Windowing;
using Rebound.Modding.Instructions;
using WinUIEx;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace ReboundHubInstaller;

public sealed partial class MainWindow : WindowEx
{
    private readonly string reboundPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rebound");
    private readonly string tempPath;

    public MainWindow()
    {
        InitializeComponent();
        tempPath = Path.Combine(reboundPath, "Temp");
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "ReboundHubInstaller.ico"));
        this.CenterOnScreen();
        ExtendsContentIntoTitleBar = true;
    }

    /*public ObservableCollection<ReboundAppInstructions> Instructions { get; } =
    [
        new WinverInstructions(),
        new OnScreenKeyboardInstructions(),
        new DiskCleanupInstructions(),
        new UserAccountControlSettingsInstructions(),
        new ControlPanelInstructions(),
        new ShellInstructions()
    ];*/

    private void WindowEx_Closed(object sender, WindowEventArgs args) => Process.GetCurrentProcess().Kill();

    [RelayCommand]
    public void CloseApp() => Close();

    [RelayCommand]
    public async Task RemoveReboundAsync()
    {
        InstallingProgressRing.IsIndeterminate = true;
        ShowProgress("Removing Rebound...");
        await Task.Delay(1000);

        /*foreach (var instruction in Instructions)
            await instruction.Uninstall();*/

        ReboundWorkingEnvironment.RemoveFolder();
        ReboundWorkingEnvironment.RemoveTasksFolder();

        // Remove ReboundHub folder from Program Files
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var reboundHubFolder = Path.Combine(programFilesPath, "ReboundHub");
        if (Directory.Exists(reboundHubFolder))
        {
            try
            {
                Directory.Delete(reboundHubFolder, true);
            }
            catch
            {

            }
        }

        // Remove start menu shortcut
        var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");
        var shortcutPath = Path.Combine(startMenuFolder, "Rebound Hub.lnk");
        if (File.Exists(shortcutPath))
        {
            try
            {
                File.Delete(shortcutPath);
            }
            catch
            {

            }
        }

        Close();
    }

    [RelayCommand]
    public async Task RepairReboundHubAsync()
    {
        InstallingProgressRing.IsIndeterminate = true;
        ShowProgress("Repairing Rebound Hub...");
        await Task.Delay(1000);

        try
        {
            PrepareDirectories();
            await InstallExecutableAsync();
        }
        catch
        {
            DescriptionBox.Text = "Couldn't repair Rebound Hub.";
            await Task.Delay(500);
        }

        Close();
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        InstallingProgressRing.IsIndeterminate = true;
        ShowProgress("Installing Rebound Hub...");
        await Task.Delay(1000);

        InstallingProgressRing.IsIndeterminate = false;
        PrepareDirectories();

        await InstallExecutableAsync();
        await InstallDotNetRuntimeAsync();

        await CleanupAsync();

        DescriptionBox.Text = "Done!";
        InstallingProgressRing.Value = 100;
        await Task.Delay(500);
        Close();
    }

    private void ShowProgress(string message)
    {
        ButtonsStackPanel.Visibility = Visibility.Collapsed;
        DescriptionBox.Visibility = Visibility.Visible;
        DescriptionBox.Text = message;
    }

    private void PrepareDirectories()
    {
        Directory.CreateDirectory(reboundPath);
        Directory.CreateDirectory(tempPath);
    }

    public static void EnsureFolderIntegrity()
    {
        try
        {
            var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");

            // Get the start menu folder of Rebound
            var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "ReboundHub");

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

    private async Task InstallExecutableAsync()
    {
        try
        {
            ShowProgress("Copying ReboundHub.exe to the Program Files folder and creating the start menu shortcut. This may take a while...");
            InstallingProgressRing.Value = 8;
            await Task.Delay(500);

            var exePath = Path.Combine(AppContext.BaseDirectory, "Rebound Hub.exe");
            var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub", "Rebound Hub.exe");

            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"));
            File.SetAttributes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"), FileAttributes.Directory);

            File.Copy(exePath, targetPath, true);

            // Ensure Rebound is properly installed
            EnsureFolderIntegrity();

            // Get the start menu folder of Rebound
            var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");

            var shortcutPath = Path.Combine(startMenuFolder, $"Rebound Hub.lnk");

            // Create ShellLink object
            var clsidShellLink = new Guid("00021401-0000-0000-C000-000000000046"); // CLSID_ShellLink
            var iidShellLink = new Guid("000214F9-0000-0000-C000-000000000046");  // IID_IShellLinkW

            Windows.Win32.PInvoke.CoCreateInstance(in clsidShellLink, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER, in iidShellLink, out var shellLinkObj);
            var targetPathPtr = Marshal.StringToHGlobalUni(targetPath);
            var targetDirPathPtr = Marshal.StringToHGlobalUni(Path.GetDirectoryName(targetPath));
            var exePathPtr = Marshal.StringToHGlobalUni(exePath);
            var shortcutPathPtr = Marshal.StringToHGlobalUni(shortcutPath);

            var shellLink = (Windows.Win32.UI.Shell.IShellLinkW)shellLinkObj;
            unsafe { shellLink.SetPath(new Windows.Win32.Foundation.PCWSTR((char*)targetPathPtr)); }
            unsafe { shellLink.SetWorkingDirectory(new Windows.Win32.Foundation.PCWSTR((char*)targetDirPathPtr)); }

            // Save it to file using IPersistFile
            var persistFile = (Windows.Win32.System.Com.IPersistFile)shellLink;
            unsafe { persistFile.Save(new Windows.Win32.Foundation.PCWSTR((char*)shortcutPathPtr), true); }
        }
        catch (Exception ex)
        {
            DescriptionBox.Text = $"Couldn't install Rebound Hub. {ex.Message}";
            await Task.Delay(5000);
        }
    }

    private async Task InstallDotNetRuntimeAsync()
    {
        try
        {
            ShowProgress("Installing .NET 9.0 Runtime. This may take a while...");
            InstallingProgressRing.Value = 42;
            await Task.Delay(50);

            var runtimeSource = Path.Combine(AppContext.BaseDirectory, "dotNET9Runtime.exe");
            var runtimeTemp = Path.Combine(tempPath, "dotNET9Runtime.exe");
            File.Copy(runtimeSource, runtimeTemp, true);

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

            File.Delete(runtimeTemp);
        }
        catch
        {
            DescriptionBox.Text = "Couldn't install .NET 9.0 Runtime.";
            await Task.Delay(50);
        }
    }

    private async Task CleanupAsync()
    {
        ShowProgress("Cleaning up...");
        InstallingProgressRing.Value = 94;
        await Task.Delay(1000);

        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
    }
}