// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Core.UI.Localizer;
using Rebound.Core.UI.Windowing;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using WinUIEx;

namespace Rebound.Cleanup;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

public partial class App : Application
{
    public PipeClient? ReboundPipeClient { get; private set; }

    private SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.Cleanup");

    public string LegacyExecutableName { get; } = "cleanmgr.exe";

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

            // Check if Rebound is installed or not
            if (!ReboundPresenceEngine.IsReboundInstalled())
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
                ReboundLogger.WriteToLog(
                    "Rebound Environment Initialization",
                    "Initializing the Rebound environment for the current app...",
                    LogMessageSeverity.Message);

                // Pipe server thread
                var pipeThread = new Thread(async () =>
                {
                    try
                    {
                        // Initialize pipe client if not already initialized
                        ReboundPipeClient ??= new();

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
                        await ReboundPipeClient.ConnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        ReboundLogger.WriteToLog(
                            "Rebound Environment Initialization",
                            "Notifying the user of Rebound Service Host failure.",
                            LogMessageSeverity.Message);

                        // Rebound Service Host doesn't exist, fall back to UI solutions
                        RunServiceHostFailedToLaunchFallback();
                    }
                })
                {
                    IsBackground = true,
                    Name = "Pipe Server Thread"
                };
                pipeThread.SetApartmentState(ApartmentState.STA);
                pipeThread.Start();

                ReboundLogger.WriteToLog(
                    "Rebound Environment Initialization",
                    "Initializing the Rebound Service Host watchdog...",
                    LogMessageSeverity.Message);

                // Service host watchdog thread
                // This ensures Rebound Service Host can't die randomly
                var watchdogThread = new Thread(ServiceHostEngine.StartWatchdog)
                {
                    IsBackground = true,
                    Name = "Service Host Watchdog Thread"
                };
                watchdogThread.SetApartmentState(ApartmentState.STA);
                watchdogThread.Start();
            }

            // Legacy launch
            // with arguments
            if (e.LaunchArguments.StartsWith(Variables.LegacyLaunchArgument, StringComparison.InvariantCultureIgnoreCase)
                // without arguments
                || e.LaunchArguments.StartsWith(Variables.LegacyLaunchArgument.Trim(), StringComparison.InvariantCultureIgnoreCase))
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

    public async void RunServiceHostFailedToLaunchFallback()
    {
    /*
        // Request an action from the user
        var result = await ReboundDialog.ShowAsync(
            title: LocalizedResource.GetLocalizedString("ServiceHostNotFound"),
            content: LocalizedResource.GetLocalizedString("ServiceHostNotFoundInfo"),
            primaryButtonText: "Launch",
            closeButtonText: "Ok",
            defaultButton: ContentDialogButton.Primary).ConfigureAwait(false);

        switch (result)
        {
            // Launch
            case ContentDialogResult.Primary:
                {
                    // Make sure Rebound Service Host exists
                    var launched = ServiceHostEngine.StartServiceHost();

                    // If it still doesn't, it's possible that Rebound is corrupted - inform the user
                    if (!launched)
                    {
                        await ReboundDialog.ShowAsync(
                            title: LocalizedResource.GetLocalizedString("CouldntLaunchServiceHost"),
                            content: LocalizedResource.GetLocalizedString("CouldntLaunchServiceHostInfo"),
                            closeButtonText: "Ok").ConfigureAwait(false);
                    }
                    break;
                }

            // Ok button
            default:
                break;
        }
        */
    }

    public void LaunchLegacy(string args)
    {
        Task.Run(async () =>
        {
            try
            {
                // Disable the IFEO entry via Rebound Service Host
                await (ReboundPipeClient?.SendAsync($"IFEOEngine::Pause#{LegacyExecutableName}"))!.ConfigureAwait(false);

                // Launch the original application
                Process.Start(new ProcessStartInfo
                {
                    FileName = LegacyExecutableName,
                    UseShellExecute = true,
                    Arguments = args
                });

                // Resume the IFEO entry
                await (ReboundPipeClient?.SendAsync($"IFEOEngine::Resume#{LegacyExecutableName}"))!.ConfigureAwait(false);
            }
            catch
            {
                // Rebound Service Host doesn't exist, fall back to UI solutions
                RunServiceHostFailedToLaunchFallback();
            }
        });
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
                MainWindow.Height = 600;
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

    //public static ReboundPipeClient ReboundPipeClient { get; set; }

    /*private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new ReboundPipeClient();
            await ReboundPipeClient.ConnectAsync();
        }

        if (!Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe").ArgsMatchKnownEntries([string.Empty], e.Arguments))
        {
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#cleanmgr.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cleanmgr.exe",
                UseShellExecute = true,
                Arguments = e.Arguments == "legacy" ? string.Empty : e.Arguments
            });
            return;
        }

        if (e.IsFirstLaunch)
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }*/
}