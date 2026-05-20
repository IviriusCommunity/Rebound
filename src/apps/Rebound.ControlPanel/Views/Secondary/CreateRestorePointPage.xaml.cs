// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views.Secondary;

internal sealed partial class CreateRestorePointPage : Page
{
    public event Action<string, int>? Confirmed;
    public event Action? Cancelled;

    public CreateRestorePointPage()
    {
        InitializeComponent();
    }

    private void OnCreateClicked(object sender, RoutedEventArgs e)
    {
        var description = DescriptionBox.Text?.Trim() ?? string.Empty;

        // Map ComboBox index to SrSetRestorePoint event type constant
        var eventType = TypeComboBox.SelectedIndex switch
        {
            0 => 12, // MODIFY_SETTINGS
            1 => 0,  // APPLICATION_INSTALL
            2 => 1,  // APPLICATION_UNINSTALL
            3 => 10, // DEVICE_DRIVER_INSTALL
            4 => 13, // CANCELLED_OPERATION
            _ => 12
        };

        Confirmed?.Invoke(description, eventType);
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
        => Cancelled?.Invoke();
}

