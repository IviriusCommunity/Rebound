// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rebound.ControlPanel.ViewModels;

/// <summary>
/// ViewModel for the DirectX settings page.
/// Requires administrator elevation - most settings are stored under HKEY_LOCAL_MACHINE.
/// </summary>
internal partial class DirectXViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsElevated { get; set; }

    #region D3D

    #region Scope

    [ObservableProperty] public partial bool IsAddingD3DScope { get; set; } 
    [ObservableProperty] public partial int SelectedD3DScopeAppIndex { get; set; } = -1;
    [ObservableProperty] public partial bool IsD3DScopeAppsListEmpty { get; set; } = true;
    [ObservableProperty] public partial string D3DScopeInputPath { get; set; } = string.Empty;

    public ObservableCollection<string> D3DScopeApps { get; } = [];

    #endregion

    #region Debug Layer

    /// <summary>
    /// 0 = Application Controlled, 1 = Force On, 2 = Force Off
    /// </summary>
    [ObservableProperty]
    public partial int D3DDebugLayerMode { get; set; }

    /// <summary>
    /// Stored inverted in the registry: 0 = tracking enabled (checkbox on).
    /// </summary>
    [ObservableProperty]
    public partial bool ConservativeResourceStateTracking { get; set; }

    [ObservableProperty]
    public partial int GpuSlowdownFactor { get; set; } = 1;

    #endregion

    #region GPU-based Validation

    /// <summary>
    /// 0 = Application Controlled, 1 = Force On, 2 = Force Off
    /// </summary>
    [ObservableProperty]
    public partial int GpuBasedValidationMode { get; set; }

    [ObservableProperty]
    public partial bool PsoCreateFrontLoad { get; set; }

    /// <summary>
    /// 0 = Application Controlled, 1 = Force On, 2 = Force Off
    /// </summary>
    [ObservableProperty]
    public partial int ShaderPatchMode { get; set; }

    #endregion

    #region Synchronized Command Queues

    /// <summary>
    /// 0 = Application Controlled, 1 = Force On, 2 = Force Off
    /// Registry stores the disable flag (inverted).
    /// </summary>
    [ObservableProperty]
    public partial int SynchronizedCommandQueuesMode { get; set; }

    #endregion

    #region Mute Settings

    [ObservableProperty]
    public partial int SelectedMutedMessageId { get; set; } = -1;

    [ObservableProperty]
    public partial bool IsAddingMuteMessage { get; set; }

    [ObservableProperty]
    public partial bool IsMuteIdsListEmpty { get; set; } = true;

    /// <summary>
    /// false = mute only selected IDs, true = mute all messages
    /// </summary>
    [ObservableProperty]
    public partial bool MuteAllMessages { get; set; }

    public ObservableCollection<string> MutedMessageIds { get; } = [];

    [ObservableProperty]
    public partial string MuteInputId { get; set; } = string.Empty;

    // Severity
    [ObservableProperty] public partial bool MuteCorruption { get; set; }
    [ObservableProperty] public partial bool MuteError { get; set; }
    [ObservableProperty] public partial bool MuteWarning { get; set; }
    [ObservableProperty] public partial bool MuteInfo { get; set; }
    [ObservableProperty] public partial bool MuteMessage { get; set; }

    // Category
    [ObservableProperty] public partial bool MuteApplicationDefined { get; set; }
    [ObservableProperty] public partial bool MuteMiscellaneous { get; set; }
    [ObservableProperty] public partial bool MuteInitialization { get; set; }
    [ObservableProperty] public partial bool MuteCleanup { get; set; }
    [ObservableProperty] public partial bool MuteCompilation { get; set; }
    [ObservableProperty] public partial bool MuteStateCreation { get; set; }
    [ObservableProperty] public partial bool MuteStateSetting { get; set; }
    [ObservableProperty] public partial bool MuteStateGetting { get; set; }
    [ObservableProperty] public partial bool MuteExecution { get; set; }
    [ObservableProperty] public partial bool MuteResourceManipulation { get; set; }
    [ObservableProperty] public partial bool MuteShader { get; set; }

    #endregion

    #region Break Settings

    [ObservableProperty]
    public partial int SelectedBreakMessageId { get; set; } = -1;

    [ObservableProperty]
    public partial bool IsAddingBreakOnMessage { get; set; }

    [ObservableProperty]
    public partial bool BreakOnApiError { get; set; }

    [ObservableProperty]
    public partial bool IsBreakIdsListEmpty { get; set; } = true;

    public ObservableCollection<string> BreakMessageIds { get; } = [];

    [ObservableProperty]
    public partial string BreakInputId { get; set; } = string.Empty;

    // Severity
    [ObservableProperty] public partial bool BreakOnCorruption { get; set; }
    [ObservableProperty] public partial bool BreakOnError { get; set; }
    [ObservableProperty] public partial bool BreakOnWarning { get; set; }
    [ObservableProperty] public partial bool BreakOnInfo { get; set; }
    [ObservableProperty] public partial bool BreakOnMessage { get; set; }

    // Category
    [ObservableProperty] public partial bool BreakOnApplicationDefined { get; set; }
    [ObservableProperty] public partial bool BreakOnMiscellaneous { get; set; }
    [ObservableProperty] public partial bool BreakOnInitialization { get; set; }
    [ObservableProperty] public partial bool BreakOnCleanup { get; set; }
    [ObservableProperty] public partial bool BreakOnCompilation { get; set; }
    [ObservableProperty] public partial bool BreakOnStateCreation { get; set; }
    [ObservableProperty] public partial bool BreakOnStateSetting { get; set; }
    [ObservableProperty] public partial bool BreakOnStateGetting { get; set; }
    [ObservableProperty] public partial bool BreakOnExecution { get; set; }
    [ObservableProperty] public partial bool BreakOnResourceManipulation { get; set; }
    [ObservableProperty] public partial bool BreakOnShader { get; set; }

    #endregion

    #region Feature Level / WARP

    private static readonly (string Display, int Dword)[] FeatureLevels =
    [
        ("None",  0x0000),
        ("12_2",  0xc200),
        ("12_1",  0xc100),
        ("12_0",  0xc000),
        ("11_1",  0xb100),
        ("11_0",  0xb000),
        ("10_1",  0xa100),
        ("10_0",  0xa000),
        ("9_3",   0x9300),
        ("9_2",   0x9200),
        ("9_1",   0x9100),
    ];

    public ObservableCollection<string> FeatureLevelDisplayNames =>
        [.. FeatureLevels.Select(x => x.Display)];

    [ObservableProperty]
    public partial int FeatureLevelLimitIndex { get; set; }

    [ObservableProperty]
    public partial bool DisableFeatureLevelUpgrade { get; set; }

    [ObservableProperty]
    public partial bool ForceWarp { get; set; }

    #endregion

    #endregion

    #region D2D

    #region Scope

    public ObservableCollection<string> D2DScopeApps { get; } = [];

    [ObservableProperty] public partial int SelectedD2DScopeAppIndex { get; set; } = -1;
    [ObservableProperty] public partial bool IsAddingD2DScope { get; set; }
    [ObservableProperty] public partial bool IsD2DScopeAppsListEmpty { get; set; } = true;
    [ObservableProperty] public partial string D2DScopeInputPath { get; set; } = string.Empty;

    #endregion

    #region Debug layer

    /// <summary>
    /// 0 = Application Controlled, 1 = Force On, 2 = Force Off
    /// </summary>
    [ObservableProperty] public partial int D2DDebugLayerMode { get; set; }

    /// <summary>
    /// 0 = Low, 1 = Medium, 2 = High
    /// </summary>
    [ObservableProperty] public partial int D2DDebugLevel { get; set; }

    #endregion

    #endregion

    public DirectXViewModel()
    {
        // Properties
        IsElevated = AppHelper.IsRunningAsAdmin();

        D3DScopeApps.CollectionChanged += (s, e) => { IsD3DScopeAppsListEmpty = D3DScopeApps.Count <= 0; };
        MutedMessageIds.CollectionChanged += (s, e) => { IsMuteIdsListEmpty = MutedMessageIds.Count <= 0; };
        BreakMessageIds.CollectionChanged += (s, e) => { IsBreakIdsListEmpty = BreakMessageIds.Count <= 0; };
        D2DScopeApps.CollectionChanged += (s, e) => { IsD2DScopeAppsListEmpty = D2DScopeApps.Count <= 0; };
        LoadSettings();
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(D3DDebugLayerMode):
            case nameof(ConservativeResourceStateTracking):
            case nameof(GpuSlowdownFactor):
                ApplyD3DDebugLayerSettings();
                break;

            case nameof(GpuBasedValidationMode):
            case nameof(PsoCreateFrontLoad):
            case nameof(ShaderPatchMode):
                ApplyGpuBasedValidationSettings();
                break;

            case nameof(SynchronizedCommandQueuesMode):
                ApplySynchronizedCommandQueuesSettings();
                break;

            case nameof(MuteAllMessages):
            case nameof(MuteCorruption):
            case nameof(MuteError):
            case nameof(MuteWarning):
            case nameof(MuteInfo):
            case nameof(MuteMessage):
            case nameof(MuteApplicationDefined):
            case nameof(MuteMiscellaneous):
            case nameof(MuteInitialization):
            case nameof(MuteCleanup):
            case nameof(MuteCompilation):
            case nameof(MuteStateCreation):
            case nameof(MuteStateSetting):
            case nameof(MuteStateGetting):
            case nameof(MuteExecution):
            case nameof(MuteResourceManipulation):
            case nameof(MuteShader):
                ApplyMuteSettings();
                break;

            case nameof(BreakOnApiError):
            case nameof(BreakOnCorruption):
            case nameof(BreakOnError):
            case nameof(BreakOnWarning):
            case nameof(BreakOnInfo):
            case nameof(BreakOnMessage):
            case nameof(BreakOnApplicationDefined):
            case nameof(BreakOnMiscellaneous):
            case nameof(BreakOnInitialization):
            case nameof(BreakOnCleanup):
            case nameof(BreakOnCompilation):
            case nameof(BreakOnStateCreation):
            case nameof(BreakOnStateSetting):
            case nameof(BreakOnStateGetting):
            case nameof(BreakOnExecution):
            case nameof(BreakOnResourceManipulation):
            case nameof(BreakOnShader):
                ApplyBreakSettings();
                break;

            case nameof(FeatureLevelLimitIndex):
            case nameof(DisableFeatureLevelUpgrade):
            case nameof(ForceWarp):
                ApplyFeatureLevelSettings();
                break;

            case nameof(D2DDebugLayerMode):
            case nameof(D2DDebugLevel):
                ApplyD2DDebugLayerSettings();
                break;
        }
    }

    #region Load helpers

    private void LoadSettings()
    {
        // --- D3D debug layer ---
        D3DDebugLayerMode = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.LoadDebugLayerDlls.KeyPath,
            RegistrySettingsCatalog.LoadDebugLayerDlls.ValueName, 0);

        ConservativeResourceStateTracking = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableConservativeResourceStateTracking.KeyPath,
            RegistrySettingsCatalog.DisableConservativeResourceStateTracking.ValueName, 0) == 0;

        GpuSlowdownFactor = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GPUSlowdownFactor.KeyPath,
            RegistrySettingsCatalog.GPUSlowdownFactor.ValueName, 1);

        // --- GPU-based validation ---
        GpuBasedValidationMode = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableGpuBasedValidation.KeyPath,
            RegistrySettingsCatalog.EnableGpuBasedValidation.ValueName, 0);

        PsoCreateFrontLoad = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GpuBasedValidationPsoCreateFrontLoad.KeyPath,
            RegistrySettingsCatalog.GpuBasedValidationPsoCreateFrontLoad.ValueName);

        ShaderPatchMode = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GpuBasedValidationShaderPatchMode.KeyPath,
            RegistrySettingsCatalog.GpuBasedValidationShaderPatchMode.ValueName, 0);

        // --- Synchronized command queues ---
        var syncDisabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableSynchronizedCommandQueueValidation.KeyPath,
            RegistrySettingsCatalog.DisableSynchronizedCommandQueueValidation.ValueName, 0);
        SynchronizedCommandQueuesMode = syncDisabled == 0 ? 0 : 2;

        // --- Mute ---
        MuteAllMessages = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MuteDebugOutput.KeyPath,
            RegistrySettingsCatalog.MuteDebugOutput.ValueName);

        LoadMuteFlags();
        LoadMutedMessageIds();

        // --- Break ---
        BreakOnApiError = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableBreakOnApiError.KeyPath,
            RegistrySettingsCatalog.EnableBreakOnApiError.ValueName);

        LoadBreakFlags();
        LoadBreakMessageIds();

        // --- Feature level / WARP ---
        var dword = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MaxFeatureLevel.KeyPath,
            RegistrySettingsCatalog.MaxFeatureLevel.ValueName, 0);

        var match = Array.FindIndex(FeatureLevels, x => x.Dword == dword);
        FeatureLevelLimitIndex = match < 0 ? 0 : match;

        DisableFeatureLevelUpgrade = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableFeatureLevelUpgrade.KeyPath,
            RegistrySettingsCatalog.DisableFeatureLevelUpgrade.ValueName);

        ForceWarp = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ForceWARP.KeyPath,
            RegistrySettingsCatalog.ForceWARP.ValueName);

        // --- D3D scope apps ---
        LoadScopeApps(RegistrySettingsCatalog.D3DScopeDrivers.KeyPath, D3DScopeApps);

        // --- D2D debug layer ---
        D2DDebugLayerMode = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.D2DEnableDebugLayer.KeyPath,
            RegistrySettingsCatalog.D2DEnableDebugLayer.ValueName, 0);

        D2DDebugLevel = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.D2DDebugLevel.KeyPath,
            RegistrySettingsCatalog.D2DDebugLevel.ValueName, 0);

        // --- D2D scope apps ---
        LoadScopeApps(RegistrySettingsCatalog.D2DScopeDrivers.KeyPath, D2DScopeApps);
    }

    private void LoadMuteFlags()
    {
        var flags = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MuteFlags.KeyPath,
            RegistrySettingsCatalog.MuteFlags.ValueName, 0);

        MuteCorruption = (flags & (1 << 0)) != 0;
        MuteError = (flags & (1 << 1)) != 0;
        MuteWarning = (flags & (1 << 2)) != 0;
        MuteInfo = (flags & (1 << 3)) != 0;
        MuteMessage = (flags & (1 << 4)) != 0;
        MuteApplicationDefined = (flags & (1 << 5)) != 0;
        MuteMiscellaneous = (flags & (1 << 6)) != 0;
        MuteInitialization = (flags & (1 << 7)) != 0;
        MuteCleanup = (flags & (1 << 8)) != 0;
        MuteCompilation = (flags & (1 << 9)) != 0;
        MuteStateCreation = (flags & (1 << 10)) != 0;
        MuteStateSetting = (flags & (1 << 11)) != 0;
        MuteStateGetting = (flags & (1 << 12)) != 0;
        MuteExecution = (flags & (1 << 13)) != 0;
        MuteResourceManipulation = (flags & (1 << 14)) != 0;
        MuteShader = (flags & (1 << 15)) != 0;
    }

    private void LoadBreakFlags()
    {
        var flags = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.BreakFlags.KeyPath,
            RegistrySettingsCatalog.BreakFlags.ValueName, 0);

        BreakOnCorruption = (flags & (1 << 0)) != 0;
        BreakOnError = (flags & (1 << 1)) != 0;
        BreakOnWarning = (flags & (1 << 2)) != 0;
        BreakOnInfo = (flags & (1 << 3)) != 0;
        BreakOnMessage = (flags & (1 << 4)) != 0;
        BreakOnApplicationDefined = (flags & (1 << 5)) != 0;
        BreakOnMiscellaneous = (flags & (1 << 6)) != 0;
        BreakOnInitialization = (flags & (1 << 7)) != 0;
        BreakOnCleanup = (flags & (1 << 8)) != 0;
        BreakOnCompilation = (flags & (1 << 9)) != 0;
        BreakOnStateCreation = (flags & (1 << 10)) != 0;
        BreakOnStateSetting = (flags & (1 << 11)) != 0;
        BreakOnStateGetting = (flags & (1 << 12)) != 0;
        BreakOnExecution = (flags & (1 << 13)) != 0;
        BreakOnResourceManipulation = (flags & (1 << 14)) != 0;
        BreakOnShader = (flags & (1 << 15)) != 0;
    }

    private static void LoadScopeApps(string keyPath, ObservableCollection<string> collection)
    {
        collection.Clear();
        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
        if (key == null) return;
        foreach (var name in key.GetValueNames())
        {
            if (key.GetValue(name) is string val && !string.IsNullOrWhiteSpace(val))
                collection.Add(val);
        }
    }

    private void LoadMutedMessageIds()
    {
        MutedMessageIds.Clear();
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.MuteList.KeyPath);
        if (key == null) return;
        foreach (var name in key.GetValueNames())
            MutedMessageIds.Add(name);
    }

    private void LoadBreakMessageIds()
    {
        BreakMessageIds.Clear();
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.BreakList.KeyPath);
        if (key == null) return;
        foreach (var name in key.GetValueNames())
            BreakMessageIds.Add(name);
    }

    #endregion

    #region Apply helpers

    private void ApplyD3DDebugLayerSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.LoadDebugLayerDlls.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.LoadDebugLayerDlls.KeyPath,
            RegistrySettingsCatalog.LoadDebugLayerDlls.ValueName,
            D3DDebugLayerMode);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableConservativeResourceStateTracking.KeyPath,
            RegistrySettingsCatalog.DisableConservativeResourceStateTracking.ValueName,
            ConservativeResourceStateTracking ? 0 : 1);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GPUSlowdownFactor.KeyPath,
            RegistrySettingsCatalog.GPUSlowdownFactor.ValueName,
            GpuSlowdownFactor);
    }

    private void ApplyGpuBasedValidationSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableGpuBasedValidation.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableGpuBasedValidation.KeyPath,
            RegistrySettingsCatalog.EnableGpuBasedValidation.ValueName,
            GpuBasedValidationMode);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GpuBasedValidationPsoCreateFrontLoad.KeyPath,
            RegistrySettingsCatalog.GpuBasedValidationPsoCreateFrontLoad.ValueName,
            PsoCreateFrontLoad);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.GpuBasedValidationShaderPatchMode.KeyPath,
            RegistrySettingsCatalog.GpuBasedValidationShaderPatchMode.ValueName,
            ShaderPatchMode);
    }

    private void ApplySynchronizedCommandQueuesSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableSynchronizedCommandQueueValidation.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableSynchronizedCommandQueueValidation.KeyPath,
            RegistrySettingsCatalog.DisableSynchronizedCommandQueueValidation.ValueName,
            SynchronizedCommandQueuesMode == 2 ? 1 : 0);
    }

    private void ApplyMuteSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MuteDebugOutput.KeyPath);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MuteDebugOutput.KeyPath,
            RegistrySettingsCatalog.MuteDebugOutput.ValueName,
            MuteAllMessages);

        var flags = 0;
        if (MuteCorruption) flags |= (1 << 0);
        if (MuteError) flags |= (1 << 1);
        if (MuteWarning) flags |= (1 << 2);
        if (MuteInfo) flags |= (1 << 3);
        if (MuteMessage) flags |= (1 << 4);
        if (MuteApplicationDefined) flags |= (1 << 5);
        if (MuteMiscellaneous) flags |= (1 << 6);
        if (MuteInitialization) flags |= (1 << 7);
        if (MuteCleanup) flags |= (1 << 8);
        if (MuteCompilation) flags |= (1 << 9);
        if (MuteStateCreation) flags |= (1 << 10);
        if (MuteStateSetting) flags |= (1 << 11);
        if (MuteStateGetting) flags |= (1 << 12);
        if (MuteExecution) flags |= (1 << 13);
        if (MuteResourceManipulation) flags |= (1 << 14);
        if (MuteShader) flags |= (1 << 15);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MuteFlags.KeyPath,
            RegistrySettingsCatalog.MuteFlags.ValueName,
            flags);
    }

    private void ApplyBreakSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableBreakOnApiError.KeyPath);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableBreakOnApiError.KeyPath,
            RegistrySettingsCatalog.EnableBreakOnApiError.ValueName,
            BreakOnApiError);

        var flags = 0;
        if (BreakOnCorruption) flags |= (1 << 0);
        if (BreakOnError) flags |= (1 << 1);
        if (BreakOnWarning) flags |= (1 << 2);
        if (BreakOnInfo) flags |= (1 << 3);
        if (BreakOnMessage) flags |= (1 << 4);
        if (BreakOnApplicationDefined) flags |= (1 << 5);
        if (BreakOnMiscellaneous) flags |= (1 << 6);
        if (BreakOnInitialization) flags |= (1 << 7);
        if (BreakOnCleanup) flags |= (1 << 8);
        if (BreakOnCompilation) flags |= (1 << 9);
        if (BreakOnStateCreation) flags |= (1 << 10);
        if (BreakOnStateSetting) flags |= (1 << 11);
        if (BreakOnStateGetting) flags |= (1 << 12);
        if (BreakOnExecution) flags |= (1 << 13);
        if (BreakOnResourceManipulation) flags |= (1 << 14);
        if (BreakOnShader) flags |= (1 << 15);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.BreakFlags.KeyPath,
            RegistrySettingsCatalog.BreakFlags.ValueName,
            flags);
    }

    private void ApplyFeatureLevelSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MaxFeatureLevel.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.MaxFeatureLevel.KeyPath,
            RegistrySettingsCatalog.MaxFeatureLevel.ValueName,
            FeatureLevels[FeatureLevelLimitIndex].Dword);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableFeatureLevelUpgrade.KeyPath,
            RegistrySettingsCatalog.DisableFeatureLevelUpgrade.ValueName,
            DisableFeatureLevelUpgrade);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ForceWARP.KeyPath,
            RegistrySettingsCatalog.ForceWARP.ValueName,
            ForceWarp);
    }

    private void ApplyD2DDebugLayerSettings()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.D2DEnableDebugLayer.KeyPath);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.D2DEnableDebugLayer.KeyPath,
            RegistrySettingsCatalog.D2DEnableDebugLayer.ValueName,
            D2DDebugLayerMode);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.D2DDebugLevel.KeyPath,
            RegistrySettingsCatalog.D2DDebugLevel.ValueName,
            D2DDebugLevel);
    }

    #endregion 

    #region D3D scope commands

    [RelayCommand]
    public void AddD3DScopeApp()
        => IsAddingD3DScope = true;

    [RelayCommand]
    public void AddD3DScopeAppImpl(string data)
    {
        var path = string.IsNullOrWhiteSpace(data) ? D3DScopeInputPath.Trim() : data;
        if (string.IsNullOrWhiteSpace(path) || D3DScopeApps.Contains(path)) return;

        RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.D3DScopeDrivers.KeyPath);
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D3DScopeDrivers.KeyPath, writable: true);
        key?.SetValue(Path.GetFileName(path), path, RegistryValueKind.String);

        D3DScopeApps.Add(path);
        D3DScopeInputPath = string.Empty;
        IsAddingD3DScope = false;
    }

    [RelayCommand]
    public void CancelAddD3DScopeApp()
        => IsAddingD3DScope = false;

    [RelayCommand]
    public void RemoveD3DScopeApp()
    {
        if (SelectedD3DScopeAppIndex >= D3DScopeApps.Count || SelectedD3DScopeAppIndex < 0) return;

        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D3DScopeDrivers.KeyPath, writable: true);
        key?.DeleteValue(Path.GetFileName(D3DScopeApps[SelectedD3DScopeAppIndex]), throwOnMissingValue: false);

        D3DScopeApps.Remove(D3DScopeApps[SelectedD3DScopeAppIndex]);
    }

    [RelayCommand]
    public void ClearAllD3DScopeApps()
    {
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D3DScopeDrivers.KeyPath, writable: true);
        if (key != null)
            foreach (var name in key.GetValueNames())
                key.DeleteValue(name, throwOnMissingValue: false);

        D3DScopeApps.Clear();
    }

    #endregion

    #region Mute list commands

    [RelayCommand]
    public void AddMutedMessageId()
        => IsAddingMuteMessage = true;

    [RelayCommand]
    public void AddMutedMessageIdImpl(string data)
    {
        var path = string.IsNullOrWhiteSpace(data) ? MuteInputId.Trim() : data;
        if (string.IsNullOrWhiteSpace(path) || MutedMessageIds.Contains(path)) return;

        RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.MuteList.KeyPath);
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.MuteList.KeyPath, writable: true);
        key?.SetValue(Path.GetFileName(path), path, RegistryValueKind.String);

        MutedMessageIds.Add(path);
        MuteInputId = string.Empty;
        IsAddingMuteMessage = false;
    }

    [RelayCommand]
    public void CancelAddMutedMessageId()
        => IsAddingMuteMessage = false;

    [RelayCommand]
    public void RemoveMutedMessageId()
    {
        if (SelectedMutedMessageId >= MutedMessageIds.Count || SelectedMutedMessageId < 0) return;

        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.MuteList.KeyPath, writable: true);
        key?.DeleteValue(MutedMessageIds[SelectedMutedMessageId], throwOnMissingValue: false);

        MutedMessageIds.Remove(MutedMessageIds[SelectedMutedMessageId]);
    }

    [RelayCommand]
    public void ClearAllMutedMessageIds()
    {
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.MuteList.KeyPath, writable: true);
        if (key != null)
            foreach (var name in key.GetValueNames())
                key.DeleteValue(name, throwOnMissingValue: false);

        MutedMessageIds.Clear();
    }

    #endregion

    #region Break list commands

    [RelayCommand]
    public void AddBreakMessageId()
        => IsAddingBreakOnMessage = true;

    [RelayCommand]
    public void AddBreakMessageIdImpl(string data)
    {
        var path = string.IsNullOrWhiteSpace(data) ? BreakInputId.Trim() : data;
        if (string.IsNullOrWhiteSpace(path) || BreakMessageIds.Contains(path)) return;

        RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.BreakList.KeyPath);
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.BreakList.KeyPath, writable: true);
        key?.SetValue(Path.GetFileName(path), path, RegistryValueKind.String);

        BreakMessageIds.Add(path);
        BreakInputId = string.Empty;
        IsAddingBreakOnMessage = false;
    }

    [RelayCommand]
    public void CancelAddBreakMessageId()
        => IsAddingBreakOnMessage = false;

    [RelayCommand]
    public void RemoveBreakMessageId()
    {
        if (SelectedBreakMessageId >= BreakMessageIds.Count || SelectedBreakMessageId < 0) return;

        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.BreakList.KeyPath, writable: true);
        key?.DeleteValue(BreakMessageIds[SelectedBreakMessageId], throwOnMissingValue: false);

        BreakMessageIds.Remove(BreakMessageIds[SelectedBreakMessageId]);
    }

    [RelayCommand]
    public void ClearAllBreakMessageIds()
    {
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.BreakList.KeyPath, writable: true);
        if (key != null)
            foreach (var name in key.GetValueNames())
                key.DeleteValue(name, throwOnMissingValue: false);

        BreakMessageIds.Clear();
    }

    #endregion

    #region D2D scope commands

    [RelayCommand]
    public void AddD2DScopeApp()
        => IsAddingD2DScope = true;

    [RelayCommand]
    public void AddD2DScopeAppImpl(string data)
    {
        var path = string.IsNullOrWhiteSpace(data) ? D2DScopeInputPath.Trim() : data;
        if (string.IsNullOrWhiteSpace(path) || D2DScopeApps.Contains(path)) return;

        RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.D2DScopeDrivers.KeyPath);
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D2DScopeDrivers.KeyPath, writable: true);
        key?.SetValue(Path.GetFileName(path), path, RegistryValueKind.String);

        D2DScopeApps.Add(path);
        D2DScopeInputPath = string.Empty;
        IsAddingD2DScope = false;
    }

    [RelayCommand]
    public void CancelAddD2DScopeApp()
        => IsAddingD2DScope = false;

    [RelayCommand]
    public void RemoveD2DScopeApp()
    {
        if (SelectedD2DScopeAppIndex >= D2DScopeApps.Count || SelectedD2DScopeAppIndex >= 0) return;

        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D2DScopeDrivers.KeyPath, writable: true);
        key?.DeleteValue(Path.GetFileName(D2DScopeApps[SelectedD2DScopeAppIndex]), throwOnMissingValue: false);

        D2DScopeApps.Remove(D2DScopeApps[SelectedD2DScopeAppIndex]);
    }

    [RelayCommand]
    public void ClearAllD2DScopeApps()
    {
        using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D2DScopeDrivers.KeyPath, writable: true);
        if (key != null)
            foreach (var name in key.GetValueNames())
                key.DeleteValue(name, throwOnMissingValue: false);

        D2DScopeApps.Clear();
    }

    #endregion

    #region Launchers

    [RelayCommand]
    public static void RelaunchAsAdmin()
    {
        ProcessStartInfo psi = new()
        {
            FileName = Process.GetCurrentProcess().MainModule?.FileName,
            Verb = "runas",
            UseShellExecute = true,
            Arguments = "--newinstance " + CplArgs.DirectXControlPanelExePath
        };
        Process.Start(psi);
        Process.GetCurrentProcess().Kill();
    }

    [RelayCommand]
    public static void LaunchDxDiag()
    {
        ProcessStartInfo psi = new()
        {
            FileName = "dxdiag.exe",
            Verb = "runas",
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    #endregion
}