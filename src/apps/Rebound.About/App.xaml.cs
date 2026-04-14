// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Core.UI.Application;
using Rebound.Core.UI.Localizer;
using Rebound.Core.UI.Threading;
using Rebound.Core.UI.Windowing;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079
#pragma warning disable CA1515
#pragma warning disable CA1031

namespace Rebound.About;

[ReboundApp("Rebound.About")]
public partial class App : Application, IReboundLegacySupportApp, IReboundPipeClientApp
{
    public PipeClient? ReboundPipeClient { get; private set; }

    public string LegacyExecutableName { get; } = "winver.exe";

    private async void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
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
            if (e.Arguments.StartsWith(Variables.LegacyLaunchArgument, StringComparison.InvariantCultureIgnoreCase)
                // without arguments
                || e.Arguments.StartsWith(Variables.LegacyLaunchArgument.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                var trimmedArgs = e.Arguments.Length > Variables.LegacyLaunchArgument.Length - 1 ?
                    // with arguments
                    e.Arguments[Variables.LegacyLaunchArgument.Length..] :
                    // without arguments
                    string.Empty;

                ReboundLogger.WriteToLog(
                    "Legacy Launch",
                    $"Launching the legacy app with arguments (raw: {e.Arguments}) (trimmed: {trimmedArgs}).",
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
                UIThread.QueueAction(async () =>
                {
                    if (MainWindow != null)
                        MainWindow.BringToFront();
                    else
                        CreateMainWindow();
                });
            }

            // The application has been launched again, simply bring the main window forward
            else
                UIThread.QueueAction(MainWindow!.BringToFront);
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

    public void RunServiceHostFailedToLaunchFallback()
    {
        UIThread.QueueAction(async () =>
        {
            // Request an action from the user
            var result = await ReboundDialog.ShowAsync(
                "Rebound About",
                LocalizedResource.GetLocalizedString("ServiceHostNotFound"),
                LocalizedResource.GetLocalizedString("ServiceHostNotFoundInfo"),
                [
                    new("Launch", true, '\uEA18'),
                    new("Ok", false)
                ],
                DialogIcon.Warning
                ).ConfigureAwait(false);
            switch (result)
            {
                // Button index 0 (Launch)
                case 0:
                    {
                        // Make sure Rebound Service Host exists
                        var launched = ServiceHostEngine.StartServiceHost();

                        // If it still doesn't, it's possible that Rebound is corrupted - inform the user
                        if (!launched)
                        {
                            await ReboundDialog.ShowAsync(
                                "Rebound About",
                                LocalizedResource.GetLocalizedString("CouldntLaunchServiceHost"),
                                LocalizedResource.GetLocalizedString("CouldntLaunchServiceHostInfo"),
                                null,
                                DialogIcon.Warning
                                ).ConfigureAwait(false);
                        }
                        break;
                    }

                // Ok and close buttons
                default:
                    break;
            }
        });
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
            // Create the window
            MainWindow = new()
            {
                IsPersistenceEnabled = true,
                PersistenceKey = "Rebound.About.MainWindow",
                PersistenceFileName = "winver",
                Width = 520,
                Height = 740,
                X = 50,
                Y = 50
            };

            // AppWindow init
            MainWindow.AppWindowInitialized += (s, e) =>
            {
                // Window metrics
                MainWindow.MinWidth = 440;
                MainWindow.MinHeight = 360;

                // Window properties
                MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor =
                MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = IslandsWindow.ButtonHoverBackgroundColor;
                MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = IslandsWindow.ButtonPressedBackgroundColor;
                MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\AboutWindows.ico");
            };

            // Load main page
            MainWindow.XamlInitialized += (s, e) =>
            {
                MainWindow.Title = LocalizedResource.GetLocalizedString("WindowTitle");

                var frame = new Frame();
                frame.Navigate(typeof(Views.MainPage));
                MainWindow.Content = frame;
            };

            // Spawn the window
            MainWindow.Create();
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

    public static IslandsWindow? MainWindow { get; set; }
}