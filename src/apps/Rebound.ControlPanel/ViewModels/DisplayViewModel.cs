// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Rebound.ControlPanel.ViewModels;

internal partial class DisplayViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SelectedPage { get; set; } = "0";

    [ObservableProperty]
    public partial double Gamma { get; set; } = 1;

    [ObservableProperty]
    public partial double Brightness { get; set; } = 0;

    [ObservableProperty]
    public partial double Contrast { get; set; } = 1;

    [ObservableProperty]
    public partial bool DoSoftwareCalibration { get; set; }

    [ObservableProperty]
    public partial bool InvalidCombination { get; set; }

    [ObservableProperty]
    public partial bool IsExpandedLayout { get; set; }

    [ObservableProperty]
    public partial string ProfileName { get; set; } = "Rebound SDR Calibration";

    [ObservableProperty]
    public partial string ProfileDescription { get; set; } = "sRGB display profile with display hardware configuration data derived from calibration, done with Rebound Control Panel - Display Color Calibration";

    public DisplayViewModel()
    {
        PropertyChanged += DisplayViewModel_PropertyChanged;
    }

    private void DisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidCombination = !IsValidCombination(Gamma, Brightness, Contrast);
    }

    [RelayCommand]
    public void BeginSoftwareCalibration()
    {
        DoSoftwareCalibration = true;
        ResetToDefault();
        SelectedPage = "1";
    }

    [RelayCommand]
    public void BeginHardwareCalibration()
    {
        DoSoftwareCalibration = false;
        ResetToDefault();
        SelectedPage = "1";
    }

    private void ResetToDefault()
    {
        Gamma = 1;
        Brightness = 0;
        Contrast = 1;
        ProfileName = "Rebound SDR Calibration";
        ProfileDescription = "sRGB display profile with display hardware configuration data derived from calibration, done with Rebound Control Panel - Display Color Calibration";
    }

    private static bool IsValidCombination(double gamma, double brightness, double contrast)
    {
        // Max output at input=1.0 must not exceed 1.0
        var maxOutput = contrast * Math.Pow(1.0, 1.0 / gamma) + brightness;
        // Min output at input=0.0 must not go below 0.0
        var minOutput = brightness; // pow(0, anything) = 0
        return maxOutput <= 1.0 && minOutput >= 0.0;
    }
}
