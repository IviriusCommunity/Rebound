// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Core.UI.Localizer;
using Rebound.Core.UI.Windowing;
using Rebound.Forge;
using Rebound.Forge.Engines;

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TerraFX.Interop.Windows;

using Windows.UI;

using WinUIEx;

namespace Rebound.Cleanup;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

public partial class App : Application
{
    public PipeClient? ReboundPipeClient { get; private set; }

    public static SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.Cleanup");

    public string LegacyExecutableName { get; } = "cleanmgr.exe";

    public static DispatcherQueue? UIDispatcher { get; private set; }

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
            //Current.Exit();
        };

        SingleInstanceAppService.Launched += OnSingleInstanceLaunched;
        SingleInstanceAppService.Launch();
    }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "Application Launch",
                "The application is starting.",
                LogMessageSeverity.Message);

            // Set locale
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.Empty;

            var isReboundInstalled = await ReboundPresenceEngine.IsReboundInstalledAsync().ConfigureAwait(true);

            // Check if Rebound is installed or not
            if (!isReboundInstalled)
            {
                ReboundLogger.WriteToLog(
                    "Rebound Installation Check",
                    "Rebound is not installed. The app will continue execution as undocked.",
                    LogMessageSeverity.Message);

                goto DirectStartup;
            }

            // If this is the application's initial launch, handle the required environment
            if (e.IsFirstLaunch)
            {
                UIDispatcher = DispatcherQueue.GetForCurrentThread()
                    ?? throw new InvalidOperationException(
                        "The WinUI UI DispatcherQueue is unavailable.");

                ReboundLogger.WriteToLog(
                    "Rebound Environment Initialization",
                    "Initializing the Rebound environment for the current app...",
                    LogMessageSeverity.Message);

                var ipcThread = new Thread(async () =>
                {
                    try
                    {
                        // Initialize pipe client if not already initialized
                        ReboundPipeClient ??= new("REBOUND_LAUNCHER");

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

                        // Connect to it
                        await ReboundPipeClient.ConnectAsync().ConfigureAwait(true);
                    }
                    catch
                    {
                        ReboundLogger.WriteToLog(
                            "Rebound Environment Initialization",
                            "Notifying the user of Rebound Service Host failure.",
                            LogMessageSeverity.Message);
                    }
                })
                {
                    Name = "IPC Thread",
                    IsBackground = true
                };
                ipcThread.SetApartmentState(ApartmentState.STA);
                ipcThread.Start();

                ReboundLogger.WriteToLog(
                    "Rebound Environment Initialization",
                    "Initializing the Rebound Service Host watchdog...",
                    LogMessageSeverity.Message);
            }

            // Legacy launch
            if (!string.IsNullOrWhiteSpace(e.LaunchArguments))
            {
                var trimmedArgs = e.LaunchArguments.Length > Variables.LegacyLaunchArgument.Length - 1 ?
                    // with arguments
                    e.LaunchArguments[Variables.LegacyLaunchArgument.Length..] :
                    // without arguments
                    string.Empty;

                ReboundLogger.WriteToLog(
                    "Legacy Launch",
                    $"Launching the legacy app with arguments (raw: {e.LaunchArguments}) (trimmed: {trimmedArgs}).",
                    LogMessageSeverity.Message);

                LaunchLegacy(trimmedArgs);

                // The application itself shouldn't handle more logic from here
                return;
            }

        DirectStartup:

            ReboundLogger.WriteToLog(
                "Application Launch",
                "Displaying the UI...",
                LogMessageSeverity.Message);

            // If this is the application's initial launch and not a legacy launch, handle the UI
            if (e.IsFirstLaunch)
            {
                // Spawn or activate the main window immediately
                if (MainWindow != null)
                    MainWindow.BringToFront();
                else
                    CreateMainWindow();
            }

            // The application has been launched again, simply bring the main window forward
            else MainWindow!.BringToFront();
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Application Launch",
                "Single instance launcher error - the application couldn't be launched.",
                LogMessageSeverity.Error,
                ex);

            // The environment is too unstable for execution to continue
            Current.Exit();
        }
    }

    public void LaunchLegacy(string args)
    {
        Task.Run(async () =>
        {
            try
            {
                if (Process.GetProcessesByName("Rebound.ServiceHost").ToList().Count > 0)
                {
                    await (ReboundPipeClient?.ConnectAsync())!.ConfigureAwait(true);
                    await (ReboundPipeClient?.SendAsync($"LAUNCH:{LegacyExecutableName}\\lnbrk\\{Convert.ToBase64String(Encoding.UTF8.GetBytes(args))}"))!.ConfigureAwait(true);
                }
                else StartDirectly();
            }
            catch
            {
                StartDirectly();
            }
        });

        void StartDirectly()
        {
            // Rebound Service Host doesn't exist, launch directly
            Process.Start(new ProcessStartInfo()
            {
                FileName = LegacyExecutableName,
                Arguments = args,
                UseShellExecute = true,
                Verb = ProcessInformation.IsRunningAsAdmin() ? "runas" : string.Empty
            });
        }
    }

    public static void CreateMainWindow()
    {
        try
        {
            MainWindow = new MainWindow
            {
                MinWidth = 400,
                MinHeight = 600,
                PersistenceId = "Rebound.Cleanup.MainWindow",
                Title = "Disk Cleanup"
            };

            // Default window size
            if (WindowManager.PersistenceStorage?.TryGetValue("Rebound.Cleanup.MainWindow", out _) != true)
            {
                MainWindow.Width = 400;
                MainWindow.Height = 640;
            }

            // Spawn the window
            MainWindow.Activate();

            // Window properties
            MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow.TitleBar.ButtonBackgroundColor =
            MainWindow.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(80, 120, 120, 120);
            MainWindow.AppWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 120, 120, 120);
            MainWindow.AppWindow.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\cleanmgr.ico");

            ReboundWindowBackdrop.Register(MainWindow);
            ReboundWindowTheme.Register(MainWindow);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "Application Launch",
                "Window creation error - the application couldn't create a main window.",
                LogMessageSeverity.Error,
                ex);

            // The environment is too unstable for execution to continue
            Current.Exit();
        }
    }

    public static WindowEx? MainWindow { get; set; }
}