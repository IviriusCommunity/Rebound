// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
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
    private static void OpenBootAndRecovery()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "SystemPropertiesAdvanced.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private static void OpenSystemProtection()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "SystemPropertiesProtection.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private static void OpenServices()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "services.msc",
                UseShellExecute = true
            });
        }
        catch { }
    }

    [RelayCommand]
    private static async Task OpenStartupAppsAsync()
        => await Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps"));

    [RelayCommand]
    private static void OpenWindowsTools()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                Arguments = "/name Microsoft.AdministrativeTools",
                UseShellExecute = true
            });
        }
        catch { }
    }
}