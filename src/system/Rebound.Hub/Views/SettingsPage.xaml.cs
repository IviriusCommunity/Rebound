// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Hub.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Rebound.Hub.Views;

internal partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public static async Task OpenLogFileAsync()
    {
        if (File.Exists(Variables.ReboundLogFile))
        {
            var file = await StorageFile.GetFileFromPathAsync(Variables.ReboundLogFile);
            await Launcher.LaunchFileAsync(file);
        }
        else
        {
            await App.MainWindow!.ShowMessageDialogAsync(
                "The log file does not exist on your machine.", 
                "Log file not found").ConfigureAwait(true);
        }
    }
}