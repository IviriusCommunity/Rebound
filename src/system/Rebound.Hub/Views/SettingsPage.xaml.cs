// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.Hub.ViewModels;

namespace Rebound.Hub.Views;

internal partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel = new();

    public SettingsPage()
    {
        InitializeComponent();
    }
}