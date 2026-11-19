// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.UI;
using Rebound.Core.UI.Windowing;
using Rebound.Forge.Engines;
using Rebound.Shell.Run;

using System;
using System.Threading;

using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using WinUIEx;

namespace Rebound.Shell;

public partial class App : Application
{
    private SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.Shell");

    private PipeHost? ShellActionsPipeServer;

    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    private DispatcherQueue? _dispatcher;

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
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            var ipcThread = new Thread(() =>
            {
                ShellActionsPipeServer = new PipeHost(
                    "REBOUND_SHELL_ACTIONS",
                    AccessLevel.Everyone);
                ShellActionsPipeServer.MessageReceived += ShellActionsPipeServer_MessageReceived;
                _ = ShellActionsPipeServer.StartAsync();
            })
            {
                Name = "IPC Thread",
                IsBackground = true
            };
            ipcThread.SetApartmentState(ApartmentState.STA);
            ipcThread.Start();
            var serviceHostThread = new Thread(() =>
            {
                // Make sure Rebound Service Host exists
                var started = ServiceHostEngine.StartServiceHost();
                if (!started)
                {
                    ReboundLogger.WriteToLog(
                        "Rebound Environment Initialization",
                        "Could not initialize Rebound Service Host.",
                        LogMessageSeverity.Error);
                    throw new InvalidOperationException("Rebound Service Host couldn't be started.");
                }
            })
            {
                Name = "Service Host Thread",
                IsBackground = true
            };
            serviceHostThread.SetApartmentState(ApartmentState.STA);
            serviceHostThread.Start();
        }
    }

    private void ShellActionsPipeServer_MessageReceived(object? sender, HostMessageReceivedEventArgs args)
    {
        if (args.Message.StartsWith("RUN_DIALOG",
            StringComparison.OrdinalIgnoreCase))
            ShowRunWindow();
    }

    public unsafe void ShowRunWindow()
    {
        _dispatcher?.TryEnqueue(() =>
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
                var frame = new Frame();
                frame.Navigate(typeof(RunWindow));
                RunWindow.Content = frame;
                RunWindow.Closed += (s, e) => RunWindow = null;
                RunWindow.SystemBackdrop = new TransparentTintBackdrop();
                RunWindow.Show();
                BringToFront(RunWindow);

                ReboundWindowBackdrop.Register(RunWindow);
                ReboundWindowTheme.Register(RunWindow);
            }
        });
    }

    private static void BringToFront(WindowEx window)
    {
        HWND hwnd = (HWND)window.GetWindowHandle();

        PInvoke.AllowSetForegroundWindow((uint)Environment.ProcessId);
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(hwnd);
    }

    public static void CloseRunWindow()
        => RunWindow?.Close();

    public static WindowEx? RunWindow { get; set; }
}