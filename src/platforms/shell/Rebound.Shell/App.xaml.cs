// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.UI;
using Rebound.Shell.Run;
using System;
using System.Diagnostics;
using Windows.Graphics;
using WinUIEx;

namespace Rebound.Shell;

public partial class App : Application
{
    private SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.Shell");

    private PipeHost? ShellActionsPipeServer;

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
            BackgroundWindow = new WindowEx();
            BackgroundWindow.AppWindow.IsShownInSwitchers = false;
            BackgroundWindow.MoveAndResize(0, 0, 0, 0);
            BackgroundWindow.Minimize();
            BackgroundWindow.ToggleWindowStyle(false, WindowStyle.Visible);
            BackgroundWindow.ToggleExtendedWindowStyle(true, ExtendedWindowStyle.ToolWindow);
            BackgroundWindow.Show();
            ShellActionsPipeServer = new PipeHost(
                "REBOUND_SHELL_ACTIONS",
                AccessLevel.Everyone);
            ShellActionsPipeServer.MessageReceived += ShellActionsPipeServer_MessageReceived;
        }
        else Process.GetCurrentProcess().Kill();
    }

    private void ShellActionsPipeServer_MessageReceived(object? sender, HostMessageReceivedEventArgs args)
    {
        if (args.Message.StartsWith("RUN_DIALOG",
            StringComparison.OrdinalIgnoreCase)) 
            ShowRunWindow();
    }

    public static unsafe void ShowRunWindow()
    {
        if (RunWindow != null)
            RunWindow.BringToFront();
        else
        {
            RunWindow = new()
            {
                Title = "Run",
                IsMaximizable = false,
                IsMinimizable = false,
                IsResizable = false
            };

            var hwnd = RunWindow.GetWindowHandle();
            var workArea = Display.GetDisplayWorkArea(new((void*)hwnd));

            float scale = HwndExtensions.GetDpiForWindow(hwnd) / 96f;

            const int width = 450;
            const int height = 240;
            const int margin = 25;

            RunWindow.AppWindow?.MoveAndResize(
                new RectInt32(
                    (int)((workArea.left / scale + margin) * scale),
                    (int)((workArea.bottom / scale - height - margin) * scale),
                    (int)(width * scale),
                    (int)(height * scale)));

            RunWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Applets\\RunBox.ico");
            RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            RunWindow.AppWindow?.Closing += (sender, args) =>
            {
                RunWindow = null;
            };
            var frame = new Frame();
            frame.Navigate(typeof(RunWindow));
            RunWindow.Content = frame;
            RunWindow.Show();
        }
    }

    public static void CloseRunWindow()
    {
        RunWindow?.Close();
    }

    public static WindowEx? RunWindow { get; set; }
    public static WindowEx? BackgroundWindow { get; set; }
}