using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Rebound.Control.ViewModels;
using Windows.System;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Control.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SystemAndSecurity : Page
{
    public SystemAndSecurity()
    {
        this.InitializeComponent();
        if (App.cpanelWin != null) App.cpanelWin.TitleBarEx.SetWindowIcon("Assets\\AppIcons\\imageres_195.ico");
        if (App.cpanelWin != null) App.cpanelWin.Title = "System and Security";
        //GetCurrentSecurityIndex();
    }



    public async void GetCurrentSecurityIndex()
    {
        await Task.Delay(1000);
            await UpdateSecurityInformation();
        /*DispatcherQueue.TryEnqueue(async () =>
        {
            await UpdateSecurityInformation();
        });*/
    }

    public async Task UpdateSecurityInformation()
    {
        var uac = await SystemAndSecurityModel.UACStatus();
        var defenderStatus = await SystemAndSecurityModel.CheckDefenderStatus(SecurityBars);
        var updatesPending = await SystemAndSecurityModel.AreUpdatesPending();
        var driveEncrypted = await SystemAndSecurityModel.IsDriveEncrypted("C");
        var isPasswordComplex = await SystemAndSecurityModel.IsPasswordComplex();
        double securityIndex =
            uac * 1 +      // 10% of total
            (defenderStatus == true ? 1 : 0) * 5 + // 50% of total
            (updatesPending == false ? 1 : 0) * 2.5 + // 25% of total
            (driveEncrypted == true ? 1 : 0) * 1 + // 10% of total
            (isPasswordComplex == true ? 1 : 0) * 0.5; // 5% of total

        var sev2 = InfoBarSeverity.Informational;
        string status = string.Empty;

        switch (securityIndex)
        {
            case >= 8:
                {
                    sev2 = InfoBarSeverity.Success;
                    status = "Great!";
                    break;
                }
            case >= 5:
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

        string uacStatus;

        switch (uac)
        {
            case 1:
                {
                    uacStatus = "Always on";
                    break;
                }
            case 0.75:
                {
                    uacStatus = "On (dim desktop)";
                    break;
                }
            case 0.5:
                {
                    uacStatus = "On (do not dim desktop)";
                    break;
                }
            default:
                {
                    uacStatus = "Off";
                    break;
                }
        }

        StatusInfoBar.Severity = sev2;
        StatusInfoBar.Title = $"Security Index: {(int)securityIndex}/10";
        StatusInfoBar.Message = $@"Current status: {status}
UAC: {uacStatus}
Antivirus: {(defenderStatus == true ? "Enabled" : "Disabled")}
Pending updates: {(updatesPending == true ? "Yes" : "No")}
Encrypted drive (C:): {(driveEncrypted == true ? "Yes" : "No")}
Complex password: {(isPasswordComplex == true ? "Yes" : "No")}";
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
        // TODO: Add this back
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
        $sourceFilePath = '{$"{AppContext.BaseDirectory}\\Shortcuts\\Rebound 11 Quick Full Computer Cleanup.lnk"}';
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
        //PinToTaskbar($"{AppContext.BaseDirectory}\\Reserved\\Quick Full Computer Cleanup.exe");
    }

    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        // Start the PowerShell process
        ProcessStartInfo startInfo2 = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Process -FilePath '{AppContext.BaseDirectory}\\Reserved\\QuickFullComputerCleanup.exe'\"",
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

    private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        GetCurrentSecurityIndex();
    }
}
