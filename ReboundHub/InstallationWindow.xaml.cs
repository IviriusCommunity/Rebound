using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.Storage;
using Windows.System;
using Windows.UI.Text;
using WinUIEx;
using WinUIEx.Messaging;
using static System.Runtime.InteropServices.JavaScript.JSType;
using FileAttributes = System.IO.FileAttributes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundHub;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InstallationWindow : WindowEx
{
    public InstallationWindow(bool Files, bool Run, bool Defrag, bool Winver, bool UAC, bool OSK, bool TPM, bool DiskCleanup)
    {
        this.InitializeComponent();
        this.MoveAndResize(0, 0, 0, 0);

        Load(Files, Run, Defrag, Winver, UAC, OSK, TPM, DiskCleanup);
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

    public static class ExplorerManager
    {
        public static void StopExplorer()
        {
            try
            {
                // Find the explorer.exe process
                Process[] processes = Process.GetProcessesByName("explorer");
                foreach (var process in processes)
                {
                    // Kill the process
                    process.Kill();
                    process.WaitForExit(); // Ensure the process has exited
                    Console.WriteLine("Explorer stopped successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping explorer: {ex.Message}");
            }
        }

        public static void StartExplorer()
        {
            try
            {
                // Start a new explorer.exe process
                Process.Start("explorer.exe");
                Console.WriteLine("Explorer started successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting explorer: {ex.Message}");
            }
        }
    }

    public static void RestartPC()
    {
        //ExitWindowsEx(EWX_REBOOT | EWX_FORCE, SHTDN_REASON_MAJOR_SOFTWARE);
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = "shutdown /r /t 0",
            Verb = "runas"
        };
        var process = Process.Start(info);
    }

    DispatcherTimer timer = new DispatcherTimer();

    double currentStep = 0;
    double totalSteps = 0;
    double currentSubstep = 0;
    double totalSubsteps = 0;

    public async void Load(bool Files, bool Run, bool Defrag, bool Winver, bool UAC, bool OSK, bool TPM, bool DiskCleanup)
    {
        timer.Interval = new TimeSpan(3);
        timer.Tick += Timer_Tick;
        timer.Start();

        // Rebound11 Folder
        totalSteps += 1;
        totalSubsteps += 6;

        // Control Panel
        totalSteps += 1;
        totalSubsteps += 3;

        // Files
        if (Files == true)
        {
            totalSteps += 1;
            totalSubsteps += 1;
        }

        // Run
        if (Run == true)
        {
            totalSteps += 1;
            totalSubsteps += 9;
        }

        // Defrag
        if (Defrag == true)
        {
            totalSteps += 1;
            totalSubsteps += 6;
        }

        // Winver
        if (Winver == true)
        {
            totalSteps += 1;
            totalSubsteps += 6;
        }

        // UAC
        if (UAC == true)
        {
            totalSteps += 1;
            totalSubsteps += 3;
        }

        // OSK
        if (OSK == true)
        {
            totalSteps += 1;
            totalSubsteps += 3;
        }

        // TPM
        if (TPM == true)
        {
            totalSteps += 1;
            totalSubsteps += 6;
        }

        // DiskCleanup
        if (DiskCleanup == true)
        {
            totalSteps += 1;
            totalSubsteps += 6;
        }

        // Initialization

        await Task.Delay(50);
        this.SetIsAlwaysOnTop(true);
        this.SetWindowPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        Title.Text = "Installing Rebound 11";
        Subtitle.Text = "Starting...";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = 0;
        ReboundProgress.Minimum = 6;
        ReboundProgress.Maximum = totalSubsteps - 1;
        InstallProgress.Minimum = 0;
        InstallProgress.Maximum = 6;
        FinishProgress.Minimum = totalSubsteps - 1;
        FinishProgress.Maximum = totalSubsteps;
        InstallText.Opacity = 1;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 0.5;

        await Task.Delay(1000);

        #region Folder

        // Creating the Rebound11 folder (3 substeps)

        currentStep += 1;

        // Substep 1: folder

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Creating the Rebound11 folder in \"C:\\\".";
            Directory.CreateDirectory("C:\\Rebound11");
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Directory already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 2: ico file

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying r11imageres.dll...";
            File.Copy($@"{AppContext.BaseDirectory}\Rebound11Files\Rebound11\r11imageres.dll", @"C:\Rebound11\r11imageres.dll");
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: \"r11imageres.dll\" file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 3: desktop.ini

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying desktop.ini...";
            File.Copy($@"{AppContext.BaseDirectory}\Rebound11Files\Rebound11\desktop2.ini", @"C:\Rebound11\desktop.ini", true);
            // Set attributes for the file
            File.SetAttributes(@"C:\Rebound11\desktop.ini", FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
            File.SetAttributes(@"C:\Rebound11\", FileAttributes.Directory | FileAttributes.System);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: \"desktop.ini\" file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 4: create wallpapers folder

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Creating wallpapers folder...";
            Directory.CreateDirectory("C:\\Rebound11\\Wallpapers");
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Wallpapers folder already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 5: copy wallpapers

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying wallpapers...";
            foreach (var file in Directory.GetFiles($"{AppContext.BaseDirectory}\\Assets\\Wallpapers\\"))
            {
                string destFile = Path.Combine("C:\\Rebound11\\Wallpapers\\", Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Something went wrong. Skipping...";
        }
        await Task.Delay(50);

        // Substep 6: create start menu shortcuts folder

        currentSubstep++;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        try
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Creating Rebound 11 Tools folder...";
            // Path to the Start Menu's Programs directory
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string programsPath = Path.Combine(startMenuPath, "Programs");

            // Folder name you want to create
            string folderName = "Rebound 11 Tools";
            string folderPath = Path.Combine(programsPath, folderName);
            Directory.CreateDirectory(folderPath);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Rebound 11 Tools folder already exists. Skipping...";
        }
        await Task.Delay(50);

        #endregion Folder

        #region Control Panel

        await InstallExeWithShortcut(
            "Rebound Control Panel",
            $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rcontrol.exe",
            $"C:\\Rebound11\\rcontrol.exe",
            $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Control Panel.lnk",
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
            await InstallAppPackage(
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundDefrag.msix",
                "C:\\Rebound11\\ReboundDefrag.msix",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundDefrag.cer",
                "Rebound Defragment and Optimize Drives",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rdfrgui.exe",
                $"C:\\Rebound11\\rdfrgui.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\dfrgui.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\dfrgui.lnk",
                "rdfrgui",
                "dfrgui");
        }

        #endregion Defragment

        #region Winver

        if (Winver == true)
        {
            await InstallAppPackage(
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundWinver.msix",
                "C:\\Rebound11\\ReboundWinver.msix",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundWinver.cer",
                "Rebound Winver",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rwinver.exe",
                $"C:\\Rebound11\\rwinver.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\winver.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\winver.lnk",
                "rwinver",
                "winver");
        }

        #endregion Winver

        #region Files

        if (Files == true)
        {
            currentStep++;
            currentSubstep++;
        }

        #endregion Files

        #region UAC

        if (UAC == true)
        {
            await InstallExeWithShortcut(
                "Change User Account Control settings",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\ruacsettings.exe",
                $"C:\\Rebound11\\ruacsettings.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Change User Account Control settings.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Change User Account Control settings.lnk",
                "ruacsettings",
                "Change User Account Control settings");
        }

        #endregion UAC

        #region Run

        if (Run == true)
        {
            await InstallAppPackage(
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundRun.msix",
                "C:\\Rebound11\\ReboundRun.msix",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundRun.cer",
                "Rebound Run",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rrun.exe",
                $"C:\\Rebound11\\rrun.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Run.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\System Tools\Run.lnk",
                "rrun",
                "Run");

            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            await InstallExeWithShortcut(
                "Rebound Run Startup Task",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rrunSTARTUP.exe",
                $"C:\\Rebound11\\rrunSTARTUP.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\ReboundRunStartup.lnk",
                $"{startupFolderPath}\\ReboundRunStartup.lnk",
                "rrunSTARTUP",
                "ReboundRunStartup");
        }

        #endregion Run

        #region DiskCleanup

        if (DiskCleanup == true)
        {
            await InstallAppPackage(
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundDiskCleanup.msix",
                "C:\\Rebound11\\ReboundDiskCleanup.msix",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundDiskCleanup.cer",
                "Rebound Disk Cleanup",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rcleanmgr.exe",
                $"C:\\Rebound11\\rcleanmgr.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Disk Cleanup.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\Disk Cleanup.lnk",
                "rcleanmgr",
                "Disk Cleanup");
        }

        #endregion DiskCleanup

        #region TPM

        if (TPM == true)
        {
            await InstallAppPackage(
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundTpm.msix",
                "C:\\Rebound11\\ReboundTpm.msix",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundTpm.cer",
                "Rebound TPM Management",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rtpm.exe",
                $"C:\\Rebound11\\rtpm.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\tpm.msc.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonAdminTools)}\tpm.msc.lnk",
                "rtpm",
                "tpm.msc");
        }

        #endregion TPM

        #region OSK

        if (OSK == true)
        {
            await InstallExeWithShortcut(
                "UWP On-Screen Keyboard",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rosk.exe",
                $"C:\\Rebound11\\rosk.exe",
                $"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\On-Screen Keyboard.lnk",
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Accessibility\On-Screen Keyboard.lnk",
                "rosk",
                "On-Screen Keyboard");
        }

        #endregion OSK

        currentSubstep = totalSubsteps;
        Title.Text = $"Installing Rebound 11: 100%";
        Subtitle.Text = $"Closing Rebound Hub...";
        App.m_window.Close();
        ReboundProgress.Minimum = 0;
        ReboundProgress.Maximum = 1;
        InstallProgress.Minimum = 0;
        InstallProgress.Maximum = 1;
        FinishProgress.Minimum = 0;
        FinishProgress.Maximum = 1;
        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 1;
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = totalSubsteps;

        await Task.Delay(1000);

        Ring.Visibility = Visibility.Collapsed;
        Title.Text = "Would you like to restart your computer now?";
        Subtitle.Visibility = Visibility.Collapsed;
        ReboundProgress.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Visible;
        ProgressBars.Visibility = Visibility.Collapsed;
        ProgressInfos.Visibility = Visibility.Collapsed;
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = totalSubsteps + 10;
        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 0.5;
        FinishText.Opacity = 0.5;
    }

    public async Task<Task> InstallAppPackage(string packagePath, string cachedPackagePath, string certificatePath, string displayAppName, string exeFile, string exeDestination, string lnkFile, string lnkDestination, string exeDisplayName, string lnkDisplayName)
    {
        // 6 SUBSTEPS

        currentStep += 1;

        // Substep 1: certificate

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        try
        {
            // Load the certificate from file
            X509Certificate2 certificate = new X509Certificate2(certificatePath);

            // Define the store location and name
            StoreLocation storeLocation = StoreLocation.LocalMachine;
            StoreName storeName = StoreName.Root;

            // Open the certificate store
            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                // Add the certificate to the store
                store.Add(certificate);

                // Close the store
                store.Close();
            }

            // Notify the user of success
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: {displayAppName} certificate installed.";
            await Task.Delay(50);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., file not found, permission issues)
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: {displayAppName} certificate installation failed.";
            await Task.Delay(50);
        }

        // Substep 2: cache package

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

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

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Installing {displayAppName}...";

        // Start the process
        var resFolder = Process.Start(procFolder);

        // Read output and errors
        string output = await resFolder.StandardOutput.ReadToEndAsync();
        string error = await resFolder.StandardError.ReadToEndAsync();

        // Wait for the process to exit
        await resFolder.WaitForExitAsync();

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: {(string.IsNullOrEmpty(error) ? $"{displayAppName} installed." : $"{displayAppName} installation failed: the package is already installed..")}";

        await Task.Delay(50);

        // Substep 3: delete cached package

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Cleaning remaining files...";
        try
        {
            File.Delete(cachedPackagePath);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: There are no remaining files. Skipping...";
        }
        await Task.Delay(50);

        // Substep 4: copy rwinver.exe

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying {exeDisplayName}.exe...";
        try
        {
            File.Copy(exeFile, exeDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 5: delete winver.lnk

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

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

        // Substep 6: copy new winver.lnk

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

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

    public async Task<Task> InstallExeWithShortcut(string displayAppName, string exeFile, string exeDestination, string lnkFile, string lnkDestination, string exeDisplayName, string lnkDisplayName)
    {
        // 3 SUBSTEPS

        currentStep += 1;

        // Substep 4: copy rwinver.exe

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

        Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying {exeDisplayName}.exe...";
        try
        {
            File.Copy(exeFile, exeDestination, true);
        }
        catch
        {
            Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
        }
        await Task.Delay(50);

        // Substep 5: delete winver.lnk

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

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

        // Substep 6: copy new winver.lnk

        currentSubstep += 1;
        Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
        ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

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

    private void Timer_Tick(object sender, object e)
    {
        ExplorerManager.StopExplorer();
    }

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

    private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.LeftWindows || e.Key == VirtualKey.RightWindows)
        {
            e.Handled = true;
            // Optionally, display a message or perform other actions
        }
    }
}
