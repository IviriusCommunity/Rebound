// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.ControlPanel.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rebound.ControlPanel.ViewModels;

public partial class EditEnvVarDialogViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string RawValue { get; set; }

    [ObservableProperty]
    public partial string NewPathInput { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial EnvironmentVariableListItem? SelectedPathItem { get; set; }

    public bool IsListMode { get; }

    internal ObservableCollection<EnvironmentVariableListItem> PathItems { get; } = [];

    public EditEnvVarDialogViewModel(string name, string value)
    {
        Name = name;
        RawValue = value ?? string.Empty;
        IsListMode = RawValue.Contains(';', StringComparison.InvariantCultureIgnoreCase);

        if (IsListMode)
        {
            var parts = RawValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                PathItems.Add(new EnvironmentVariableListItem(part));
            }
        }
    }

    [RelayCommand]
    private void AddPath()
    {
        if (string.IsNullOrWhiteSpace(NewPathInput)) return;

        PathItems.Add(new EnvironmentVariableListItem(NewPathInput.Trim()));
        NewPathInput = string.Empty;
    }

    [RelayCommand]
    internal void RemovePath(EnvironmentVariableListItem? item)
    {
        if (item != null)
        {
            PathItems.Remove(item);
        }
    }

    public string GetFinalValue()
    {
        if (!IsListMode) return RawValue.Trim();

        return string.Join(";", PathItems
            .Select(x => x.Value.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name)) return false;
        if (IsListMode && !PathItems.Any(x => !string.IsNullOrWhiteSpace(x.Value))) return false;
        return true;
    }
}