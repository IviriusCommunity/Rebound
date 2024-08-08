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
using Windows.Storage;
using Windows.System;
using Windows.UI.Text;
using WinUIEx;
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
    public InstallationWindow(bool Files, bool Run, bool Defrag)
    {
        this.InitializeComponent();
        this.MoveAndResize(0, 0, 0, 0);
        Load(Files, Run, Defrag);
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_R = 0x52;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private static IntPtr hookId = IntPtr.Zero;
    private static LowLevelKeyboardProc keyboardProc;

    public static async void StartHook()
    {
        keyboardProc = HookCallback;
        hookId = SetHook(keyboardProc);

        await Task.Delay(1000);

        StartHook();
    }

    public static void StopHook()
    {
        UnhookWindowsHookEx(hookId);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public const int WM_KEYUP = 0x0101;

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam is WM_KEYUP or WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            // Check for Win
            if (vkCode is VK_LWIN or VK_RWIN)
            {
                // Prevent default behavior of Win
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    public static extern short GetKeyState(int keyCode);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    private const uint EWX_REBOOT = 0x00000002;
    private const uint EWX_FORCE = 0x00000004;
    private const uint SHTDN_REASON_MAJOR_SOFTWARE = 0x00000008;

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

    public async void Load(bool Files, bool Run, bool Defrag)
    {
        await Task.Run(() => StartHook());

        // Variables
        double currentStep = 0;
        double totalSteps = 0;
        double currentSubstep = 0;
        double totalSubsteps = 0;

        // Rebound11 Folder
        totalSteps += 1;
        totalSubsteps += 5;

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
            totalSubsteps += 8;
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

        // Substep 4: copy wallpapers

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

        #endregion Folder

        #region Control Panel

        currentStep++;
        currentSubstep += 3;

        InstallText.Opacity = 0.5;
        ReboundText.Opacity = 1;
        FinishText.Opacity = 0.5;

        #endregion Control Panel

        #region Defragment

        #endregion Defragment

        #region Regedit

        #endregion Regedit

        #region Winver

        #endregion Winver

        #region Files

        #endregion Files

        #region Run

        if (Run == true)
        {
            // Creating the Rebound11 folder (3 substeps)

            currentStep += 1;

            // Substep 1: certificate

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            try
            {
                // Load the certificate from file
                X509Certificate2 certificate = new X509Certificate2($"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundRun.cer");

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
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: Rebound Run certificate installed.";
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, permission issues)
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: Rebound Run certificate installation failed.";
                await Task.Delay(50);
            }

            // Substep 2: cache package

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            string packagePath = $"{AppContext.BaseDirectory}\\Rebound11Files\\AppPackages\\ReboundRun.msix";

            File.Copy(packagePath, "C:\\Rebound11\\ReboundRun.msix");

            // Setup the process start info
            var procFolder = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Verb = "runas",                 // Run as administrator
                UseShellExecute = false,
                CreateNoWindow = true,// Required to redirect output
                Arguments = $"Add-AppxPackage -Path \"C:\\Rebound11\\ReboundRun.msix\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Installing Rebound Run...";

            // Start the process
            var resFolder = Process.Start(procFolder);

            // Read output and errors
            string output = await resFolder.StandardOutput.ReadToEndAsync();
            string error = await resFolder.StandardError.ReadToEndAsync();

            // Wait for the process to exit
            await resFolder.WaitForExitAsync();

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: {(string.IsNullOrEmpty(error) ? "Rebound Run installed." : $"Rebound Run installation failed: the package is already installed..")}";

            await Task.Delay(50);

            // Substep 3: delete cached package

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Cleaning remaining files...";
            try
            {
                File.Delete("C:\\Rebound11\\ReboundRun.msix");
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: There are no remaining files. Skipping...";
            }
            await Task.Delay(50);

            // Substep 4: copy rrun.exe

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying rrun.exe...";
            try
            {
                File.Copy($"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rrun.exe", $"C:\\Rebound11\\rrun.exe");
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
            }
            await Task.Delay(50);

            // Substep 5: copy rrunSTARTUP.exe

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying rrunSTARTUP.exe...";
            try
            {
                File.Copy($"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\rrunSTARTUP.exe", $"C:\\Rebound11\\rrunSTARTUP.exe");
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
            }
            await Task.Delay(50);

            // Substep 6: copy ReboundRunStartup.exe

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying ReboundRunStartup.lnk...";
            try
            {
                File.Copy($"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\ReboundRunStartup.lnk", $"{startupFolderPath}\\ReboundRunStartup.lnk");
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
            }
            await Task.Delay(50);

            // Substep 7: delete Run.lnk

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Deleting Run.lnk...";
            try
            {
                File.Delete($"{Environment.GetFolderPath(Environment.SpecialFolder.AdminTools)}\\Run.lnk");
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file does not exist. Skipping...";
            }
            await Task.Delay(50);

            // Substep 8: copy new Run.lnk

            currentSubstep += 1;
            Title.Text = $"Installing Rebound 11: " + ((int)(currentSubstep / totalSubsteps * 100)).ToString() + "%";
            ReboundProgress.Value = InstallProgress.Value = FinishProgress.Value = currentSubstep;

            Subtitle.Text = $"Step {currentStep} of {totalSteps}: Copying new Run.lnk...";
            try
            {
                File.Copy($"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Run.lnk", $"{Environment.GetFolderPath(Environment.SpecialFolder.AdminTools)}\\Run.lnk", true);
            }
            catch
            {
                Subtitle.Text = $"Step {currentStep} of {totalSteps}: The file already exists. Skipping...";
            }
            await Task.Delay(50);
        }

        #endregion Run

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

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        Ring.Visibility = Visibility.Visible;
        Title.Text = "Restarting...";
        Subtitle.Visibility = Visibility.Collapsed;
        Description.Visibility = Visibility.Collapsed;
        Buttons.Visibility = Visibility.Collapsed;

        await Task.Delay(3000);

        StopHook();
        RestartPC();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        StopHook();
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
