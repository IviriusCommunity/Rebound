// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Core.UI;
using Rebound.Forge.Engines;
using Rebound.ServiceHost.Modules;
using System;
using System.IO;
using WinUIEx;

namespace Rebound.ServiceHost;

public partial class App : Application
{
    private SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.ServiceHost");

    private static DLLInjectionModule DlLInjectionModule { get; } = new();

    private static LauncherModule LauncherModule { get; } = new();

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
        if (e.IsFirstLaunch)
        {
            var mainWindow = new WindowEx()
            {
                Title = "Rebound",
                IsVisibleInTray = true,
            };
            mainWindow.AppWindow.IsShownInSwitchers = false;
            mainWindow.MoveAndResize(0, 0, 0, 0);
            mainWindow.Minimize();
            mainWindow.ToggleWindowStyle(false, WindowStyle.Visible);
            mainWindow.ToggleExtendedWindowStyle(true, ExtendedWindowStyle.ToolWindow);
            mainWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Rebound.ico"));
            mainWindow.Show();
            WindowManager.Get(mainWindow).TrayIconSelected += App_TrayIconSelected;
            WindowManager.Get(mainWindow).TrayIconContextMenu += App_TrayIconContextMenu;

            DlLInjectionModule.Start();
            LauncherModule.Start();
        }
    }

    private void App_TrayIconSelected(WindowManager sender, TrayIconEventArgs args) => args.Handled = true;

    private void App_TrayIconContextMenu(WindowManager sender, TrayIconEventArgs args)
    {
        var flyout = new MenuFlyout();

        var openItem = new MenuFlyoutItem
        {
            Text = "Open Rebound Hub",
            Icon = new FontIcon { Glyph = "\uE80F" }
        };
        openItem.Click += (_, _) => ApplicationLaunchEngine.LaunchApp("Rebound.Hub_rcz2tbwv5qzb8");
        flyout.Items.Add(openItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem
        {
            Text = "Exit Rebound",
            Icon = new FontIcon { Glyph = "\uE711" }
        };
        exitItem.Click += (_, _) => Exit();
        flyout.Items.Add(exitItem);

        args.Flyout = flyout;
    }
}