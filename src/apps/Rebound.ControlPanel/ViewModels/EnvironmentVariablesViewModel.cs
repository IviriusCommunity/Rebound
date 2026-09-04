// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Environment;
using Rebound.Core.Native.Helpers;
using System.Collections.ObjectModel;

namespace Rebound.ControlPanel.ViewModels;

internal class EnvironmentVariable
{
    public string Variable { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

internal partial class EnvironmentVariablesViewModel : ObservableObject
{
    public ObservableCollection<EnvironmentVariable> UserVariables = [];

    public ObservableCollection<EnvironmentVariable> SystemVariables = [];

    [ObservableProperty] public partial int SelectedUserVariable { get; set; } = 0;

    [ObservableProperty] public partial int SelectedSystemVariable { get; set; } = 0;

    [ObservableProperty] public partial bool IsAdmin { get; set; }

    public EnvironmentVariablesViewModel()
    { 
        IsAdmin = ApplicationEnvironment.IsRunningAsAdmin();
        UserVariables.CollectionChanged += CollectionChanged;
        SystemVariables.CollectionChanged += CollectionChanged;
        RefreshVariables();
    }

    private void CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (SelectedUserVariable < 0 || SelectedUserVariable >= UserVariables.Count)
            SelectedUserVariable = 0;
        if (SelectedSystemVariable < 0 || SelectedSystemVariable >= SystemVariables.Count)
            SelectedSystemVariable = 0;
    }

    partial void OnSelectedUserVariableChanged(int value)
    {
        if (SelectedUserVariable < 0 || SelectedUserVariable >= UserVariables.Count)
            SelectedUserVariable = 0;
    }

    partial void OnSelectedSystemVariableChanged(int value)
    {
        if (SelectedSystemVariable < 0 || SelectedSystemVariable >= SystemVariables.Count)
            SelectedSystemVariable = 0;
    }

    private void RefreshVariables()
    {
        UserVariables.Clear();
        SystemVariables.Clear();

        var userVariables = EnvironmentVariablesHelper.EnumerateVariables(EnvironmentScope.User);
        foreach (var variable in userVariables)
        {
            UserVariables.Add(new()
            {
                Value = variable.Value,
                Variable = variable.Key
            });
        }

        var systemVariables = EnvironmentVariablesHelper.EnumerateVariables(EnvironmentScope.System);
        foreach (var variable in systemVariables)
        {
            SystemVariables.Add(new()
            {
                Value = variable.Value,
                Variable = variable.Key
            });
        }
    }
}