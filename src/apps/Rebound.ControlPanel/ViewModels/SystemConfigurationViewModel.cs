// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Core.Native.Windows;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI.Application;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System.Diagnostics;

namespace Rebound.ControlPanel.ViewModels;

internal partial class SystemConfigurationViewModel : ObservableObject
{
    // Properties
    [ObservableProperty] public partial bool IsElevated { get; set; }
    [ObservableProperty] public partial bool IsRestartRequired { get; set; } = false;
    [ObservableProperty] public partial bool IsComputerNameError { get; set; } = false;
    [ObservableProperty] public partial bool AreChangesPending { get; set; } = false;

    // Settings
    [ObservableProperty] public partial string ComputerName { get; set; }
    [ObservableProperty] public partial string ComputerDescription { get; set; }
    [ObservableProperty] public partial bool InstallOemApps { get; set; }

    private bool _isInitialized;

    public SystemConfigurationViewModel()
    {
        // Properties
        IsElevated = false;//AppHelper.IsRunningAsAdmin();

        // Settings
        ComputerName = WindowsInformation.GetComputerName();
        ComputerDescription = WindowsInformation.GetComputerDescription();
        try
        {
            InstallOemApps = !RegistrySettingsEngine.GetBool(RegistryHive.LocalMachine,
                RegistrySettingsCatalog.InstallOemApps.KeyPath,
                RegistrySettingsCatalog.InstallOemApps.ValueName);
        }
        catch
        {

        }

        _isInitialized = true;
    }

    partial void OnInstallOemAppsChanged(bool value) 
        => RegistrySettingsEngine.SetBool(RegistryHive.LocalMachine,
            RegistrySettingsCatalog.InstallOemApps.KeyPath,
            RegistrySettingsCatalog.InstallOemApps.ValueName, !value);

    partial void OnComputerNameChanged(string value) { if (_isInitialized) AreChangesPending = true; }
    partial void OnComputerDescriptionChanged(string value) { if (_isInitialized) AreChangesPending = true; }

    [RelayCommand]
    public void ApplyChanges()
    {
        IsComputerNameError = false;

        // Computer name
        if (ComputerName != WindowsInformation.GetComputerName()) // Check if there's changes
        {
            if (!WindowsInformation.IsValidComputerName(ComputerName)) // Validate
                IsComputerNameError = true;
            else if (WindowsInformation.SetComputerName(ComputerName)) // Try to set
                IsRestartRequired = true;
            else
                IsComputerNameError = true;
        }
        
        // Computer description
        if (ComputerDescription != WindowsInformation.GetComputerDescription()) // Check if there's changes
        {
            if (WindowsInformation.IsValidComputerDescription(ComputerDescription))
                WindowsInformation.SetComputerDescription(ComputerDescription);
        }

        AreChangesPending = false;
    }

    [RelayCommand]
    public void CancelChanges()
    {
        _isInitialized = false;

        // Retrieve data again
        ComputerName = WindowsInformation.GetComputerName();
        ComputerDescription = WindowsInformation.GetComputerDescription();

        _isInitialized = true;
        AreChangesPending = false;
    }

    [RelayCommand]
    public static void Restart()
        => Shutdown.RestartNow(true);

    [RelayCommand]
    public static void RelaunchAsAdmin()
    {
        ProcessStartInfo psi = new()
        {
            FileName = Process.GetCurrentProcess().MainModule?.FileName,
            Verb = "runas",
            UseShellExecute = true,
            Arguments = "--newinstance " + CplArgs.SystemPropertiesComputerNameExePath
        };
        Process.Start(psi);
        Process.GetCurrentProcess().Kill();
    }
}
