// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rebound.ControlPanel.Models;

public partial class EnvironmentVariableListItem(string value) : ObservableObject
{
    [ObservableProperty]
    public partial string Value { get; set; } = value;
}