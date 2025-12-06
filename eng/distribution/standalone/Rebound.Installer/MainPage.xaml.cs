using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReboundHubInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Rebound.Installer;

public sealed class BoolUniversalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool? input = value as bool?;
        if (value is bool b) input = b;

        if (input == null)
            return DependencyProperty.UnsetValue;

        bool result = input.Value;

        // Parameter-based inversion
        if (parameter is string p &&
            p.IndexOf("invert", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            result = !result;
        }

        // Output type check: bool or Visibility
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return result;
        }

        if (targetType == typeof(Visibility))
        {
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        // Fallback: let the binding engine handle mismatch
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // ConvertBack only meaningful for bool or Visibility ? bool

        bool? result = null;

        if (value is bool b)
        {
            result = b;
        }
        else if (value is Visibility vis)
        {
            result = vis == Visibility.Visible;
        }

        if (result == null)
            return DependencyProperty.UnsetValue;

        // Apply inversion if requested
        if (parameter is string p &&
            p.IndexOf("invert", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            result = !result.Value;
        }

        return result;
    }
}

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    [RelayCommand]
    public void CloseApp() => App.MainWindow
        .Close();

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
                    Verb = "runas"
                });
            }
            catch
            {

            }
        }

        App.MainWindow.Close();
    }

    [RelayCommand]
    public async Task BeginAsync()
    {
        await Task.Delay(500); // Optional visual delay

        Panel1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        Panel2.Opacity = 1;

        await Task.Delay(500); // Optional visual delay

        /*if (InstallButton.IsChecked == true)
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
        }*/

        await Task.Delay(500); // Optional visual delay

        Panel2.Opacity = 0;
        Panel3.Visibility = Windows.UI.Xaml.Visibility.Visible;
    }
}
