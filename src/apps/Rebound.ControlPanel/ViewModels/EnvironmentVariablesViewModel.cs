// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Native.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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

    public EnvironmentVariablesViewModel()
    {
        RefreshVariables();
    }

    private void RefreshVariables()
    {
        UserVariables.Clear();
        SystemVariables.Clear();

        var userVariables = EnvironmentVariablesHelper.EnumerateVariables(EnvironmentVariablesHelper.EnvironmentScope.User);
        foreach (var variable in userVariables)
        {
            UserVariables.Add(new()
            {
                Value = variable.Value,
                Variable = variable.Key
            });
        }

        var systemVariables = EnvironmentVariablesHelper.EnumerateVariables(EnvironmentVariablesHelper.EnvironmentScope.System);
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