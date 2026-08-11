// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Core.Environment;
using Rebound.Core.Native.Windows;
using Rebound.Core.SystemInformation.Software;
using Rebound.Forge;
using Rebound.Forge.Engines;

namespace Rebound.ControlPanel.ViewModels;

internal partial class SystemConfigurationViewModel : ObservableObject
{
    // Properties
    [ObservableProperty] public partial bool IsElevated { get; set; }
    [ObservableProperty] public partial bool IsRestartRequired { get; set; } = false;

    // Settings
    [ObservableProperty] public partial string ComputerName { get; set; }
    [ObservableProperty] public partial string ComputerDescription { get; set; }
    [ObservableProperty] public partial bool InstallOemApps { get; set; }

    public SystemConfigurationViewModel()
    {
        // Properties
        IsElevated = ApplicationEnvironment.IsRunningAsAdmin();

        // Settings
        ComputerName = WindowsInformation.GetComputerName();
        ComputerDescription = WindowsInformation.GetComputerDescription();

        if (IsElevated)
        {
            InstallOemApps = !RegistrySettingsEngine.GetBool(RegistryHive.LocalMachine,
                RegistrySettingsCatalog.InstallOemApps.KeyPath,
                RegistrySettingsCatalog.InstallOemApps.ValueName);
        }
    }

    partial void OnInstallOemAppsChanged(bool value) 
        => RegistrySettingsEngine.SetBool(RegistryHive.LocalMachine,
            RegistrySettingsCatalog.InstallOemApps.KeyPath,
            RegistrySettingsCatalog.InstallOemApps.ValueName, !value);

    partial void OnComputerNameChanged(string value)
    {
        if (ComputerName != WindowsInformation.GetComputerName()) // Check if there's changes
            if (WindowsInformation.IsValidComputerName(ComputerName)) // Validate
            {
                WindowsInformation.SetComputerName(ComputerName);
                IsRestartRequired = true;
            }
    }

    partial void OnComputerDescriptionChanged(string value)
    {
        if (ComputerDescription != WindowsInformation.GetComputerDescription()) // Check if there's changes
            if (WindowsInformation.IsValidComputerDescription(ComputerDescription)) // Validate
                WindowsInformation.SetComputerDescription(ComputerDescription);
    }

    [RelayCommand]
    public static void Restart()
        => Shutdown.RestartNow(true);
}
