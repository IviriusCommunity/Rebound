using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Control.ViewModels;
using WinUIEx;

namespace Rebound.Control.Views;

public sealed partial class SystemAndSecurity : Page
{
    public SystemAndSecurity()
    {
        this?.InitializeComponent();
        App.cpanelWin?.TitleBarEx.SetWindowIcon("Assets\\AppIcons\\imageres_195.ico");
        if (App.cpanelWin is not null)
        {
            App.cpanelWin.Title = "System and Security";
        }
    }

    public async void GetCurrentSecurityIndex()
    {
        _ = PleaseWaitDialog.ShowAsync();

        await Task.Delay(1000);
        await UpdateSecurityInformation();
    }

    public async Task UpdateSecurityInformation()
    {
        var uac = await SystemAndSecurityModel.UACStatus();
        var defenderStatus = await SystemAndSecurityModel.CheckDefenderStatus(SecurityBars);
        var updatesPending = await SystemAndSecurityModel.AreUpdatesPending();
        var driveEncrypted = await SystemAndSecurityModel.IsDriveEncrypted("C");
        var isPasswordComplex = await SystemAndSecurityModel.IsPasswordComplex();
        var securityIndex =
            uac * 1 +      // 10% of total
            (defenderStatus == true ? 1 : 0) * 5 + // 50% of total
            (updatesPending == false ? 1 : 0) * 2.5 + // 25% of total
            (driveEncrypted == true ? 1 : 0) * 1 + // 10% of total
            (isPasswordComplex == true ? 1 : 0) * 0.5; // 5% of total

        string status;
        InfoBarSeverity sev2;

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

        PleaseWaitDialog.Hide();
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
            if (App.cpanelWin is not null)
            {
                App.cpanelWin.Title = ex.Message;
            }
        }
    }

    private void SettingsCard_Click_1(object sender, RoutedEventArgs e)
    {
        var win = new UACWindow();
        win.Show();
    }

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
