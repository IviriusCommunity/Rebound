// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using WinUIEx;

namespace Rebound.Uninstaller;

public partial class App : Application
{
    public static WindowEx _window;

    public static bool canClose = true;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (Process.GetProcesses().Contains(Process.GetCurrentProcess())) Process.GetCurrentProcess().Close();

        _window = new MainWindow();
        _window.Activate();
    }
}
