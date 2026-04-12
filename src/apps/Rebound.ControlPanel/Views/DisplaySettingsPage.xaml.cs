// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Rebound.ControlPanel.Brushes;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.ICC.Profiles;
using Rebound.Core.Native.Storage;
using Rebound.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT.Interop;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DisplaySettingsPage : Page, IDisposable
{
    private readonly SDRCalibrationBackdropBrush _brush;

    private DisplayViewModel ViewModel { get; } = new();

    public DisplaySettingsPage()
    {
        InitializeComponent();
        _brush = new SDRCalibrationBackdropBrush();
        BrushSurface.Background = _brush;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.Gamma):
            case nameof(ViewModel.Brightness):
            case nameof(ViewModel.Contrast):
                {
                    UpdateCalibration();
                    break;
                }
        }
    }

    private void UpdateCalibration()
    {
        _brush?.UpdateCalibration(
            ViewModel.Gamma,
            ViewModel.Brightness,
            ViewModel.Contrast);
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (ViewModel.DoSoftwareCalibration)
        {
            var bytes = WcsProfile.Generate(
                ViewModel.ProfileName,
                ViewModel.ProfileDescription,
                ViewModel.Gamma,
                ViewModel.Brightness,
                ViewModel.Contrast);

            if (bytes == null) return;

            // Create the file picker
            var result = FilePickers.PickSaveFile(
                App.MainWindow!,
                "Save Calibration Profile",
                "New Calibration.icc",
                [
                    new("ICC Profile", ".icc;.icm" ),
                    new("All files", "*" )
                ]);

            if (result.IsCancelled == true) return;

            var path = result.Path;
            await Task.Run(() =>
            {
                File.WriteAllBytes(path!, bytes);
            }).ConfigureAwait(false);
        }

        UIThreadQueue.QueueAction(() => ViewModel.SelectedPage = "0");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        UIThreadQueue.QueueAction(() =>
        {
            ViewModel.SelectedPage = "0";
            ViewModel.ResetToDefault();
        });
        
    }

    [RelayCommand]
    private static void LaunchLegacy(string exe)
        => ((App)Application.Current).LaunchLegacy(exe, string.Empty);

    private void StackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        => ViewModel.IsExpandedLayout = e.NewSize.Width > 560;

    public void Dispose()
    {
        _brush.Dispose();
    }
}
