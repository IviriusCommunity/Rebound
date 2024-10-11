using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.WindowModels;
using WinUIEx;

namespace Rebound;

public sealed partial class UninstallationWindow : WindowEx
{
    public const int SUBSTEPS_APP_PACKAGE_NO_LINK = 2;
    public const int SUBSTEPS_REPLACE_SHORTCUT = 1;
    public const int SUBSTEPS_DELETE_SHORTCUT = 1;
    public const int SUBSTEPS_APP_PACKAGE = 2;
    public const int SUBSTEPS_FOLDER = 1;
    public const int SUBSTEPS_REG = 1;

    public UninstallationWindow(bool deleteAll)
    {
        this?.InitializeComponent();
        this.MoveAndResize(0, 0, 0, 0);
        Load(deleteAll);
    }

    public double CurrentStep = 0;
    public double TotalSteps = 0;
    public double CurrentSubstep = 0;
    public double TotalSubsteps = 0;

    public async void Load(bool deleteAll)
    {
        this.SetIsAlwaysOnTop(true);
        this.SetWindowPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        Title.Text = "Uninstalling Rebound 11";
        Subtitle.Text = "Starting...";

        await Task.Delay(1000);

        Timer.Interval = new TimeSpan(3);
        Timer.Tick += Timer_Tick;
        Timer.Start();

        TotalSteps = 11;
        TotalSubsteps = 
            SUBSTEPS_FOLDER +                  // Rebound 11 Folder
            SUBSTEPS_REPLACE_SHORTCUT +        // Control Panel
            SUBSTEPS_FOLDER +                  // Rebound 11 Tools
            SUBSTEPS_DELETE_SHORTCUT +         // UAC Settings
            SUBSTEPS_APP_PACKAGE +             // Run
            SUBSTEPS_DELETE_SHORTCUT +         // Rebound Run Startup
            SUBSTEPS_APP_PACKAGE +             // Defrag
            SUBSTEPS_APP_PACKAGE +             // Winver
            SUBSTEPS_REPLACE_SHORTCUT +        // OSK
            SUBSTEPS_APP_PACKAGE +             // TPM
            SUBSTEPS_APP_PACKAGE;              // Disk Cleanup
        if (deleteAll == true)
        {
            TotalSteps += 2;
            TotalSubsteps +=
                SUBSTEPS_APP_PACKAGE_NO_LINK + // Files App
                SUBSTEPS_REG;                  // Files App Registry Modification
        }

        ReboundProgress.Maximum = TotalSubsteps;

        await RemoveFolder(@"C:\Rebound11");

        await ReplaceShortcut(
            $@"{AppContext.BaseDirectory}\Shortcuts\Included\Control Panel.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Control Panel.lnk",
            "Control Panel");

        await RemoveFolder($@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools");

        await DeleteShortcut(
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Administrative Tools\Change User Account Control settings.lnk",
            "Change User Account Control settings");

        await UninstallAppPackage(
            InstallationWindowModel.RUN,
            "Rebound Run",
            $@"{AppContext.BaseDirectory}\Shortcuts\Included\Run.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Run.lnk",
            "Run");

        await DeleteShortcut(
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Startup\ReboundRunStartup.lnk",
            "Rebound Run Startup");

        await UninstallAppPackage(
            InstallationWindowModel.DEFRAG,
            "Rebound Defragment And Optimize Drives",
            $@"{AppContext.BaseDirectory}\Shortcuts\Included\dfrgui.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\dfrgui.lnk",
            "Defragment And Optimize Drives");

        await UninstallAppPackage(
            InstallationWindowModel.WINVER,
            "Rebound Winver",
            $@"",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\winver.lnk",
            "winver");

        await ReplaceShortcut(
            $@"{AppContext.BaseDirectory}\Shortcuts\Included\On-Screen Keyboard.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Accessibility\On-Screen Keyboard.lnk",
            "On-Screen Keyboard");

        await UninstallAppPackage(
            InstallationWindowModel.TPM,
            "Rebound TPM Management",
            $@"",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\tpm.msc.lnk",
            "tpm.msc");

        await UninstallAppPackage(
            InstallationWindowModel.DISK_CLEANUP,
            "Rebound Disk Cleanup",
            $@"{AppContext.BaseDirectory}\Shortcuts\Included\Disk Cleanup.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Disk Cleanup.lnk",
            "Disk Cleanup");

        if (deleteAll == true)
        {
            await UninstallAppPackageWithoutLink(
                FILES_APP,
                "Files App",
                "Files.exe");
            await ApplyRegFile(
                $@"{AppContext.BaseDirectory}\AppRT\Registry\UnsetFilesAsDefault.reg",
                "Unset Files as Default");

        }

        CurrentSubstep = TotalSubsteps;
        Title.Text = $"Uninstalling Rebound 11: 100%";
        Subtitle.Text = $"Closing Rebound Hub...";
        ReboundProgress.Minimum = 0;
        ReboundProgress.Maximum = 1;
        ReboundProgress.Value = TotalSubsteps;

        await Task.Delay(1000);

        Ring.Visibility = Visibility.Collapsed;
        Title.Text = "Would you like to restart your computer now?";
        Subtitle.Visibility = Visibility.Collapsed;
        ReboundProgress.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Visible;
        ProgressBars.Visibility = Visibility.Collapsed;
        ReboundProgress.Value = TotalSubsteps + 10;
    }

    public void UpdateProgress(string title)
    {
        CurrentSubstep++;
        Title.Text = title;
        ReboundProgress.Value = CurrentSubstep;
    }

    public async Task<Task> RemoveFolder(string directoryPath)
    {
        CurrentStep++;

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        if (Directory.Exists(directoryPath) == true)
        {
            try
            {
                // Define the PowerShell command to delete the directory
                var powershellCommand = $@"
        if (Test-Path '{directoryPath}') {{
            Remove-Item -Path '{directoryPath}' -Recurse -Force
            Write-Output 'The directory `{directoryPath}` and all its contents have been deleted.'
        }} else {{
            Write-Output 'The directory `{directoryPath}` does not exist.'
        }}";

                // Create a process to run PowerShell
                using var process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -Command \"{powershellCommand}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Read the output and error
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();
            }
            catch
            {

            }
        }

        Subtitle.Text = $@"Deleting C:\Rebound11...";

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    public async Task<Task> UninstallAppPackage(string packageFamilyName, string displayAppName, string lnkFile, string lnkDestination, string lnkDisplayName)
    {
        CurrentStep++;

        // Substep 1: delete package

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            // Prepare the PowerShell command to remove the package by family name
            var command = $@"
                $package = Get-AppxPackage | Where-Object {{ $_.PackageFamilyName -eq '{packageFamilyName}' }};
                if ($package) {{
                    Remove-AppxPackage -Package $package.PackageFullName;
                    Write-Host 'Package removed: ' + $package.PackageFullName;
                }} else {{
                    Write-Host 'No package found with the given PackageFamilyName.';
                }}
            ";

            // Execute the command using PowerShell via Process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Start the process
            process.Start();

            var error = process.StandardError.ReadToEnd();

            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: {(string.IsNullOrEmpty(error) ? $"{displayAppName} uninstalled." : $"{displayAppName} uninstallation failed: the package does not exist.")}";

            process.WaitForExit();
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
        }

        await Task.Delay(50);

        // Substep 2: copy new LNK file

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying new {lnkDisplayName}.lnk...";
        try
        {
            File.Copy(lnkFile, lnkDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: The file already exists. Skipping...";
        }

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    public async Task<Task> UninstallAppPackageWithoutLink(string packageFamilyName, string displayAppName, string taskName)
    {
        CurrentStep++;

        // Substep 1: end task

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Ending task {taskName}...";
        try
        {
            TaskManager.StopTask(taskName);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
        }

        await Task.Delay(50);

        // Substep 2: delete package

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            // Prepare the PowerShell command to remove the package by family name
            var command = $@"
                $package = Get-AppxPackage | Where-Object {{ $_.PackageFamilyName -eq '{packageFamilyName}' }};
                if ($package) {{
                    Remove-AppxPackage -Package $package.PackageFullName;
                    Write-Host 'Package removed: ' + $package.PackageFullName;
                }} else {{
                    Write-Host 'No package found with the given PackageFamilyName.';
                }}
            ";

            // Execute the command using PowerShell via Process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Start the process
            process.Start();

            var error = process.StandardError.ReadToEnd();

            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: {(string.IsNullOrEmpty(error) ? $"{displayAppName} uninstalled." : $"{displayAppName} uninstallation failed: the package does not exist.")}";

            process.WaitForExit();
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
        }

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    public async Task<Task> DeleteShortcut(string lnkDestination, string lnkDisplayName)
    {
        CurrentStep++;

        // Substep 1: delete LNK file

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Deleting {lnkDisplayName}.lnk...";
        try
        {
            File.Delete(lnkDestination);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: The file does not exist. Skipping...";
        }

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    public async Task<Task> ReplaceShortcut(string lnkFile, string lnkDestination, string lnkDisplayName)
    {
        CurrentStep++;

        // Substep 1: replace LNK file

        UpdateProgress($"Uninstalling Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Replacing {lnkDisplayName}.lnk...";
        try
        {
            File.Copy(lnkFile, lnkDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
        }

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    public async Task<Task> ApplyRegFile(string regFilePath, string regDisplayName)
    {
        CurrentStep++;

        // Substep 1: end task

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $@"Step {CurrentStep} of {TotalSteps}: Installing registry key ""{regDisplayName}.reg""...";

        // Ensure the .reg file exists
        if (File.Exists(regFilePath))
        {
            // Prepare the process to run regedit silently
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    Arguments = $"/s \"{regFilePath}\"", // /s makes it silent (no prompts)
                    RedirectStandardOutput = false, // No output needed
                    RedirectStandardError = false,  // No error redirect needed
                    UseShellExecute = true,         // Use the shell to execute the command
                    CreateNoWindow = true           // No window shown
                }
            };

            // Start the process
            try
            {
                process.Start();
                process.WaitForExit();  // Optionally wait for the process to finish
            }
            catch
            {
                Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
            }
        }
        else
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: File does not exist. Skipping...";
        }

        await Task.Delay(50);

        return Task.CompletedTask;
    }

    private void Timer_Tick(object sender, object e)
    {
        TaskManager.StopTask("explorer.exe");
    }

    private readonly DispatcherTimer Timer = new();

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        Title.Text = "Restarting...";
        Subtitle.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Collapsed;

        await Task.Delay(3000);

        Timer.Stop();
        TaskManager.StartTask("explorer.exe");
        InstallationWindowModel.RestartPC();
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        Timer.Stop();
        TaskManager.StartTask("explorer.exe");
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
