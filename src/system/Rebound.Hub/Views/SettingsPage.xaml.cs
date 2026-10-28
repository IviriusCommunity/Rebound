// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Windows.UI.Xaml;
using Rebound.Hub.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Rebound.Hub.Views;

internal partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
    }
}