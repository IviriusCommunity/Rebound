// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.About.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace Rebound.About.Views;

public sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        InitializeComponent();
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

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }
}