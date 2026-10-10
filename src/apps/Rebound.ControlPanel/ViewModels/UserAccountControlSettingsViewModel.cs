// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Rebound.Core.Environment;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System;

namespace Rebound.ControlPanel.ViewModels;

internal partial class UserAccountControlSettingsViewModel : ObservableObject
{
    private bool _init = true;

    [ObservableProperty]
    public partial bool IsAdmin { get; set; }

    [ObservableProperty]
    public partial bool AllowSilentElevation { get; set; }

    [ObservableProperty]
    public partial int AdministratorsInAdminApprovalMode { get; set; }

    [ObservableProperty]
    public partial int ElevationPromptForStandardUsers { get; set; }

    [ObservableProperty]
    public partial bool DetectApplicationInstallations { get; set; }

    [ObservableProperty]
    public partial bool PromptOnSecureDesktop { get; set; }

    [ObservableProperty]
    public partial bool ValidateAdminCodeSignatures { get; set; }

    [ObservableProperty]
    public partial bool EnableSecureUIAPaths { get; set; }

    [ObservableProperty]
    public partial bool EnableVirtualization { get; set; }

    [ObservableProperty]
    public partial bool RunAllAdministratorsInAdminApprovalMode { get; set; }

    [ObservableProperty]
    public partial bool BuiltInAdministratorApprovalMode { get; set; }

    internal UserAccountControlSettingsViewModel()
    {
        IsAdmin = ApplicationEnvironment.IsRunningAsAdmin();

        if (!IsAdmin)
        {
            _init = false;
            return;
        }

        AllowSilentElevation = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableUIADesktopToggle, 
            0) == 1;

        AdministratorsInAdminApprovalMode = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
            5);

        ElevationPromptForStandardUsers = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ConsentPromptBehaviorUser, 3) switch
        {
            0 => 0,
            1 => 1,
            3 => 2,
            _ => 2
        };

        DetectApplicationInstallations = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableInstallerDetection, 
            1) == 1;

        PromptOnSecureDesktop = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.PromptOnSecureDesktop, 
            1) == 1;

        ValidateAdminCodeSignatures = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ValidateAdminCodeSignatures, 
            0) == 1;

        EnableSecureUIAPaths = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableSecureUIAPaths, 
            1) == 1;

        EnableVirtualization = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableVirtualization, 1) == 1;

        RunAllAdministratorsInAdminApprovalMode = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableLUA, 1) == 1;

        BuiltInAdministratorApprovalMode = RegistrySettingsEngine.GetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.FilterAdministratorToken, 0) == 1;

        _init = false;
    }

    partial void OnAllowSilentElevationChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.EnableUIADesktopToggle, value);
    }

    partial void OnAdministratorsInAdminApprovalModeChanged(int value)
    {
        if (!_init && value is >= 0 and <= 5)
        {
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
                value);
        }
    }

    partial void OnElevationPromptForStandardUsersChanged(int value)
    {
        if (!_init && value is >= 0 and <= 2)
        {
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.ConsentPromptBehaviorUser,
                value switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 3,
                    _ => throw new ArgumentOutOfRangeException(nameof(value))
                });
        }
    }

    partial void OnDetectApplicationInstallationsChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.EnableInstallerDetection, value);
    }

    partial void OnPromptOnSecureDesktopChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.PromptOnSecureDesktop, value);
    }

    partial void OnValidateAdminCodeSignaturesChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.ValidateAdminCodeSignatures, value);
    }

    partial void OnEnableSecureUIAPathsChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.EnableSecureUIAPaths, value);
    }

    partial void OnEnableVirtualizationChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.EnableVirtualization, value);
    }

    partial void OnRunAllAdministratorsInAdminApprovalModeChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.EnableLUA, value);
    }

    partial void OnBuiltInAdministratorApprovalModeChanged(bool value)
    {
        if (!_init)
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.FilterAdministratorToken, value);
    }
}