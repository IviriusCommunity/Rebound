// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.Settings;
using System.Collections.ObjectModel;

namespace Rebound.Shell.Run;

internal partial class RunViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunButtonEnabled))]
    public partial string Path { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RunAsAdmin { get; set; }

    public bool IsRunButtonEnabled => !string.IsNullOrEmpty(Path) && !string.IsNullOrWhiteSpace(Path); 

    public RunViewModel()
    {
        RunAsAdmin = SettingsManager.GetValue("RunAsAdmin", "rshell.run", false);
    }

    partial void OnRunAsAdminChanged(bool value) 
        => SettingsManager.SetValue("RunAsAdmin", "rshell.run", value);

    public ObservableCollection<string> RunHistory { get; } = [];
}