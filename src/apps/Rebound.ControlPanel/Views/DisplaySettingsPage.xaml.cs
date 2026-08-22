// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.ControlPanel.Windows;
using WinUIEx;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DisplaySettingsPage : Page
{
    private DisplayViewModel ViewModel { get; } = new();

    public DisplaySettingsPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            await ViewModel.HDRCalibration.UpdateIntegrityAsync().ConfigureAwait(false);
        };
    }

    [RelayCommand]
    public static void BeginDisplayColorCalibration()
    {
        var colorCalibrationWindow = new DisplayColorCalibrationWindow();
        colorCalibrationWindow.Show();
    }

    [RelayCommand]
    private static void LaunchLegacy(string exe)
        => ((App)Application.Current).LaunchLegacy(exe, string.Empty);
}