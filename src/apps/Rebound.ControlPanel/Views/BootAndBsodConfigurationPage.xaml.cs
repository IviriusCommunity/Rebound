// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace Rebound.ControlPanel.Views;

internal sealed partial class BootAndBsodConfigurationPage : Page
{
    BootAndBsodConfigurationViewModel ViewModel { get; set; } = new();

    public BootAndBsodConfigurationPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private static async Task ManageUsersAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:otherusers"));

    [RelayCommand]
    private static async Task OpenBootAndRecoveryAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:recovery"));

    [RelayCommand]
    private static async Task OpenSystemProtectionAsync()
        => await Launcher.LaunchUriAsync(new Uri("windowsdefender:systemprotection"));

    [RelayCommand]
    public async Task RelaunchAsAdminAsync()
    {
        try
        {
            App.SingleInstanceAppService.Relaunch(new InstanceRelaunchOptions
            {
                Elevated = true,
                ShutdownCurrent = true,
                ForceNewInstance = true,
                Arguments = CplArgs.BOOT_AND_BSOD_CONFIGURATION
            });
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch Rebound Control Panel as administrator.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task OpenServicesAsync()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "services.msc",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch Services.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    public async Task SelectDumpDirAsync()
    {
        var dialog = new FolderPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(dialog, hwnd);

        var result = await dialog.PickSingleFolderAsync();
        if (result != null)
            ViewModel.DumpDirectory = result.Path;
    }

    [RelayCommand]
    private static async Task OpenStartupAppsAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps"));
}