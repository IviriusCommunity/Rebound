// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Native.Storage;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System.IO;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DirectXPage : Page
{
    private DirectXViewModel ViewModel { get; } = new();

    public DirectXPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public void AddD3DScopeAppFromFiles()
    {
        var result = FilePickers.PickOpenFile(
            App.MainWindow!.Handle,
            "Select an app or program",
            [
                new("Executable", ".exe;.com" ),
                    new("All files", "*" )
            ]);

        if (result.IsCancelled == true) 
            return;

        if (!string.IsNullOrWhiteSpace(result.Path))
        {
            ViewModel.AddD3DScopeAppImpl(result.Path);
        }
    }

    [RelayCommand]
    public void AddD2DScopeAppFromFiles()
    {
        var result = FilePickers.PickOpenFile(
            App.MainWindow!.Handle,
            "Select an app or program",
            [
                new("Executable", ".exe;.com" ),
                    new("All files", "*" )
            ]);

        if (result.IsCancelled == true)
            return;

        if (!string.IsNullOrWhiteSpace(result.Path))
        {
            ViewModel.AddD2DScopeAppImpl(result.Path);
        }
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var path = ViewModel.D3DScopeInputPath.Trim();
            if (string.IsNullOrWhiteSpace(path) || ViewModel.D3DScopeApps.Contains(path)) return;

            RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.D3DScopeDrivers.KeyPath);
            using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D3DScopeDrivers.KeyPath, writable: true);
            key?.SetValue(Path.GetFileName(path), path, RegistryValueKind.String);

            ViewModel.D3DScopeApps.Add(path);
            ViewModel.D3DScopeInputPath = string.Empty;
            ViewModel.IsAddingD3DScope = false;
        }
    }

    private void TextBox_KeyDown_1(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var id = ViewModel.MuteInputId.Trim();
            if (string.IsNullOrWhiteSpace(id) || ViewModel.MutedMessageIds.Contains(id)) return;

            RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.MuteList.KeyPath);
            using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.MuteList.KeyPath, writable: true);
            key?.SetValue(id, 1, RegistryValueKind.DWord);

            ViewModel.MutedMessageIds.Add(id);
            ViewModel.MuteInputId = string.Empty;
            ViewModel.IsAddingMuteMessage = false;
        }
    }

    private void TextBox_KeyDown_2(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var id = ViewModel.BreakInputId.Trim();
            if (string.IsNullOrWhiteSpace(id) || ViewModel.BreakMessageIds.Contains(id)) return;

            RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.BreakList.KeyPath);
            using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.BreakList.KeyPath, writable: true);
            key?.SetValue(id, 1, RegistryValueKind.DWord);

            ViewModel.BreakMessageIds.Add(id);
            ViewModel.BreakInputId = string.Empty;
            ViewModel.IsAddingBreakOnMessage = false;
        }
    }

    private void TextBox_KeyDown_3(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var path = ViewModel.D2DScopeInputPath.Trim();
            if (string.IsNullOrWhiteSpace(path) || ViewModel.D2DScopeApps.Contains(path)) return;

            RegistrySettingsEngine.EnsureKeyExists(RegistryHive.LocalMachine, RegistrySettingsCatalog.D2DScopeDrivers.KeyPath);
            using var key = Registry.LocalMachine.OpenSubKey(RegistrySettingsCatalog.D2DScopeDrivers.KeyPath, writable: true);
            key?.SetValue(System.IO.Path.GetFileName(path), path, RegistryValueKind.String);

            ViewModel.D2DScopeApps.Add(path);
            ViewModel.D2DScopeInputPath = string.Empty;
            ViewModel.IsAddingD2DScope = false;
        }
    }
}
