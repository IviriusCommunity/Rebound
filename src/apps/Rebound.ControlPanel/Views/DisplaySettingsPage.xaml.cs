// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Rebound.ControlPanel.Brushes;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.ICC.Curves;
using Rebound.Core.ICC.Display;
using Rebound.Core.ICC.Profiles;
using Rebound.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using WinRT.Interop;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DisplaySettingsPage : Page
{
    private SDRCalibrationBackdropBrush _brush;

    private DisplayViewModel ViewModel { get; } = new();

    public DisplaySettingsPage()
    {
        InitializeComponent();
        _brush = new SDRCalibrationBackdropBrush(this.GetVisual().Compositor);
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

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "New Calibration"
            };
            picker.FileTypeChoices.Add("ICC Profile", new List<string> { ".icc", ".icm" });
            InitializeWithWindow.Initialize(picker, App.MainWindow!.Handle);
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            var path = file.Path;
            await Task.Run(() =>
            {
                File.WriteAllBytes(path, bytes);
            });
        }

        UIThreadQueue.QueueAction(() => ViewModel.SelectedPage = "0");
    }

    private void StackPanel_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
    {
        ViewModel.IsExpandedLayout = e.NewSize.Width > 560;
    }
}
