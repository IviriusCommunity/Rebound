using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.WindowModels;
using WinUIEx;
using FileAttributes = System.IO.FileAttributes;

namespace Rebound;

public sealed partial class InstallationWindow : WindowEx
{
    public const int SUBSTEPS_EXEWITHSHORTCUT = 2;
    public const int SUBSTEPS_MSIXPACKAGE = 5;
    public const int SUBSTEPS_APPXPACKAGE = 2;
    public const int SUBSTEPS_FOLDER = 5;

    public InstallationWindow(bool Files, bool Run, bool Defrag, bool Winver, bool UAC, bool OSK, bool TPM, bool DiskCleanup)
    {
        this?.InitializeComponent();
        this.MoveAndResize(0, 0, 0, 0);

        Load(Files, Run, Defrag, Winver, UAC, OSK, TPM, DiskCleanup);
    }

    private readonly DispatcherTimer Timer = new();
    private double CurrentStep = 0;
    private double TotalSteps = 0;
    private double CurrentSubstep = 0;
    private double TotalSubsteps = 0;

    public async void Load(bool Files, bool Run, bool Defrag, bool Winver, bool UAC, bool OSK, bool TPM, bool DiskCleanup)
    {
        Timer.Interval = new TimeSpan(3);
        Timer.Tick += Timer_Tick;
        Timer.Start();

        // Rebound11 Folder
        TotalSteps++;
        TotalSubsteps += SUBSTEPS_FOLDER;

        // Control Panel
        TotalSteps++;
        TotalSubsteps += SUBSTEPS_EXEWITHSHORTCUT;

        // Files
        if (Files == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_APPXPACKAGE;
        }

        // Run
        if (Run == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_MSIXPACKAGE;
            TotalSubsteps += SUBSTEPS_EXEWITHSHORTCUT;
        }

        // Defrag
        if (Defrag == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_MSIXPACKAGE;
        }

        // Winver
        if (Winver == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_MSIXPACKAGE;
        }

        // UAC
        if (UAC == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_EXEWITHSHORTCUT;
        }

        // OSK
        if (OSK == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_EXEWITHSHORTCUT;
        }

        // TPM
        if (TPM == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_MSIXPACKAGE;
        }

        // DiskCleanup
        if (DiskCleanup == true)
        {
            TotalSteps++;
            TotalSubsteps += SUBSTEPS_MSIXPACKAGE;
        }

        // Initialization

        await Task.Delay(50);
        this.SetIsAlwaysOnTop(true);
        this.SetWindowPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        Title.Text = "Installing Rebound 11";
        Subtitle.Text = "Starting...";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = 0;
        ReboundProgress.Minimum = 6;
        ReboundProgress.Maximum = TotalSubsteps - 1;
        InstallProgress.Minimum = 0;
        InstallProgress.Maximum = 6;
        FinishProgress.Minimum = TotalSubsteps - 1;
        FinishProgress.Maximum = TotalSubsteps;
        InstallText.Opacity = 1;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 0.5;

        await Task.Delay(1000);

        #region Folder

        CurrentStep += 1;

        _ = await CreateRebound11Folder();

        #endregion Folder

        #region Control Panel

        _ = await InstallExeWithShortcut(
            $"{AppContext.BaseDirectory}\\Reserved\\rcontrol.exe",
            $"C:\\Rebound11\\rcontrol.exe",
            $"{AppContext.BaseDirectory}\\Shortcuts\\Control Panel.lnk",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Control Panel.lnk",
            "rcontrol",
            "Control Panel");

        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 1;
        FinishText.Opacity = 0.5;

        #endregion Control Panel

        #region Defragment

        if (Defrag == true)
        {
            _ = await InstallMsixPackage(
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Defrag.msix",
                "C:\\Rebound11\\Rebound.Defrag.msix",
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Defrag.cer",
                "Rebound Defragment and Optimize Drives",
                $"{AppContext.BaseDirectory}\\Reserved\\rdfrgui.exe",
                $"C:\\Rebound11\\rdfrgui.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\dfrgui.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\dfrgui.lnk",
                "rdfrgui",
                "dfrgui");
        }

        #endregion Defragment

        #region Winver

        if (Winver == true)
        {
            _ = await InstallMsixPackage(
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.About.msix",
                "C:\\Rebound11\\Rebound.About.msix",
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.About.cer",
                "Rebound Winver",
                $"{AppContext.BaseDirectory}\\Reserved\\rwinver.exe",
                $"C:\\Rebound11\\rwinver.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\winver.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\winver.lnk",
                "rwinver",
                "winver");
        }

        #endregion Winver

        #region Files

        if (Files == true)
        {
            _ = await InstallAppInstallerFile("Files App", "https://cdn.files.community/files/stable/Files.Package.appinstaller", "Files.exe");
            _ = await ApplyRegFile($@"{AppContext.BaseDirectory}\AppRT\Registry\SetFilesAsDefault.reg", "Set Files as Default");
        }

        #endregion Files

        #region UAC

        if (UAC == true)
        {
            _ = await InstallExeWithShortcut(
                $"{AppContext.BaseDirectory}\\Reserved\\ruacsettings.exe",
                $"C:\\Rebound11\\ruacsettings.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\Change User Account Control settings.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Change User Account Control settings.lnk",
                "ruacsettings",
                "Change User Account Control settings");
        }

        #endregion UAC

        #region Run

        if (Run == true)
        {
            _ = await InstallMsixPackage(
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Run.msix",
                "C:\\Rebound11\\Rebound.Run.msix",
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Run.cer",
                "Rebound Run",
                $"{AppContext.BaseDirectory}\\Reserved\\rrun.exe",
                $"C:\\Rebound11\\rrun.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\Run.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Run.lnk",
                "rrun",
                "Run");

            var startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            _ = await InstallExeWithShortcut(
                $"{AppContext.BaseDirectory}\\Reserved\\rrunSTARTUP.exe",
                $"C:\\Rebound11\\rrunSTARTUP.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\Rebound.RunStartup.lnk",
                $"{startupFolderPath}\\Rebound.RunStartup.lnk",
                "rrunSTARTUP",
                "ReboundRunStartup");
        }

        #endregion Run

        #region DiskCleanup

        if (DiskCleanup == true)
        {
            _ = await InstallMsixPackage(
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Cleanup.msix",
                "C:\\Rebound11\\Rebound.Cleanup.msix",
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.Cleanup.cer",
                "Rebound Disk Cleanup",
                $"{AppContext.BaseDirectory}\\Reserved\\rcleanmgr.exe",
                $"C:\\Rebound11\\rcleanmgr.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\Disk Cleanup.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Disk Cleanup.lnk",
                "rcleanmgr",
                "Disk Cleanup");
        }

        #endregion DiskCleanup

        #region TPM

        if (TPM == true)
        {
            _ = await InstallMsixPackage(
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.TrustedPlatform.msix",
                "C:\\Rebound11\\Rebound.TrustedPlatform.msix",
                $"{AppContext.BaseDirectory}\\Reserved\\Extended\\Rebound.TrustedPlatform.cer",
                "Rebound TPM Management",
                $"{AppContext.BaseDirectory}\\Reserved\\rtpm.exe",
                $"C:\\Rebound11\\rtpm.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\tpm.msc.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\tpm.msc.lnk",
                "rtpm",
                "tpm.msc");
        }

        #endregion TPM

        #region OSK

        if (OSK == true)
        {
            _ = await InstallExeWithShortcut(
                $"{AppContext.BaseDirectory}\\Reserved\\rosk.exe",
                $"C:\\Rebound11\\rosk.exe",
                $"{AppContext.BaseDirectory}\\Shortcuts\\On-Screen Keyboard.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Accessibility\On-Screen Keyboard.lnk",
                "rosk",
                "On-Screen Keyboard");
        }

        #endregion OSK

        CurrentSubstep = TotalSubsteps;
        Title.Text = $"Installing Rebound 11: 100%";
        Subtitle.Text = $"Closing Rebound Hub...";
        App.MainAppWindow.Close();
        ReboundProgress.Minimum = 0;
        ReboundProgress.Maximum = 1;
        InstallProgress.Minimum = 0;
        InstallProgress.Maximum = 1;
        FinishProgress.Minimum = 0;
        FinishProgress.Maximum = 1;
        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 1;
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = TotalSubsteps;

        await Task.Delay(1000);

        Ring.Visibility = Visibility.Collapsed;
        Title.Text = "Would you like to restart your computer now?";
        Subtitle.Visibility = Visibility.Collapsed;
        ReboundProgress.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Visible;
        ProgressBars.Visibility = Visibility.Collapsed;
        ProgressInfos.Visibility = Visibility.Collapsed;
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = TotalSubsteps + 10;
        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 0.5;
    }

    public void UpdateProgress(string title)
    {
        CurrentSubstep++;
        Title.Text = title;
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = CurrentSubstep;
    }

    private void Timer_Tick(object sender, object e) => TaskManager.StopTask("explorer.exe");

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

    public async Task<Task> InstallMsixPackage(string packagePath, string cachedPackagePath, string certificatePath, string displayAppName, string exeFile, string exeDestination, string lnkFile, string lnkDestination, string exeDisplayName, string lnkDisplayName)
    {
        CurrentStep++;

        // Substep 1: certificate

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        #region Certificate

        try
        {
            // Load the certificate from file
            var certificate = new X509Certificate2(certificatePath);

            // Define the store location and name
            var storeLocation = StoreLocation.LocalMachine;
            var storeName = StoreName.Root;

            // Open the certificate store
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                // Add the certificate to the store
                store.Add(certificate);

                // Close the store
                store.Close();
            }

            // Notify the user of success
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: {displayAppName} certificate installed.";
            await Task.Delay(50);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: {displayAppName} certificate installation failed.";
            await Task.Delay(50);
        }

        #endregion Certificate

        // Substep 2: cache package

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        #region CachePackage

        File.Copy(packagePath, cachedPackagePath);

        // Setup the process start info
        var procFolder = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Verb = "runas",                 // Run as administrator
            UseShellExecute = false,
            CreateNoWindow = true,// Required to redirect output
            Arguments = $"Add-AppxPackage -Path \"{cachedPackagePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Write output
        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Installing {displayAppName}...";

        // Start the process
        var resFolder = Process.Start(procFolder);

        var error = await resFolder.StandardError.ReadToEndAsync();

        // Wait for the process to exit
        await resFolder.WaitForExitAsync();

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: {(string.IsNullOrEmpty(error) ? $"{displayAppName} installed." : $"{displayAppName} installation failed: the package is already installed..")}";
        await Task.Delay(50);

        #endregion CachePackage

        // Substep 3: delete cached package

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        #region DeleteCachedPackage

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Cleaning remaining files...";
        try
        {
            File.Delete(cachedPackagePath);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: There are no remaining files. Skipping...";
        }
        await Task.Delay(50);

        #endregion DeleteCachedPackage

        // Substep 4: copy EXE

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        #region CopyEXE

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying {exeDisplayName}.exe...";
        try
        {
            File.Copy(exeFile, exeDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: The file already exists. Skipping...";
        }
        await Task.Delay(50);

        #endregion CopyEXE

        // Substep 5: copy new LNK

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        #region CopyLNK

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

        #endregion CopyLNK

        return Task.CompletedTask;
    }

    public async Task<Task> InstallExeWithShortcut(string exeFile, string exeDestination, string lnkFile, string lnkDestination, string exeDisplayName, string lnkDisplayName)
    {
        CurrentStep++;

        // Substep 1: copy EXE

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying {exeDisplayName}.exe...";
        try
        {
            File.Copy(exeFile, exeDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: The file already exists. Skipping...";
        }

        await Task.Delay(50);

        // Substep 2: copy new LNK file

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

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

    public async Task<Task> InstallAppInstallerFile(string displayName, string appxLocation, string taskName)
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

        // Substep 2: install APPX

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Installing {displayName}...";
        try
        {
            var args = $@"Add-AppxPackage -AppInstallerFile {appxLocation}";
            var info = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $@"-ExecutionPolicy Bypass -Command ""{args}"""
            };

            var process = Process.Start(info);
            await process.WaitForExitAsync();
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
                _ = process.Start();
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

    public async Task<Task> CreateRebound11Folder()
    {
        CurrentStep++;

        // Substep 1: create the folder

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            Subtitle.Text = $@"Step {CurrentStep} of {TotalSteps}: Creating the Rebound11 folder in ""C:\"".";
            _ = Directory.CreateDirectory("C:\\Rebound11");
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Directory already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 2: DLL file

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying r11imageres.dll...";
            File.Copy($@"{AppContext.BaseDirectory}\AppRT\r11imageres.dll", @"C:\Rebound11\r11imageres.dll");
        }
        catch
        {
            Subtitle.Text = $@"Step {CurrentStep} of {TotalSteps}: ""r11imageres.dll"" file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 3: desktop.ini

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying desktop.ini...";
            File.Copy($@"{AppContext.BaseDirectory}\AppRT\desktop2.ini", @"C:\Rebound11\desktop.ini", true);

            // Set attributes for the file
            File.SetAttributes(@"C:\Rebound11\desktop.ini", FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
            File.SetAttributes(@"C:\Rebound11\", FileAttributes.Directory | FileAttributes.System);
        }
        catch
        {
            Subtitle.Text = $@"Step {CurrentStep} of {TotalSteps}: ""desktop.ini"" file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 4: create wallpapers folder

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Creating wallpapers folder...";
            _ = Directory.CreateDirectory(@"C:\Rebound11\Wallpapers");
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Wallpapers folder already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 5: copy wallpapers

        UpdateProgress($"Installing Rebound 11: " + ((int)(CurrentSubstep / TotalSubsteps * 100)).ToString() + "%");

        try
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Copying wallpapers...";
            foreach (var file in Directory.GetFiles($@"{AppContext.BaseDirectory}\AppRT\Wallpapers\"))
            {
                var destFile = Path.Combine(@"C:\Rebound11\Wallpapers\", Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
        }
        catch
        {
            Subtitle.Text = $"Step {CurrentStep} of {TotalSteps}: Something went wrong. Skipping...";
        }
        await Task.Delay(50);

        return Task.CompletedTask;
    }
}
