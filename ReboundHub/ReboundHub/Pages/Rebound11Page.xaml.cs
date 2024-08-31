using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundHub.ReboundHub.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Rebound11Page : Page
{
    public Rebound11Page()
    {
        this.InitializeComponent();
        if (IsAdmin() == true)
        {
            Admin1.Visibility = Visibility.Collapsed;
            Admin3.Visibility = Visibility.Collapsed;
            Admin5.Visibility = Visibility.Collapsed;
        }
        if (IsReboundInstalled() == true)
            {
                Rebound11IsInstalledGrid.Visibility = Visibility.Visible;
                Rebound11IsInstallingGrid.Visibility = Visibility.Collapsed;
                Rebound11IsNotInstalledGrid.Visibility = Visibility.Collapsed;
                DetailsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                Rebound11IsInstalledGrid.Visibility = Visibility.Collapsed;
                Rebound11IsInstallingGrid.Visibility = Visibility.Collapsed;
                Rebound11IsNotInstalledGrid.Visibility = Visibility.Visible;
                DetailsPanel.Visibility = Visibility.Visible;
            }
        GetWallpaper();
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("INSTALLREBOUND11"))
        {
            Rebound11IsInstalledGrid.Visibility = Visibility.Collapsed;
            Rebound11IsInstallingGrid.Visibility = Visibility.Visible;
            Rebound11IsNotInstalledGrid.Visibility = Visibility.Collapsed;
            DetailsPanel.Visibility = Visibility.Visible;
        }
        CheckForUpdatesAsync();
    }

    public async void GetWallpaper()
    {
        try
        {
            if (this.ActualTheme == ElementTheme.Light)
            {
                BKGImage.Path = "/Assets/Backgrounds/BackgroundLight.png";
            }
            if (this.ActualTheme == ElementTheme.Dark)
            {
                BKGImage.Path = "/Assets/Backgrounds/BackgroundDark.png";
            }
            await Task.Delay(100);
            GetWallpaper();
        }
        catch
        {
        
        }
    }

    public bool IsAdmin()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool IsReboundInstalled()
    {
        return Directory.Exists("C:\\Rebound11");
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var win = new InstallationWindow((bool)FilesCheck.IsChecked, (bool)RunCheck.IsChecked, (bool)DefragCheck.IsChecked, (bool)WinverCheck.IsChecked, (bool)UACCheck.IsChecked, (bool)OSKCheck.IsChecked, (bool)TPMCheck.IsChecked, (bool)DiskCleanupCheck.IsChecked);
        win.Show();
    }

    private async Task CheckForUpdatesAsync()
    {
        // URL of the text file containing the latest version number
        string versionUrl = "https://ivirius.vercel.app/reboundhubversion.txt";

        // Use HttpClient to fetch the content
        using HttpClient client = new HttpClient();

        try
        {
            // Fetch the version string from the URL
            string latestVersion = await client.GetStringAsync(versionUrl);

            // Trim any excess whitespace/newlines from the fetched string
            latestVersion = latestVersion.Trim();

            // Get the current app version
            string currentVersion = "v0.0.2 ALPHA";

            // Compare versions
            if (latestVersion == currentVersion)
            {
                // The app is up-to-date
                UpdateBar.IsOpen = false;
            }
            else
            {
                // A new version is available
                UpdateBar.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            // Handle any errors that occur during the request
            UpdateBar.IsOpen = true;
            UpdateBar.Severity = InfoBarSeverity.Error;
            UpdateBar.Title = "Something went wrong.";
        }
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas",
            Arguments = @$"Start-Process ""shell:AppsFolder\d6ef5e04-e9da-4e22-9782-8031af8beae7_yejd587sfa94t!App"" -ArgumentList ""INSTALLREBOUND11"" -Verb RunAs"
        };
        var process = Process.Start(info);

        // Wait for the process to exit before proceeding
        await process.WaitForExitAsync();
        App.m_window.Close();
    }

    private async void Button_Click_2(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas",
            Arguments = @$"Start-Process ""shell:AppsFolder\d6ef5e04-e9da-4e22-9782-8031af8beae7_yejd587sfa94t!App"" -ArgumentList ""UNINSTALL"" -Verb RunAs"
        };
        var process = Process.Start(info);

        // Wait for the process to exit before proceeding
        await process.WaitForExitAsync();
        App.m_window.Close();
    }
}
