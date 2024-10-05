using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ColorCode.Compilation.Languages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using static Rebound.InstallationWindow;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class UninstallationWindow : WindowEx
{
    public UninstallationWindow()
    {
        this.InitializeComponent();
        this.MoveAndResize(0, 0, 0, 0);
        Load();
    }

    double currentStep = 0;
    double totalSteps = 0;
    double currentSubstep = 0;
    double totalSubsteps = 0;

    public async void Load()
    {
        this.SetIsAlwaysOnTop(true);
        this.SetWindowPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        Title.Text = "Uninstalling Rebound 11";
        Subtitle.Text = "Starting...";

        await Task.Delay(1000);

        timer.Interval = new TimeSpan(3);
        timer.Tick += Timer_Tick;
        timer.Start();

        totalSteps = 11;
        totalSubsteps = 21;

        ReboundProgress.Maximum = totalSubsteps;

        currentStep++;
        currentSubstep++;

        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        Subtitle.Text = $"Deleting C:\\Rebound11...";

        if (Directory.Exists("C:\\Rebound11") == true)
        {
            try
            {
                // Define the directory path
                string directoryPath = @"C:\Rebound11";

                // Define the PowerShell command to delete the directory
                string powershellCommand = $@"
        if (Test-Path '{directoryPath}') {{
            Remove-Item -Path '{directoryPath}' -Recurse -Force
            Write-Output 'The directory `{directoryPath}` and all its contents have been deleted.'
        }} else {{
            Write-Output 'The directory `{directoryPath}` does not exist.'
        }}";

                // Create a process to run PowerShell
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-NoProfile -Command \"{powershellCommand}\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    // Start the process
                    process.Start();

                    // Read the output and error
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();
                }
            }
            catch
            {
            
            }
        }

        await ReplaceShortcut(
            "Control Panel",
            $@"{AppContext.BaseDirectory}\Rebound11Files\shcwin11\Control Panel.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Control Panel.lnk",
            "Control Panel");

        await DeleteShortcut(
            "Rebound 11 Quick Full Computer Cleanup",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools\Rebound 11 Quick Full Computer Cleanup.lnk",
            "Rebound 11 Quick Full Computer Cleanup");

        await DeleteShortcut(
            "Change User Account Control settings",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Administrative Tools\Change User Account Control settings.lnk",
            "Change User Account Control settings");

        await UninstallAppPackage(
            "8ab98b2f-6dbe-4358-a752-979d011f968d_0.1.0.0_x64__yejd587sfa94t",
            "Rebound Run",
            $@"{AppContext.BaseDirectory}\Rebound11Files\shcwin11\Run.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Run.lnk",
            "Run");

        await DeleteShortcut(
            "Rebound Run",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Startup\Rebound.RunStartup.lnk",
            "Rebound Run");

        await UninstallAppPackage(
            "54d2a63e-e616-4159-bed6-c776b8a816e1_0.1.0.0_x64__yejd587sfa94t",
            "Rebound Defragment And Optimize Drives",
            $@"{AppContext.BaseDirectory}\Rebound11Files\shcwin11\dfrgui.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\dfrgui.lnk",
            "Defragment And Optimize Drives");

        await UninstallAppPackage(
            "039b9731-7b33-49de-bb09-5b81d5978d1c_0.0.3.0_x64__yejd587sfa94t",
            "Rebound Winver",
            $@"",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\winver.lnk",
            "winver");

        await ReplaceShortcut(
            "On-Screen Keyboard",
            $@"{AppContext.BaseDirectory}\Rebound11Files\shcwin11\On-Screen Keyboard.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Accessibility\On-Screen Keyboard.lnk",
            "On-Screen Keyboard");

        await UninstallAppPackage(
            "0b347e39-1da3-4fc7-80c2-dbf3603118f3_1.0.4.0_x64__yejd587sfa94t",
            "Rebound TPM Management",
            $@"",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\tpm.msc.lnk",
            "tpm.msc");

        await UninstallAppPackage(
            "e8dfd11c-954d-46a2-b700-9cbc6201f056_1.0.2.0_x64__yejd587sfa94t",
            "Rebound Disk Cleanup",
            $@"{AppContext.BaseDirectory}\Rebound11Files\shcwin11\Disk Cleanup.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Disk Cleanup.lnk",
            "Disk Cleanup");

        currentSubstep = totalSubsteps;
        Title.Text = $"Uninstalling Rebound 11: 100%";
        Subtitle.Text = $"Closing Rebound Hub...";
        ReboundProgress.Minimum = 0;
        ReboundProgress.Maximum = 1;
        ReboundProgress.Value = totalSubsteps;

        await Task.Delay(1000);

        Ring.Visibility = Visibility.Collapsed;
        Title.Text = "Would you like to restart your computer now?";
        Subtitle.Visibility = Visibility.Collapsed;
        ReboundProgress.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Visible;
        ProgressBars.Visibility = Visibility.Collapsed;
        ReboundProgress.Value = totalSubsteps + 10;
    }

    public async Task<Task> UninstallAppPackage(string packageFamilyName, string displayAppName, string lnkFile, string lnkDestination, string lnkDisplayName)
    {
        // 3 SUBSTEPS

        currentStep += 1;

        string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        // Substep 1: cache package

        currentSubstep += 1;
        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = currentSubstep;

        // Setup the process start info
        var procFolder = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Verb = "runas",                 // Run as administrator
            UseShellExecute = false,
            CreateNoWindow = true,// Required to redirect output
            Arguments = $"Remove-AppxPackage -Package \"{packageFamilyName}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Uninstalling {displayAppName}...";

        // Start the process
        var resFolder = Process.Start(procFolder);

        // Read output and errors
        string output = await resFolder.StandardOutput.ReadToEndAsync();
        string error = await resFolder.StandardError.ReadToEndAsync();

        // Wait for the process to exit
        await resFolder.WaitForExitAsync();

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: {(string.IsNullOrEmpty(error) ? $"{displayAppName} uninstalled." : $"{displayAppName} uninstallation failed: the package does not exist..")}";

        await Task.Delay(50);

        // Substep 2: delete winver.lnk
        
        currentSubstep += 1;
        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Deleting {lnkDisplayName}.lnk...";
        try
        {
            File.Delete(lnkDestination);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file does not exist. Skipping...";
        }

        await Task.Delay(50);
        
        // Substep 3: copy new winver.lnk

        currentSubstep += 1;
        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying new {lnkDisplayName}.lnk...";
        try
        {
            File.Copy(lnkFile, lnkDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
        }
        await Task.Delay(50);
        return Task.CompletedTask;
    }

    public async Task<Task> DeleteShortcut(string displayAppName, string lnkDestination, string lnkDisplayName)
    {
        // 1 SUBSTEP

        currentStep += 1;

        // Substep 5: delete winver.lnk

        currentSubstep += 1;
        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Deleting {lnkDisplayName}.lnk...";
        try
        {
            File.Delete(lnkDestination);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file does not exist. Skipping...";
        }

        await Task.Delay(50);
        return Task.CompletedTask;
    }

    public async Task<Task> ReplaceShortcut(string displayAppName, string lnkFile, string lnkDestination, string lnkDisplayName)
    {
        // 1 SUBSTEP

        currentStep += 1;

        // Substep 5: delete winver.lnk

        currentSubstep += 1;
        Title.Text = $"Uninstalling Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Deleting {lnkDisplayName}.lnk...";
        try
        {
            File.Copy(lnkFile, lnkDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file does not exist. Skipping...";
        }

        await Task.Delay(50);
        return Task.CompletedTask;
    }

    private void Timer_Tick(object sender, object e)
    {
        ExplorerManager.StopExplorer();
    }

    DispatcherTimer timer = new DispatcherTimer();

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        Title.Text = "Restarting...";
        Subtitle.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Collapsed;

        await Task.Delay(3000);

        timer.Stop();
        ExplorerManager.StartExplorer();
        RestartPC();
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        timer.Stop();
        ExplorerManager.StartExplorer();
        Ring.Visibility = Visibility.Visible;
        Title.Text = "Restarting Explorer...";
        Subtitle.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Collapsed;

        await Task.Delay(3000);

        SystemLock.Lock();
        Close();
    }
}
