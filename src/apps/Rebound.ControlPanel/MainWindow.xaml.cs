// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using WinUIEx;
using Rebound.Core.UI.Windowing;

namespace Rebound.ControlPanel;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        this.SetMica();
        RootFrame.Navigate(typeof(Views.RootPage));
    }
}
