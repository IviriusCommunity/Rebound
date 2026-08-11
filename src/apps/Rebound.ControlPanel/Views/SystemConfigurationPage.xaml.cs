// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.Views;

internal sealed partial class SystemConfigurationPage : Page
{
    public SystemConfigurationViewModel ViewModel { get; }

    public SystemConfigurationPage()
    {
        InitializeComponent();
        ViewModel = new SystemConfigurationViewModel();
    }

    [RelayCommand]
    public async Task ChangeComputerNameAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Change computer name",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var sp = new StackPanel() { Spacing = 8 };

        var ib = new InfoBar()
        {
            Title = "Only letters, numbers and hyphens allowed, max 15 characters. Cannot start or end with a hyphen.",
            IsClosable = false,
            IsOpen = true,
            MaxWidth = 400
        };
        sp.Children.Add(ib);

        var tb = new TextBox()
        {
            Text = ViewModel.ComputerName,
            PlaceholderText = "Computer name",
            MaxLength = 15
        };
        sp.Children.Add(tb);

        var err = new TextBlock()
        {
            Text = "Invalid",
            Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"],
            Visibility = Visibility.Collapsed
        };
        sp.Children.Add(err);

        tb.TextChanged += (s, e) => Validate();

        cd.Content = sp;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.ComputerName = tb.Text;

        void Validate()
        {
            var valid = WindowsInformation.IsValidComputerName(tb.Text);
            cd.IsPrimaryButtonEnabled = valid;
            err.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    [RelayCommand]
    public async Task ChangeComputerDescriptionAsync()
    {
        var cd = new ContentDialog()
        {
            Title = "Change computer description",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var tb = new TextBox()
        {
            Text = ViewModel.ComputerDescription,
            PlaceholderText = "Computer description",
            MaxLength = 255
        };
        cd.Content = tb;

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.ComputerDescription = tb.Text;
    }

    [RelayCommand]
    public async Task LaunchDeviceManagerAsync()
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "devmgmt.msc",
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch Device Manager.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }

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
                Arguments = CplArgs.SystemPropertiesComputerNameExePath
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
}