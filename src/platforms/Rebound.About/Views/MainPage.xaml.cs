// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Rebound.About.ViewModels;
using Rebound.Forge;
using Rebound.Helpers;
using Rebound.Helpers.Environment;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;

namespace Rebound.About.Views;

public sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        Load();
    }

    public async void Load()
    {
        await Task.Delay(100);
        App.MainAppWindow.Width = ViewModel.IsSidebarOn ? 720 : 520;
        App.MainAppWindow.Height = ViewModel.IsReboundOn ? 640 : 500;
        if (SettingsHelper.GetValue("FetchMode", "rebound", false))
        {
            App.MainAppWindow.SetWindowSize(850, 480);
            FetchArea.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            var accentBrush = (SolidColorBrush)App.Current.Resources["AccentFillColorDefaultBrush"];
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = ViewModel.CurrentUser + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = new string('=', ViewModel.CurrentUser.Length) + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "OS: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.WindowsVersionName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "Windows Version: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.DetailedWindowsVersion + "\n"
            });
            if (ViewModel.IsReboundOn)
            {
                FetchTextBlock.Inlines.Add(new Run()
                {
                    Foreground = accentBrush,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Text = "Rebound Version: "
                });
                FetchTextBlock.Inlines.Add(new Run()
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    Text = ReboundVersion.REBOUND_VERSION + "\n"
                });
            }
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "Resolution: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = DisplayArea.GetFromWindowId(App.MainAppWindow.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea.Width + "x" + DisplayArea.GetFromWindowId(App.MainAppWindow.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea.Height + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "CPU: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.CPUName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "GPU: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.GPUName + "\n"
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = accentBrush,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "RAM: "
            });
            FetchTextBlock.Inlines.Add(new Run()
            {
                Foreground = new SolidColorBrush(Colors.White),
                Text = ViewModel.RAM + "\n"
            });
        }
    }

    [RelayCommand]
    private void CopyWindowsVersion() => CopyToClipboard(ViewModel.DetailedWindowsVersion);

    [RelayCommand]
    private void CopyLicenseOwners() => CopyToClipboard(ViewModel.LicenseOwners);

    [RelayCommand]
    private static void CopyReboundVersion() => CopyToClipboard(Helpers.Environment.ReboundVersion.REBOUND_VERSION);

    [RelayCommand]
    private void CloseWindow() => App.MainAppWindow.Close();

    [RelayCommand]
    public async Task ToggleSidebarAsync()
    {
        await Task.Delay(50);
        if (ViewModel.IsSidebarOn)
        {
            for (var i = 0; i <= 100; i += 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Width = 520 + 200 * Math.Sin(radians);
            }
            App.MainAppWindow.Width = 720;
        }
        else
        {
            for (var i = 100; i >= 0; i -= 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Width = 520 + 200 * Math.Sin(radians);
            }
            App.MainAppWindow.Width = 520;
        }
    }

    [RelayCommand]
    public async Task ToggleReboundAsync()
    {
        await Task.Delay(50);
        if (ViewModel.IsReboundOn)
        {
            for (var i = 0; i <= 100; i += 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Height = 500 + 140 * Math.Sin(radians);
            }
            App.MainAppWindow.Height = 640;
        }
        else
        {
            for (var i = 100; i >= 0; i -= 3)
            {
                await Task.Delay(2);
                var radians = i * Math.PI / 180; // Convert degrees to radians
                App.MainAppWindow.Height = 500 + 140 * Math.Sin(radians);
            }
            App.MainAppWindow.Height = 500;
        }
    }

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }

    private void TextBlock_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }
}