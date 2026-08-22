// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Core.Native.Wrappers;
using Rebound.Forge;
using Rebound.Forge.Cogs;
using Rebound.Forge.Engines;
using Rebound.Forge.Launchers;
using System;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Rebound.ControlPanel.ViewModels;

internal partial class DisplayViewModel : ObservableObject
{
    [ObservableProperty] public partial int FontSmoothingType { get; set; }
    [ObservableProperty] public partial int ClearTypeLevel { get; set; }
    [ObservableProperty] public partial int ClearTypeGamma { get; set; }
    [ObservableProperty] public partial int SubpixelLayout { get; set; }
    [ObservableProperty] public partial bool IsClearTypeEnabled { get; set; }
    [ObservableProperty] public partial bool IsRefreshRequired { get; set; }

    public DisplayViewModel()
    {
        LoadClearTypeSettings();
        PropertyChanged += DisplayViewModel_PropertyChanged;
    }

    public Mod HDRCalibration = new()
    {
        Name = "Windows HDR Calibration",
        Id = new Guid("a8357b0d-d30b-4ef4-8f2d-55adf9094e69"),
        Variants =
        [
            new ModVariant
            {
                Name = "Windows HDR Calibration",
                Id = new Guid("e1bf060c-4288-4fe0-b682-342479f05635"),
                Launchers = 
                [
                    new PackageLauncher()
                    {
                        PackageFamilyName = "MicrosoftCorporationII.WindowsHDRCalibration_8wekyb3d8bbwe"
                    }
                ],
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Windows HDR Calibration",
                        CogId = new Guid("0f732795-39d2-4d92-b054-5c44e20f9822"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9N7F2SM5D1LR",
                            PackageFamilyName: "MicrosoftCorporationII.WindowsHDRCalibration_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    private void DisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FontSmoothingType):
            case nameof(ClearTypeLevel):
            case nameof(ClearTypeGamma):
            case nameof(SubpixelLayout):
                ApplyClearTypeSettings();
                break;
        }
    }

    private void LoadClearTypeSettings()
    {
        var smoothingEnabled = RegistrySettingsEngine.GetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothing.KeyPath,
            RegistrySettingsCatalog.FontSmoothing.ValueName,
            "2");

        var smoothingType = RegistrySettingsEngine.GetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingType.KeyPath,
            RegistrySettingsCatalog.FontSmoothingType.ValueName,
            2);

        FontSmoothingType = smoothingEnabled == "0" ? 0 : smoothingType;

        ClearTypeLevel = RegistrySettingsEngine.GetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonClearTypeLevel.KeyPath,
            RegistrySettingsCatalog.AvalonClearTypeLevel.ValueName,
            100);

        ClearTypeGamma = RegistrySettingsEngine.GetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AvalonGammaLevel.KeyPath,
            RegistrySettingsCatalog.AvalonGammaLevel.ValueName,
            1400);

        SubpixelLayout = RegistrySettingsEngine.GetValue(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.FontSmoothingOrientation.KeyPath,
            RegistrySettingsCatalog.FontSmoothingOrientation.ValueName,
            0);

        IsClearTypeEnabled = FontSmoothingType == 2;
    }

    public void ApplyClearTypeSettings()
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
        IsRefreshRequired = true;
    }

    [RelayCommand]
    public void Refresh()
    {
        IsRefreshRequired = false;

        _ = Task.Run(() =>
        {
            unsafe
            {
                try
                {
                    // Update system parameters
                    TerraFX.Interop.Windows.Windows.SystemParametersInfoW(
                        SPI.SPI_SETFONTSMOOTHING,
                        FontSmoothingType != 0 ? 1u : 0u,
                        null,
                        TerraFX.Interop.Windows.Windows.SPIF_UPDATEINIFILE);

                    // ClearType / Gamma parameters require SPI_SETFONTSMOOTHINGORIENTATION
                    TerraFX.Interop.Windows.Windows.SystemParametersInfoW(
                        SPI.SPI_SETFONTSMOOTHINGORIENTATION,
                        (uint)SubpixelLayout,
                        null,
                        TerraFX.Interop.Windows.Windows.SPIF_UPDATEINIFILE);

                    var pDesktop = NativeString.Alloc("Control Panel\\Desktop");

                    // Send WM_SETTINGCHANGE
                    TerraFX.Interop.Windows.Windows.SendMessageW(
                        HWND.HWND_BROADCAST,
                        WM.WM_SETTINGCHANGE,
                        (WPARAM)SPI.SPI_SETFONTSMOOTHING,
                        (LPARAM)pDesktop.Pointer);
                }
                catch
                {

                }
            }
        });
    }
}
