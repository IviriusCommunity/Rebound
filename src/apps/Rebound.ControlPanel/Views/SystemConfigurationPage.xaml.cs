// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Rebound.ControlPanel.Brushes;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.UI;
using Rebound.Core.UI.Windowing;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

internal sealed partial class SystemConfigurationPage : Page
{
    public SystemConfigurationViewModel ViewModel = new();

    public SystemConfigurationPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public static async Task LaunchDeviceManagerAsync()
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
            await ReboundDialog.ShowAsync(
                "Rebound Control Panel",
                "Couldn't launch Device Manager.",
                ex.Message,
                null,
                DialogIcon.Error).ConfigureAwait(false);
        }
    }
}