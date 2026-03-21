// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Forge;
using Rebound.Forge.Engines;

namespace Rebound.ControlPanel.ViewModels;

internal partial class DisplayViewModel : ObservableObject
{
    // Color calibration

    [ObservableProperty] public partial string SelectedPage { get; set; } = "0";
    [ObservableProperty] public partial double Gamma { get; set; } = 1;
    [ObservableProperty] public partial double Brightness { get; set; } = 0;
    [ObservableProperty] public partial double Contrast { get; set; } = 1;
    [ObservableProperty] public partial bool DoSoftwareCalibration { get; set; }
    [ObservableProperty] public partial bool InvalidCombination { get; set; }
    [ObservableProperty] public partial bool IsExpandedLayout { get; set; }
    [ObservableProperty] public partial string ProfileName { get; set; } = "Rebound SDR Calibration";
    [ObservableProperty] public partial string ProfileDescription { get; set; } = "sRGB display profile with display hardware configuration data derived from calibration, done with Rebound Control Panel - Display Color Calibration";

    // ClearType

    [ObservableProperty] public partial int FontSmoothingType { get; set; }
    [ObservableProperty] public partial int ClearTypeLevel { get; set; }
    [ObservableProperty] public partial int ClearTypeGamma { get; set; }
    [ObservableProperty] public partial int SubpixelLayout { get; set; }
    [ObservableProperty] public partial bool IsClearTypeEnabled { get; set; }

    public DisplayViewModel()
    {
        LoadClearTypeSettings();
        PropertyChanged += DisplayViewModel_PropertyChanged;
    }

    private void DisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Gamma):
            case nameof(Brightness):
            case nameof(Contrast):
                InvalidCombination = !IsValidCombination(Gamma, Brightness, Contrast);
                break;
            case nameof(FontSmoothingType):
            case nameof(ClearTypeLevel):
            case nameof(ClearTypeGamma):
            case nameof(SubpixelLayout):
                ApplyClearTypeSettings();
                break;
        }
    }

    // Color calibration

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

    public void ResetToDefault()
    {
        Gamma = 1;
        Brightness = 0;
        Contrast = 1;
        ProfileName = "Rebound SDR Calibration";
        ProfileDescription = "sRGB display profile with display hardware configuration data derived from calibration, done with Rebound Control Panel - Display Color Calibration";
    }

    private static bool IsValidCombination(double gamma, double brightness, double contrast)
    {
        // There's some kind of validation algorithm and idk what it is exactly
        return true;
    }

    // ClearType

    private void LoadClearTypeSettings()
    {
        var smoothingEnabled = RegistrySettingsEngine.GetValue<string>(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothing.KeyPath,
            RegistrySettingsCatalog.FontSmoothing.ValueName,
            "2");

        var smoothingType = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingType.KeyPath,
            RegistrySettingsCatalog.FontSmoothingType.ValueName,
            2);

        FontSmoothingType = smoothingEnabled == "0" ? 0 : smoothingType;

        ClearTypeLevel = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonClearTypeLevel.KeyPath,
            RegistrySettingsCatalog.AvalonClearTypeLevel.ValueName,
            100);

        ClearTypeGamma = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonGammaLevel.KeyPath,
            RegistrySettingsCatalog.AvalonGammaLevel.ValueName,
            1400);

        SubpixelLayout = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingOrientation.KeyPath,
            RegistrySettingsCatalog.FontSmoothingOrientation.ValueName,
            0);

        IsClearTypeEnabled = FontSmoothingType == 2;
    }

    public unsafe void ApplyClearTypeSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonClearTypeLevel.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothing.KeyPath,
            RegistrySettingsCatalog.FontSmoothing.ValueName,
            FontSmoothingType == 0 ? "0" : "2",
            RegistryValueKind.String);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingType.KeyPath,
            RegistrySettingsCatalog.FontSmoothingType.ValueName,
            FontSmoothingType == 0 ? 1 : FontSmoothingType,
            RegistryValueKind.DWord);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingGamma.KeyPath,
            RegistrySettingsCatalog.FontSmoothingGamma.ValueName,
            ClearTypeGamma,
            RegistryValueKind.DWord);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingOrientation.KeyPath,
            RegistrySettingsCatalog.FontSmoothingOrientation.ValueName,
            SubpixelLayout,
            RegistryValueKind.DWord);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonClearTypeLevel.KeyPath,
            RegistrySettingsCatalog.AvalonClearTypeLevel.ValueName,
            ClearTypeLevel,
            RegistryValueKind.DWord);

        RegistrySettingsEngine.SetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonGammaLevel.KeyPath,
            RegistrySettingsCatalog.AvalonGammaLevel.ValueName,
            ClearTypeGamma,
            RegistryValueKind.DWord);

        IsClearTypeEnabled = FontSmoothingType == 2;

        // Needed to reload fonts for all Win32 apps
        unsafe
        {
            TerraFX.Interop.Windows.Windows.SystemParametersInfoW(
                0x004A,
                FontSmoothingType != 0 ? 1u : 0u,
                null,
                0x0003);
        }
    }
}
