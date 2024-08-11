using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
        var col = (Color)Application.Current.Resources["SystemAccentColor"];
        MainGrid.Resources["AccentColor"] = new Color()
        {
            A = 0,
            R = col.R,
            G = col.G,
            B = col.B
        };
        MainGrid.Resources["TransparentAccentColor"] = new Color()
        {
            A = 125,
            R = col.R,
            G = col.G,
            B = col.B
        };
        if (IsAdmin() == true)
        {
            Admin1.Visibility = Visibility.Collapsed;
            Admin2.Visibility = Visibility.Collapsed;
            if (IsReboundInstalled() == true)
            {
                ReboundNotInstalledInfoBar.Visibility = Visibility.Collapsed;
                ReboundInstallInfoBar.Visibility = Visibility.Collapsed;
                ReboundUninstallInfoBar.Visibility = Visibility.Visible;
            }
            else
            {
                ReboundNotInstalledInfoBar.Visibility = Visibility.Collapsed;
                ReboundInstallInfoBar.Visibility = Visibility.Visible;
                ReboundUninstallInfoBar.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            if (IsReboundInstalled() == true)
            {
                ReboundNotInstalledInfoBar.Visibility = Visibility.Collapsed;
                ReboundInstallInfoBar.Visibility = Visibility.Collapsed;
                ReboundUninstallInfoBar.Visibility = Visibility.Visible;
            }
            else
            {
                ReboundNotInstalledInfoBar.Visibility = Visibility.Visible;
                ReboundInstallInfoBar.Visibility = Visibility.Collapsed;
                ReboundUninstallInfoBar.Visibility = Visibility.Collapsed;
                InstallRebound.IsEnabled = false;
                InstallRebound.Content = "You must run Rebound Hub as Administrator to install Rebound 11.";
            }
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
        var win = new InstallationWindow((bool)FilesCheck.IsChecked, (bool)RunCheck.IsChecked, (bool)DefragCheck.IsChecked, (bool)WinverCheck.IsChecked, (bool)UACCheck.IsChecked);
        win.Show();
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {

    }
}
