// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Native.Helpers;
using System;
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
    public async Task CreateUserEnvVarAsync()
    {
        // Yes constructing the XAML tree manually in the big 26
        ContentDialog dialog = new()
        {
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
            Title = "Add user environment variable"
        };
        var sp = new StackPanel()
        {
            Spacing = 16
        };
        var variableTextBox = new TextBox()
        {
            Header = "Variable",
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(variableTextBox);
        var valueTextBox = new TextBox()
        {
            Header = "Value",
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(valueTextBox);
        variableTextBox.TextChanged += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(variableTextBox.Text) || string.IsNullOrWhiteSpace(valueTextBox.Text))
                dialog.IsPrimaryButtonEnabled = false;
            else
                dialog.IsPrimaryButtonEnabled = true;
        };
        valueTextBox.TextChanged += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(variableTextBox.Text) || string.IsNullOrWhiteSpace(valueTextBox.Text))
                dialog.IsPrimaryButtonEnabled = false;
            else
                dialog.IsPrimaryButtonEnabled = true;
        };
        dialog.Content = sp;
        var result = await dialog.ShowAsync();
        switch (result)
        {
            case ContentDialogResult.Primary:
                try
                {
                    EnvironmentVariablesHelper.SetVariable(variableTextBox.Text, valueTextBox.Text, EnvironmentVariablesHelper.EnvironmentScope.User);
                    ViewModel.UserVariables.Add(new EnvironmentVariable()
                    {
                        Variable = variableTextBox.Text,
                        Value = valueTextBox.Text
                    });
                }
                catch (Exception ex)
                {
                    await new ContentDialog()
                    {
                        Title = "Error",
                        Content = $"Failed to create environment variable: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = XamlRoot
                    }.ShowAsync();
                }
                break;
        }
    }
}
