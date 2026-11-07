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

using WinRT.Interop;

using WinUIEx;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DirectXPage : Page
{
    private DirectXViewModel ViewModel { get; } = new();

    public DirectXPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public async Task AddD3DScopeAppFromFilesAsync()
    {
        var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add(".com");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();

        if (file is not null)
            ViewModel.AddD3DScopeAppImpl(file.Path);
    }

    [RelayCommand]
    public async Task AddD2DScopeAppFromFilesAsync()
    {
        var picker = new FileOpenPicker(App.MainWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add(".com");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();

        if (file is not null)
            ViewModel.AddD2DScopeAppImpl(file.Path);
    }

    [RelayCommand]
    public async Task AddD3DScopeAppAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Add Direct3D Scope",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = XamlRoot
        };

        var tb = new TextBox()
        {
            PlaceholderText = "File path"
        };
        tb.TextChanged += (s, e) =>
        {
            cd.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(tb.Text);
        };
        cd.Content = tb;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.AddD3DScopeAppImpl(tb.Text);
    }

    [RelayCommand]
    public async Task AddMutedMessageIdAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Add Muted Message ID",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = XamlRoot
        };

        var tb = new TextBox()
        {
            PlaceholderText = "Muted Message ID"
        };
        tb.TextChanged += (s, e) =>
        {
            cd.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(tb.Text);
        };
        cd.Content = tb;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.AddMutedMessageIdImpl(tb.Text);
    }

    [RelayCommand]
    public async Task AddBreakMessageIdAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Add Break Message ID",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = XamlRoot
        };

        var tb = new TextBox()
        {
            PlaceholderText = "Break Message ID"
        };
        tb.TextChanged += (s, e) =>
        {
            cd.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(tb.Text);
        };
        cd.Content = tb;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.AddBreakMessageIdImpl(tb.Text);
    }

    [RelayCommand]
    public async Task AddD2DScopeAppAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Add Direct2D Scope",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            XamlRoot = XamlRoot
        };

        var tb = new TextBox()
        {
            PlaceholderText = "File path"
        };
        tb.TextChanged += (s, e) =>
        {
            cd.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(tb.Text);
        };
        cd.Content = tb;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.AddD2DScopeAppImpl(tb.Text);
    }

    #region Launchers

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
                Arguments = CplArgs.DirectXControlPanelExePath
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
    public async Task LaunchDxDiagAsync()
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "dxdiag.exe",
                Verb = "runas",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch dxdiag.exe.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }

    #endregion
}
