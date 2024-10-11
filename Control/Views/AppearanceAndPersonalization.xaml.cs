using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

namespace Rebound.Control.Views;

public sealed partial class AppearanceAndPersonalization : Page
{
    public AppearanceAndPersonalization()
    {
        this?.InitializeComponent();
        App.cpanelWin?.TitleBarEx.SetWindowIcon("AppRT\\Exported\\imageres_197.ico");
        if (App.cpanelWin != null)
        {
            App.cpanelWin.Title = "Appearance and Personalization";
        }
    }

    private async void ApplyThemeClick(object sender, RoutedEventArgs e)
    {
        bool exists;
        try
        {
            exists = Process.GetProcessesByName("SystemSettings")[0] != null;
        }
        catch
        {
            exists = false;
        }
        _ = PleaseWaitDialog.ShowAsync();
        App.cpanelWin.IsAlwaysOnTop = true;
        var filePath = $"{AppContext.BaseDirectory}\\Themes\\{((FrameworkElement)sender).Tag}.deskthemepack";
        Process.Start(new ProcessStartInfo()
        {
            FileName = $"{filePath}",
            UseShellExecute = true
        });
        await Task.Delay(1000);
        await Task.Delay(800);
        App.cpanelWin.IsAlwaysOnTop = false;
        await Task.Delay(600);
        if (!exists)
        {
            Process.GetProcessesByName("SystemSettings")[0].Kill();
        }
        await Task.Delay(200);
        App.cpanelWin.BringToFront();
        PleaseWaitDialog.Hide();
    }

    private static void OpenFileExplorerOptions()
    {
        // Constants for ShellExecute
        const int SW_SHOWNORMAL = 1;

        // Call ShellExecute to open the File Explorer Options dialog
        ShellExecute(IntPtr.Zero, "open", "control.exe", "/name Microsoft.FolderOptions", null, SW_SHOWNORMAL);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr ShellExecute(IntPtr hWnd, string lpOperation, string lpFile, string lpParameters, string? lpDirectory, int nShowCmd);

    private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if ((NavigationViewItem)sender.SelectedItem == TBAndNav)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:taskbar"));
        }
        if ((NavigationViewItem)sender.SelectedItem == Access)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess"));
        }
        if ((NavigationViewItem)sender.SelectedItem == ExpOptions)
        {
            OpenFileExplorerOptions();
        }
        if ((NavigationViewItem)sender.SelectedItem == Fonts)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:fonts"));
        }
        sender.SelectedItem = Rebound11Item;
    }
}
