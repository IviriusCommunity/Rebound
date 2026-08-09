// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.ControlPanel.ViewModels;

internal partial class RootViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsReboundInstalled { get; set; }

    [ObservableProperty] public partial bool CanGoBack { get; set; }

    [ObservableProperty] public partial bool CanGoForward { get; set; }

    [ObservableProperty] public partial bool CollapseLeftItems { get; set; }

    [ObservableProperty] public partial bool CollapseRightItems { get; set; }

    [ObservableProperty] public partial string Username { get; set; }

    [ObservableProperty] public partial string PageAddress { get; set; }

    [ObservableProperty] public partial bool IsMicrosoftAccount { get; set; }
}