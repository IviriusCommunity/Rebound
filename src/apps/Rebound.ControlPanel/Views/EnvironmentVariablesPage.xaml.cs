// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Native.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        var selected = scope == EnvironmentScope.User ? ViewModel.UserVariables[ViewModel.SelectedUserVariable] : ViewModel.SystemVariables[ViewModel.SelectedSystemVariable];

        TextBox valueTextBox = new()
        {
            Header = "Value",
            TextWrapping = TextWrapping.Wrap,
            Text = selected.Value
        };

        ContentDialog dialog = new()
        {
            Title = $"Edit {selected.Variable}",
            Content = valueTextBox,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(selected.Value),
            XamlRoot = XamlRoot
        };

        // Make sure the input isn't an empty string
        void ValidateInputs()
            => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(valueTextBox.Text);
        valueTextBox.TextChanged += (s, e) => ValidateInputs();

        // User cancelled, exit early
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        // Trimmed string
        string variableName = selected.Variable;
        string variableValue = valueTextBox.Text.Trim();

        try
        {
            // Set the variable itself
            EnvironmentVariablesHelper.SetVariable(variableName, variableValue, scope);

            // Update the list
            if (scope == EnvironmentScope.User)
            {
                ViewModel.UserVariables.Remove(ViewModel.UserVariables.First(i => i.Variable == variableName)!);
                ViewModel.UserVariables.Add(new EnvironmentVariable()
                {
                    Variable = variableName,
                    Value = variableValue
                });
            }
            else
            {
                ViewModel.SystemVariables.Remove(ViewModel.SystemVariables.First(i => i.Variable == variableName)!);
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
        // Yes constructing the XAML tree manually in the big 26
        TextBox variableTextBox = new() { Header = "Variable", TextWrapping = TextWrapping.Wrap };
        TextBox valueTextBox = new() { Header = "Value", TextWrapping = TextWrapping.Wrap };

        // Stack panel for the two text boxes
        StackPanel sp = new() { Spacing = 16 };
        sp.Children.Add(variableTextBox);
        sp.Children.Add(valueTextBox);

        ContentDialog dialog = new()
        {
            Title = $"Add {(scope == EnvironmentScope.User ? "User" : "System")} environment variable",
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
}
