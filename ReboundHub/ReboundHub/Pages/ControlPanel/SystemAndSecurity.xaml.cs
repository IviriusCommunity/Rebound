using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.StartScreen;
using WinUIEx;
using IWshRuntimeLibrary;
using Windows.Storage;
using File = System.IO.File;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundHub.ReboundHub.Pages.ControlPanel;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SystemAndSecurity : Page
{
    public SystemAndSecurity()
    {
        this.InitializeComponent();
        if (App.cpanelWin != null) App.cpanelWin.SetWindowIcon("Assets\\AppIcons\\imageres_195.ico");
        if (App.cpanelWin != null) App.cpanelWin.Title = "Security and Maintenance";
        CheckDefenderStatus();
    }

    void CheckDefenderStatus()
    {
        try
        {
            // Query WMI for Windows Defender status
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "root\\SecurityCenter2",
                "SELECT * FROM AntivirusProduct");

            bool isProtectionEnabled = false;
            foreach (ManagementObject queryObj in searcher.Get())
            {
                Debug.WriteLine("Name: {0}", queryObj["displayName"]);
                Debug.WriteLine("Product State: {0}", queryObj["productState"]);
                Debug.WriteLine("Timestamp: {0}", DateTime.Now);
                InfoBarSeverity sev = InfoBarSeverity.Informational;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "A") sev = InfoBarSeverity.Error;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "B") sev = InfoBarSeverity.Success;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "C") sev = InfoBarSeverity.Warning;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "D") sev = InfoBarSeverity.Warning;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "E") sev = InfoBarSeverity.Informational;
                string msg = string.Empty;
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "A") msg = "This antivirus service is disabled.";
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "B") msg = "You're protected.";
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "C") msg = "This antivirus service is snoozed.";
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "D") msg = "Your subscription for your 3rd party antivirus service provider has expired. Please contact them for a new subscription or download another antivirus program.";
                if (DecodeProductState((int)((uint)queryObj["productState"])).Substring(0, 1) == "E") msg = "Unknown.";
                bool exists = false;
                foreach (InfoBar bar in SecurityBars.Children.Cast<InfoBar>())
                {
                    if (bar.Title == queryObj["displayName"].ToString() + ":")
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists != true)
                {
                    var x = new InfoBar()
                    {
                        IsOpen = true,
                        IsClosable = false,
                        Severity = sev,
                        Title = queryObj["displayName"].ToString() + ":",
                        Message = msg
                    };
                    SecurityBars.Children.Add(x);
                    if (queryObj["displayName"].ToString() == "Windows Defender")
                    {
                        var y = new Button()
                        {
                            Content = "Open",
                            Margin = new Thickness(0, 0, 0, 15)
                        };
                        y.Click += async (s, e) => { await Launcher.LaunchUriAsync(new Uri("windowsdefender://")); };
                        x.Content = y;
                    }
                    if (sev == InfoBarSeverity.Success) isProtectionEnabled = true;
                }
            }

            int securityIndex = UACStatus() + (isProtectionEnabled == true ? 2 : 0);

            var sev2 = InfoBarSeverity.Informational;
            string status = string.Empty;

            switch (securityIndex)
            {
                case >= 4:
                    {
                        sev2 = InfoBarSeverity.Success;
                        status = "Great!";
                        break;
                    }
                case >= 3:
                    {
                        sev2 = InfoBarSeverity.Warning;
                        status = "Exposed to risks.";
                        break;
                    }
                default:
                    {
                        sev2 = InfoBarSeverity.Error;
                        status = "Needs attention.";
                        break;
                    }
            }

            StatusInfoBar.Severity = sev2;
            StatusInfoBar.Title = $"Security Index: {(int)((double)securityIndex / 5 * 10)}/10";
            StatusInfoBar.Message = $"-   Current status: {status}";

        }
        catch (ManagementException e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
        }
    }

    public static int UACStatus()
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
            {
                if (key != null)
                {
                    // Read the EnableLUA value
                    object enableLUAValue = key.GetValue("EnableLUA");
                    object consentPromptBehaviorAdminValue = key.GetValue("ConsentPromptBehaviorAdmin");
                    object consentPromptBehaviorUserValue = key.GetValue("ConsentPromptBehaviorUser");
                    object promptOnSecureDesktopValue = key.GetValue("PromptOnSecureDesktop");

                    if (enableLUAValue != null)
                    {
                        int enableLUA = Convert.ToInt32(enableLUAValue);
                        if (enableLUA == 1)
                        {
                            // UAC is enabled
                            int consentPromptBehaviorAdmin = consentPromptBehaviorAdminValue != null ? Convert.ToInt32(consentPromptBehaviorAdminValue) : -1;
                            int promptOnSecureDesktop = promptOnSecureDesktopValue != null ? Convert.ToInt32(promptOnSecureDesktopValue) : -1;

                            int value = 0;

                            // Determine the UAC level
                            if (consentPromptBehaviorAdmin == 2)
                            {
                                value = 3; // UAC enabled and desktop is dimmed
                            }
                            else if (consentPromptBehaviorAdmin == 5)
                            {
                                if (promptOnSecureDesktop == 1) value = 2; // UAC always prompts for elevation (UAC always on)
                                else value = 1; // UAC always prompts for elevation (UAC always on)
                            }
                            else
                            {
                                value = 0; // UAC enabled and desktop is not dimmed
                            }
                            return value;
                        }
                        else
                        {
                            return 0; // UAC is disabled
                        }
                    }
                    else
                    {
                        return -10; // Error: UAC setting not found
                    }
                }
                else
                {
                    return -100; // Error: Registry key not found
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("An error occurred while checking UAC status: " + ex.Message);
            return -1000; // Error: Exception occurred
        }
    }

    string DecodeProductState(int productState)
    {
        // Define the bit masks
        const int ProductStateMask = 0xF000;
        const int SignatureStatusMask = 0x00F0;
        const int ProductOwnerMask = 0x0F00;

        // Extract bits using bitwise AND and shift right
        int productStateValue = (productState & ProductStateMask) >> 12;
        int signatureStatusValue = (productState & SignatureStatusMask) >> 4;
        int productOwnerValue = (productState & ProductOwnerMask) >> 8;

        // Decode the values
        string productStateStr = productStateValue switch
        {
            0x0 => "A",        // Off
            0x1 => "B",        // On
            0x2 => "C",        // Snoozed
            0x3 => "D",        // Expired
            _ => "E"           // Unknown
        };

        string signatureStatusStr = signatureStatusValue switch
        {
            0x0 => "UpToDate",
            0x1 => "OutOfDate",
            _ => "Unknown"
        };

        string productOwnerStr = productOwnerValue switch
        {
            0x0 => "Non-Microsoft",
            0x1 => "Microsoft",
            _ => "Unknown"
        };

        // Define the bit masks based on known values
        const int EnabledMask = 0x1;          // Bit 0
        const int RealTimeProtectionMask = 0x2; // Bit 1
        const int SampleSubmissionMask = 0x4;  // Bit 2
        const int UpToDateMask = 0x8;          // Bit 3
        const int MalwareDetectedMask = 0x10;  // Bit 4

        // Extract bits using bitwise AND
        bool isEnabled = (productStateValue & EnabledMask) != 0;
        bool isRealTimeProtectionEnabled = (productStateValue & RealTimeProtectionMask) != 0;
        bool isSampleSubmissionEnabled = (productStateValue & SampleSubmissionMask) != 0;
        bool isUpToDate = (productStateValue & UpToDateMask) != 0;
        bool isMalwareDetected = (productStateValue & MalwareDetectedMask) != 0;

        //return Convert.ToString(productState, 2);

        // Format the result
        return $"{productStateStr}";
    }

    // Constants for SystemParametersInfo function
    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int MAX_PATH = 260;

    // P/Invoke declaration for SystemParametersInfo function
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    // Method to retrieve the current user's wallpaper path
    private string GetWallpaperPath()
    {
        StringBuilder wallpaperPath = new StringBuilder(MAX_PATH);
        SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);
        return wallpaperPath.ToString();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.cpanelWin != null)
        {
            App.cpanelWin.RootFrame.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.cpanelWin != null)
        {
            App.cpanelWin.RootFrame.GoForward();
        }
    }

    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        App.cpanelWin.Close();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.cpanelWin != null)
        {
            var oldHistory = App.cpanelWin.RootFrame.ForwardStack;
            var newList = new List<PageStackEntry>();
            foreach (var item in oldHistory)
            {
                newList.Add(item);
            }
            App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            App.cpanelWin.RootFrame.GoBack();
            App.cpanelWin.RootFrame.ForwardStack.Clear();
            foreach (var item in newList)
            {
                App.cpanelWin.RootFrame.ForwardStack.Add(item);
            }
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (App.cpanelWin != null)
        {
            App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as ComboBox).SelectedIndex == 0 && (App.cpanelWin != null))
        {
            App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
        if ((sender as ComboBox).SelectedIndex == 1 && (App.cpanelWin != null))
        {
            App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if ((NavigationViewItem)sender.SelectedItem == AppearanceItem || (NavigationViewItem)sender.SelectedItem == Re11Item)
            {
                App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            }
            if ((NavigationViewItem)sender.SelectedItem == WinToolsItem)
            {
                App.cpanelWin.AddressBox.Text = @"Control Panel\System and Security\Windows Tools";
                App.cpanelWin.NavigateToPath();
            }
        }
        catch (Exception ex)
        {
            if (App.cpanelWin != null) App.cpanelWin.Title = ex.Message;
        }
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = "Start-Process -FilePath \"C:\\Windows\\System32\\control.exe\"",
            Verb = "runas",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(info);

        App.cpanelWin.Close();
    }

    private void SettingsCard_Click_1(object sender, RoutedEventArgs e)
    {
        var win = new UACWindow();
        win.Show();
    }

    static void RunPowerShellScript(string script)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"{script}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Error: {error}");
            }
            else
            {
                Console.WriteLine($"Output: {output}");
            }
        }
    }

    // Import the CreateDirectory function from kernel32.dll
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

    // Define the constants for error handling
    private const int ERROR_ALREADY_EXISTS = 183;

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        string appPath = $@"{AppContext.BaseDirectory}\Rebound11Files\Executables\QuickFullComputerCleanup.exe"; // Path to the application you want to pin

        string destDir = $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools";

        // Define the location for the shortcut in the Start menu for the current user
        string shortcutLocation = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), // Start Menu path
            "Programs", "Rebound 11 Tools",
            "Rebound Deep Cleaning Tool.lnk"); // The .lnk extension is used for shortcuts

        // PowerShell command to create the folder
        string powerShellCommand = @"
                $roamingPath = [System.Environment]::GetFolderPath('ApplicationData');
                $startMenuPath = Join-Path -Path $roamingPath -ChildPath 'Microsoft\Windows\Start Menu\Programs';
                $newFolderPath = Join-Path -Path $startMenuPath -ChildPath 'Rebound 11 Tools';
                if (-not (Test-Path -Path $newFolderPath)) {
                    New-Item -ItemType Directory -Path $newFolderPath;
                    Write-Output 'Folder created';
                } else {
                    Write-Output 'Folder already exists';
                }";

        // Start the PowerShell process
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellCommand}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();

            // Capture the output
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Show the output (optional)
            // This can be replaced with whatever UI feedback you want to give
            Debug.WriteLine(output);
        }

        // PowerShell command to copy the file
        string powerShellCommand2 = $@"
        $sourceFilePath = '{$"{AppContext.BaseDirectory}\\Rebound11Files\\shcre11\\Rebound 11 Quick Full Computer Cleanup.lnk"}';
        $destinationDirectory = '{destDir}';
        $destinationFilePath = Join-Path -Path $destinationDirectory -ChildPath (Split-Path -Leaf $sourceFilePath);
        if (Test-Path -Path $sourceFilePath) {{
            if (-not (Test-Path -Path $destinationDirectory)) {{
                New-Item -ItemType Directory -Path $destinationDirectory;
            }}
            Copy-Item -Path $sourceFilePath -Destination $destinationFilePath -Force;
            Write-Output 'File copied to: $destinationFilePath';
        }} else {{
            Write-Output 'Source file does not exist: $sourceFilePath';
        }}";

        // Start the PowerShell process
        ProcessStartInfo startInfo2 = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellCommand2}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo2;
            process.Start();

            // Capture the output
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Show the output (optional)
            Debug.WriteLine(output);
        }
    }

    static void CreateShortcut(string shortcutLocation, string targetPath)
    {
        // Create a new WshShell instance
        WshShell shell = new WshShell();

        // Create the shortcut object
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

        // Set the target path (path to the executable)
        shortcut.TargetPath = targetPath;

        // Set the working directory (optional)
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);

        // Set the description (optional)
        //shortcut.Description = "Shortcut";

        // Set an icon for the shortcut (optional)
        //shortcut.IconLocation = targetPath + ",0";

        // Save the shortcut
        shortcut.Save();
    }

    static void PinShortcutToStartMenu(string shortcutPath)
    {
        string command = $"powershell -command \"$s=(New-Object -COM WScript.Shell).CreateShortcut('{shortcutPath}'); $s.Save(); Start-Process explorer.exe /select, '{shortcutPath}'\"";
        System.Diagnostics.Process.Start("cmd.exe", $"/C {command}");
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        //PinToTaskbar($"{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\Quick Full Computer Cleanup.exe");
    }

    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        // Start the PowerShell process
        ProcessStartInfo startInfo2 = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Process -FilePath '{AppContext.BaseDirectory}\\Rebound11Files\\Executables\\QuickFullComputerCleanup.exe'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo2;
            process.Start();

            // Capture the output
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Show the output (optional)
            Debug.WriteLine(output);
        }
    }
}
