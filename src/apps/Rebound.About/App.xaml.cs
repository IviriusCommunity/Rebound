// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Core.UI.Application;
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
                $"Hello",
                LogMessageSeverity.Error);

            // If this is the application's initial launch, handle the required environment
            if (e.IsFirstLaunch)
            {
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
                            throw new InvalidOperationException("Rebound Service Host couldn't be started.");

                        // Connect to it
                        await ReboundPipeClient.ConnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
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
                LaunchLegacy(e.Arguments.Length > Variables.LegacyLaunchArgument.Length - 1 ?
                    // with arguments
                    e.Arguments[Variables.LegacyLaunchArgument.Length..] :
                    // without arguments
                    string.Empty);

                // The application itself shouldn't handle more logic from here
                return;
            }

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
                "Rebound Service Host not found.",
                "Could not find Rebound Service Host. This process is required for the \"Legacy winver\" feature to work.",
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
                                "Couldn't launch Rebound Service Host.",
                                "Your Rebound installation might be corrupted. Please open Rebound Hub and check.",
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
                MainWindow.Title = "About Windows";

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