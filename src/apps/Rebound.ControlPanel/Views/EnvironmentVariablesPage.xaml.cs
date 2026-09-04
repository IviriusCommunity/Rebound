// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.Dialogs;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Native.Helpers;
using Rebound.Core.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Rebound.ControlPanel.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class EnvironmentVariablesPage : Page
{
    private EnvironmentVariablesViewModel ViewModel { get; } = new();

    public EnvironmentVariablesPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public async Task DeleteUserEnvVarAsync()
        => await DeleteEnvVarAsync(EnvironmentScope.User).ConfigureAwait(true);

    [RelayCommand]
    public async Task DeleteSystemEnvVarAsync()
        => await DeleteEnvVarAsync(EnvironmentScope.System).ConfigureAwait(true);

    private async Task DeleteEnvVarAsync(EnvironmentScope scope)
    {
        ContentDialog dialog = new()
        {
            Title = $"Delete {(scope == EnvironmentScope.User ? "user" : "system")} environment variable",
            Content = $"Are you sure you want to delete {
                (scope == EnvironmentScope.User ? ViewModel.UserVariables : ViewModel.SystemVariables)
                    [(scope == EnvironmentScope.User ? ViewModel.SelectedUserVariable : ViewModel.SelectedSystemVariable)].Variable}?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        // User cancelled, exit early
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        // Find the variable and erase it
        EnvironmentVariablesHelper.SetVariable(
            (scope == EnvironmentScope.User ? ViewModel.UserVariables : ViewModel.SystemVariables)
                [(scope == EnvironmentScope.User ? ViewModel.SelectedUserVariable : ViewModel.SelectedSystemVariable)].Variable,
            null,
            scope);

        // Remove it from the list as well
        (scope == EnvironmentScope.User ? ViewModel.UserVariables : ViewModel.SystemVariables)
            .Remove((scope == EnvironmentScope.User ? ViewModel.UserVariables : ViewModel.SystemVariables)
                [(scope == EnvironmentScope.User ? ViewModel.SelectedUserVariable : ViewModel.SelectedSystemVariable)]);
        switch (scope)
        {
            case EnvironmentScope.User:
                ViewModel.SelectedUserVariable = 0;
                break;
            case EnvironmentScope.System:
                ViewModel.SelectedSystemVariable = 0;
                break;
        }
    }

    [RelayCommand]
    public async Task EditUserEnvVarAsync()
        => await EditEnvVarAsync(EnvironmentScope.User).ConfigureAwait(true);

    [RelayCommand]
    public async Task EditSystemEnvVarAsync()
        => await EditEnvVarAsync(EnvironmentScope.System).ConfigureAwait(true);

    private async Task EditEnvVarAsync(EnvironmentScope scope)
    {
        // Retrieve targets per scope
        var targetList = scope == EnvironmentScope.User ? ViewModel.UserVariables : ViewModel.SystemVariables;
        int selectedIndex = scope == EnvironmentScope.User ? ViewModel.SelectedUserVariable : ViewModel.SelectedSystemVariable;

        if ((uint)selectedIndex >= (uint)targetList.Count) return;

        var selected = targetList[selectedIndex];
        var dialogVm = new EditEnvVarDialogViewModel(selected.Variable, selected.Value);

        var dialog = new EditEnvVarDialog(dialogVm) { XamlRoot = XamlRoot };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary || !dialogVm.IsValid())
            return;

        string newName = dialogVm.Name.Trim();
        string newValue = dialogVm.GetFinalValue();

        if (selected.Variable.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(selected.Value ?? string.Empty, newValue, StringComparison.Ordinal))
            return;

        // Trimmed string
        string oldName = selected.Variable;
        string? oldValue = selected.Value;

        try
        {
            if (oldName != newName)
            {
                if (targetList.Any(i => i.Variable == newName))
                {
                    ContentDialog overwriteDialog = new()
                    {
                        Title = "Variable Already Exists",
                        Content = $"A {(scope == EnvironmentScope.User ? "user" : "system")} variable named '{newName}' already exists. Do you want to overwrite it?",
                        PrimaryButtonText = "Overwrite",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = XamlRoot
                    };

                    // User cancelled the overwrite
                    if (await overwriteDialog.ShowAsync() != ContentDialogResult.Primary)
                        return;
                }

                // Remove the old variable from the environment
                EnvironmentVariablesHelper.SetVariable(oldName, null, scope);
            }

            // Set the variable itself
            EnvironmentVariablesHelper.SetVariable(newName, newValue, scope);

            // Remove both the old entry and any overwritten entry
            var oldItem = targetList.FirstOrDefault(i => i.Variable == oldName);
            if (oldItem != null) targetList.Remove(oldItem);

            var overwrittenItem = targetList.FirstOrDefault(i => i.Variable == newName);
            if (overwrittenItem != null) targetList.Remove(overwrittenItem);

            // Add the fresh variable entry
            targetList.Add(new EnvironmentVariable()
            {
                Variable = newName,
                Value = newValue
            });
        }
        catch (Exception ex)
        {
            await new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to edit environment variable: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            }.ShowAsync();
        }
    }

    [RelayCommand]
    public async Task CreateUserEnvVarAsync()
        => await CreateEnvVarAsync(EnvironmentScope.User).ConfigureAwait(true);

    [RelayCommand]
    public async Task CreateSystemEnvVarAsync()
        => await CreateEnvVarAsync(EnvironmentScope.System).ConfigureAwait(true);

    private async Task CreateEnvVarAsync(EnvironmentScope scope)
    {
        TextBox variableTextBox = new() { Header = "Variable", TextWrapping = TextWrapping.Wrap };
        TextBox valueTextBox = new() { Header = "Value", TextWrapping = TextWrapping.Wrap };

        // Stack panel for the two text boxes
        StackPanel sp = new() { Spacing = 16 };
        sp.Children.Add(variableTextBox);
        sp.Children.Add(valueTextBox);

        ContentDialog dialog = new()
        {
            Title = $"Add {(scope == EnvironmentScope.User ? "user" : "system")} environment variable",
            Content = sp,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false, // Empty strings are default
            XamlRoot = XamlRoot
        };

        // Make sure the inputs aren't empty strings
        void ValidateInputs()
        {
            dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(variableTextBox.Text)
                && !string.IsNullOrWhiteSpace(valueTextBox.Text);
        }
        variableTextBox.TextChanged += (s, e) => ValidateInputs();
        valueTextBox.TextChanged += (s, e) => ValidateInputs();

        // User cancelled, exit early
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        // Trimmed strings
        string variableName = variableTextBox.Text.Trim();
        string variableValue = valueTextBox.Text.Trim();

        try
        {
            // Variable already exists (edge case)
            var existing = scope == EnvironmentScope.User
                ? ViewModel.UserVariables.FirstOrDefault(v => v.Variable.Equals(variableName, StringComparison.OrdinalIgnoreCase))
                : ViewModel.SystemVariables.FirstOrDefault(v => v.Variable.Equals(variableName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                ContentDialog overwriteDialog = new()
                {
                    Title = "Variable Already Exists",
                    Content = $"A {(scope == EnvironmentScope.User ? "user" : "system")} variable named '{variableName}' already exists. Do you want to overwrite it?",
                    PrimaryButtonText = "Overwrite",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                // User cancelled the overwrite
                if (await overwriteDialog.ShowAsync() != ContentDialogResult.Primary)
                    return;
            }

            // Set the variable itself
            EnvironmentVariablesHelper.SetVariable(variableName, variableValue, scope);

            // Update the list
            if (existing is not null)
            {
                if (scope == EnvironmentScope.User)
                    ViewModel.UserVariables.Remove(existing);
                else
                    ViewModel.SystemVariables.Remove(existing);
            }

            if (scope == EnvironmentScope.User)
            {
                ViewModel.UserVariables.Add(new EnvironmentVariable()
                {
                    Variable = variableName,
                    Value = variableValue
                });
            }
            else
            {
                ViewModel.SystemVariables.Add(new EnvironmentVariable()
                {
                    Variable = variableName,
                    Value = variableValue
                });
            }
        }
        catch (Exception ex)
        {
            await new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to save environment variable: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            }.ShowAsync();
        }
    }

    [RelayCommand]
    public async Task RelaunchAsAdminAsync()
    {
        try
        {
            App.SingleInstanceAppService.Relaunch(new InstanceRelaunchOptions
            {
                Elevated = true,
                ShutdownCurrent = true,
                ForceNewInstance = true,
                Arguments = CplArgs.ENVIRONMENT_VARIABLES
            });
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch Rebound Control Panel as administrator.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }
}
