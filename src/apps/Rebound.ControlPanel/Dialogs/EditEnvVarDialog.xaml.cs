// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.Models;
using Rebound.ControlPanel.ViewModels;

namespace Rebound.ControlPanel.Dialogs;

public sealed partial class EditEnvVarDialog : ContentDialog
{
    private EditEnvVarDialogViewModel ViewModel { get; }

    public EditEnvVarDialog(EditEnvVarDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void RemovePath_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EnvironmentVariableListItem item)
            ViewModel.RemovePathCommand.Execute(item);
    }

    private void KeyboardAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (string.IsNullOrEmpty(ViewModel.NewPathInput))
            return; // Let the dialog process the enter key to apply changes if there's no pending entry
        args.Handled = true;
        ViewModel.AddPathCommand.Execute(ViewModel.NewPathInput);
    }
}