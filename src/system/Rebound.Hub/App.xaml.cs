// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Rebound.Core;
using Rebound.Core.UI;
using Rebound.Core.UI.Windowing;
using Rebound.Hub.ViewModels;
using System;
using System.IO;
using Windows.UI;
using WinUIEx;

namespace Rebound.Hub;

public partial class App : Application
{
    private SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.Hub");

    internal static ReboundService ReboundService { get; } = new();

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UnhandledException += (s, e) =>
        {
            ReboundLogger.WriteToLog(
                "Unhandled Exception",
                "An unhandled exception was caught in the application.",
                LogMessageSeverity.Error,
                e.Exception);
            // The environment is too unstable for execution to continue
            Current.Exit();
        };

        SingleInstanceAppService.Launched += OnSingleInstanceLaunched;
        SingleInstanceAppService.Launch();
    }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (MainWindow != null)
            MainWindow.Activate();
        else
            CreateMainWindow();
    }

    public static void CreateMainWindow()
    {
        MainWindow = new MainWindow()
        {
            MinWidth = 640,
            MinHeight = 400,
            PersistenceId = "Rebound.Hub.MainWindow",
            Title = "Rebound Hub"
        };

        // Default window size
        if (WindowManager.PersistenceStorage?.TryGetValue("Rebound.Hub.MainWindow", out _) != true)
        {
            MainWindow.Width = 1000;
            MainWindow.Height = 800;
        }

        // Spawn the window
        MainWindow.Activate();

        // Window properties
        MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        MainWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        MainWindow.AppWindow.TitleBar.ButtonBackgroundColor =
        MainWindow.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        MainWindow.AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(80, 120, 120, 120);
        MainWindow.AppWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 120, 120, 120);
        MainWindow.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcons", "ReboundHub.ico"));

        ReboundWindowBackdrop.Register(MainWindow);
        ReboundWindowTheme.Register(MainWindow);
    }

    public static WindowEx? MainWindow { get; set; }
}