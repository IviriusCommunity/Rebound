// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.ControlPanel.ViewModels;

internal partial class DisplayColorCalibrationViewModel() : ObservableObject
{
    [ObservableProperty] public partial int SelectedPageIndex { get; set; }
    [ObservableProperty] public partial bool IsSoftwareCalibration { get; set; }
    [ObservableProperty] public partial bool CanNavigateBack { get; set; }
    [ObservableProperty] public partial string Name { get; set; } = "Rebound SDR Calibration";
    [ObservableProperty] public partial string Description { get; set; } = "sRGB display profile with display hardware configuration data derived from calibration, done with Rebound Control Panel - Display Color Calibration";

    partial void OnSelectedPageIndexChanged(int oldValue, int newValue)
    {
        // Skip brightness and contrast calibration
        if (oldValue == 2 && newValue == 3 && IsSoftwareCalibration)
            SelectedPageIndex = 7;

        if (oldValue == 7 && newValue == 6 && IsSoftwareCalibration)
            SelectedPageIndex = 2;
    }
}