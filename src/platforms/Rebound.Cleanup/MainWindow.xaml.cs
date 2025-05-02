// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Rebound.Cleanup.Views;
using Rebound.Helpers.Windowing;
using WinUIEx;

namespace Rebound.Cleanup;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "cleanmgr.ico"));
        this.Move(25, 25);
        RootFrame.Navigate(typeof(MainPage));
    }
}