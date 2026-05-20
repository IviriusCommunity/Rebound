// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using Rebound.Forge.Engines;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views.Secondary;

internal sealed partial class ConfigureAutoBackupPage : Page
{
    public event Action? Closed;

    public ConfigureAutoBackupPage()
    {
        InitializeComponent();
        LoadFromRegistry();
    }

    private void LoadFromRegistry()
    {
        var frequency = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Frequency", 2);

        var day = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Day", 0);

        var hour = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Hour", 19);

        // Frequency: 1 = Daily, 2 = Weekly, 3 = Monthly
        FrequencyComboBox.SelectedIndex = frequency switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            _ => 1
        };

        DayComboBox.SelectedIndex = Math.Clamp(day, 0, 6);
        HourComboBox.SelectedIndex = Math.Clamp(hour, 0, 23);

        UpdateDayComboBoxState();
    }

    private void UpdateDayComboBoxState()
    {
        // Day only applies to Weekly
        DayComboBox.IsEnabled = FrequencyComboBox.SelectedIndex == 1;
    }

    private void WriteToRegistry()
    {
        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup");

        // Frequency: ComboBox index 0 = Daily (1), 1 = Weekly (2), 2 = Monthly (3)
        var frequency = FrequencyComboBox.SelectedIndex switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            _ => 2
        };

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Frequency", frequency);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Day", DayComboBox.SelectedIndex);

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Hour", HourComboBox.SelectedIndex);
    }

    private void OnFrequencyChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDayComboBoxState();
        WriteToRegistry();
    }

    private void OnDayChanged(object sender, SelectionChangedEventArgs e)
        => WriteToRegistry();

    private void OnHourChanged(object sender, SelectionChangedEventArgs e)
        => WriteToRegistry();

    private void OnCloseClicked(object sender, RoutedEventArgs e)
        => Closed?.Invoke();
}
