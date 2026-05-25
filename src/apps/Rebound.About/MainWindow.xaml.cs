// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using WinUIEx;

namespace Rebound.About;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(Views.MainPage));
    }
}
