// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using WinUIEx;

namespace Rebound.Hub;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
        => InitializeComponent();

    private async void RootFrame_Loaded(object sender, RoutedEventArgs e)
    {
        RootFrame.Navigate(typeof(Views.ShellPage));
        await App.ReboundService.LoadModsStatesAsync().ConfigureAwait(false);
        await App.ReboundService.InitializeAsync().ConfigureAwait(false);
        await App.ReboundService.CheckForUpdatesAsync().ConfigureAwait(false);
    }
}