using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReboundHubInstaller;

namespace Rebound.Installer;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    [RelayCommand]
    public void CloseApp() => App.MainAppWindow.Close();

    [RelayCommand]
    public void Finish()
    {
        if (LaunchHubCheckBox.IsChecked == true)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub", "Rebound Hub.exe"),
                    UseShellExecute = true,
                    WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"),
                    Verb = "runas"
                });
            }
            catch
            {

            }
        }

        App.MainAppWindow.Close();
    }

    [RelayCommand]
    public async Task BeginAsync()
    {
        await Task.Delay(500); // Optional visual delay

        Panel1.Visibility = Visibility.Collapsed;
        Panel2.Opacity = 1;

        await Task.Delay(500); // Optional visual delay

        if (InstallButton.IsChecked == true)
        {
            await ViewModel.InstallAsync(false);
        }
        else if (RepairButton.IsChecked == true)
        {
            await ViewModel.InstallAsync(true);
        }
        else if (UninstallButton.IsChecked == true)
        {
            await ViewModel.RemoveAsync();
        }

        await Task.Delay(500); // Optional visual delay

        Panel2.Opacity = 0;
        Panel3.Visibility = Visibility.Visible;
    }
}
