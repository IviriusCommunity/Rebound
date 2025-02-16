using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Control.ViewModels;
using WinUIEx;
using File = System.IO.File;

namespace Rebound.Control.Views;

public sealed partial class SystemAndSecurity : Page
{
    public SystemAndSecurity()
    {
        this?.InitializeComponent();
        App.ControlPanelWindow?.TitleBarEx.SetWindowIcon("AppRT\\Exported\\imageres_195.ico");
        if (App.ControlPanelWindow is not null)
        {
            App.ControlPanelWindow.Title = "System and Security";
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
            (uac * 1) +      // 10% of total
            ((defenderStatus == true ? 1 : 0) * 5) + // 50% of total
            ((updatesPending == false ? 1 : 0) * 2.5) + // 25% of total
            ((driveEncrypted == true ? 1 : 0) * 1) + // 10% of total
            ((isPasswordComplex == true ? 1 : 0) * 0.5); // 5% of total

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
            if ((NavigationViewItem)sender.SelectedItem == WinToolsItem)
            {
                App.ControlPanelWindow.AddressBox.Text = @"Control Panel\System and Security\Windows Tools";
                App.ControlPanelWindow.NavigateToPath();
            }
        }
        catch (Exception ex)
        {
            if (App.ControlPanelWindow is not null)
            {
                App.ControlPanelWindow.Title = ex.Message;
            }
        }
    }

    private void SettingsCard_Click_1(object sender, RoutedEventArgs e)
    {
        var win = new UACWindow();
        _ = win.Show();
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        _ = $@"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools";

        // PowerShell command to create the folder
        var powerShellCommand = @"
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
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellCommand}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process())
        {
            process.StartInfo = startInfo;
            _ = process.Start();

            // Capture the output
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        _ = await InstallExeWithShortcut(
            "Rebound 11 Quick Full Computer Cleanup",
            $"{AppContext.BaseDirectory}\\Reserved\\QuickFullComputerCleanup.exe",
            @"C:\Rebound11\QuickFullComputerCleanup.exe",
            $"{AppContext.BaseDirectory}\\Shortcuts\\Rebound 11 Quick Full Computer Cleanup.lnk",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\\Programs\\Rebound 11 Tools\\Rebound 11 Quick Full Computer Cleanup.lnk",
            "Rebound 11 Quick Full Computer Cleanup",
            "Rebound 11 Quick Full Computer Cleanup.lnk");

        Debug.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));

        // PowerShell command to copy the file
        /*var powerShellCommand2 = $@"
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
        var startInfo2 = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellCommand2}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process())
        {
            process.StartInfo = startInfo2;
            process.Start();

            // Capture the output
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }*/
    }

    public static void RunPowerShellCommand(string shortcutLocation, string targetPath)
    {

    }

    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
        if (File.Exists(@"C:\Rebound11\QuickFullComputerCleanup.exe"))
        {
            _ = Process.Start(@"C:\Rebound11\QuickFullComputerCleanup.exe");
        }
    }

    private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args) => GetCurrentSecurityIndex();

    public async Task<Task> InstallExeWithShortcut(string displayAppName, string exeFile, string exeDestination, string lnkFile, string lnkDestination, string exeDisplayName, string lnkDisplayName)
    {
        try
        {
            File.Copy(exeFile, exeDestination, true);
        }
        catch
        {

        }
        await Task.Delay(50);

        try
        {
            File.Copy(lnkFile, lnkDestination, true);
        }
        catch
        {

        }

        await Task.Delay(50);
        return Task.CompletedTask;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
}
