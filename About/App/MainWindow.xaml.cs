using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;

#nullable enable

namespace Rebound.About;
public sealed partial class MainWindow : WindowEx
{
    private MainViewModel ViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = new MainViewModel();
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        IsMaximizable = false;
        IsMinimizable = false;
        MinWidth = 650;
        this.MoveAndResize(25, 25, 650, 690);
        Title = "About Windows";
        IsResizable = false;
        SystemBackdrop = new MicaBackdrop();
        this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Rebound.ico");
        User.Text = GetCurrentUserName();
        LegalStuff.Text = ViewModel.GetInformation();
        Load();
    }

    public async void Load()
    {
        await Task.Delay(100);

        this.SetWindowSize(WinverPanel.ActualWidth + 60, 690);
    }

    public static string GetCurrentUserName()
    {
        try
        {
            // Open the registry key
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                // Retrieve current username
                var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
                var owner2 = key.GetValue("RegisteredOrganization", "Unknown") as string;

                return owner + "\n" + owner2;
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving OS version details: {ex.Message}";
        }

        return "Registry key not found";
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "winver",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var proc = Process.Start(info);

        Close();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e) => Close();
}
