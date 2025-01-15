using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rebound.Cleanup.Views;
using WinUIEx;

namespace Rebound.Cleanup;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        this.SetWindowSize(350, 200);
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;
        this.CenterOnScreen();
        Title = "Disk Cleanup : Drive Selection";
        SystemBackdrop = new MicaBackdrop();
        this.SetIcon($@"{AppContext.BaseDirectory}\Assets\cleanmgr.ico");
        var x = Directory.GetLogicalDrives();
        foreach (var i in x)
        {
            DrivesBox.Items.Add(i[..2]);
        }
        DrivesBox.SelectedIndex = 0;
        RootFrame.Navigate(typeof(DriveSelectionPage));
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        Working.IsIndeterminate = true;
        OkButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        await Task.Delay(50);

        await OpenWindow(DrivesBox.SelectedItem.ToString());

        Close();
    }

    public async Task ArgumentsLaunch(string disk)
    {
        Working.IsIndeterminate = true;
        OkButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        await Task.Delay(50);

        await OpenWindow(disk);

        Close();
    }

    public Task OpenWindow(string disk)
    {
        Title = "Disk Cleanup : Calculating total cache size... (This may take a while)";
        var win = new DiskWindow(disk);
        win.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        win.SetWindowSize(450, 640);
        win.IsMaximizable = false;
        win.IsMinimizable = false;
        win.IsResizable = false;
        win.Move(50, 50);
        win.Title = $"Disk Cleanup for ({disk})";
        win.SystemBackdrop = new MicaBackdrop();
        win.SetIcon($@"{AppContext.BaseDirectory}\Assets\cleanmgr.ico");
        _ = win.Show();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void CloseWindow()
    {

    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Process.GetCurrentProcess().Kill();
}
