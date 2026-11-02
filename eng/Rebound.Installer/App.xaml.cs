// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using WinUIEx;

#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.Installer;

public partial class App : Application
{
    public static WindowEx? MainWindow { get; set; }

    public static bool CanClose { get; set; } = true;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (Process.GetProcesses().Contains(Process.GetCurrentProcess())) Process.GetCurrentProcess().Close();

        MainWindow = new MainWindow();
        MainWindow.CenterOnScreen();
        MainWindow.Activate();
    }
}