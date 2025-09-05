// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.UI.Xaml.Media;
using Rebound.About.Views;
using Rebound.Helpers;
using Rebound.Helpers.Windowing;
using WinUIEx;

namespace Rebound.About;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        this.Move(25, 25);
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AboutWindows.ico"));
        this.TurnOffDoubleClick();
        ExtendsContentIntoTitleBar = true;
        RootFrame.Navigate(typeof(MainPage));
    }
}