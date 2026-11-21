// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using DesktopFlyouts;

using Rebound.Shell.Run;

namespace Rebound.Shell.Views;

public sealed partial class RunFlyout : DesktopFlyout
{
    public RunFlyout()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(RunWindow), true);
    }
}
