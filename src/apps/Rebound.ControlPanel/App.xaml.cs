// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.ControlPanel.Views;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Core.UI.Application;
using Rebound.Core.UI.Threading;
using Rebound.Core.UI.Windowing;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079
#pragma warning disable CA1515
#pragma warning disable CA1031

namespace Rebound.ControlPanel;

[ReboundApp("Rebound.Control")]
public partial class App : Application, IReboundLegacySupportApp, IReboundPipeClientApp
{
    private object? _validatedItem;

    public PipeClient? ReboundPipeClient { get; private set; }

    public string LegacyExecutableName { get; } = "control.exe";

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        try
        {
            _validatedItem = ResolveFromArgs(e.Arguments);

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
            // with no validated item
            if (_validatedItem == null)
            {
                LaunchLegacy(e.Arguments);

                // The application itself shouldn't handle more logic from here
                return;
            }
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

            // Launch validated item, if any
            if (_validatedItem != null)
                await LaunchPageOrUriAsync(_validatedItem).ConfigureAwait(false);
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

    private static object? ResolveFromArgs(string arguments)
    {
        return SearchArgs(CplItemPairs.CplItems, arguments.Trim());
    }

    private static object? SearchArgs(IEnumerable<CplItem> items, string arguments)
    {
        foreach (var item in items)
        {
            foreach (var arg in item.Args)
            {
                bool isMatch = string.IsNullOrEmpty(arg)
                    ? string.IsNullOrEmpty(arguments)
                    : arguments.Contains(arg.Trim(), StringComparison.InvariantCultureIgnoreCase);

                if (isMatch)
                    return item.Page ?? (object?)item.Uri;
            }

            var result = SearchArgs(item.Children, arguments);
            if (result != null)
                return result;
        }
        return null;
    }

    public void RunServiceHostFailedToLaunchFallback()
    {
        UIThread.QueueAction(async () =>
        {
            // Request an action from the user
            var result = await ReboundDialog.ShowAsync(
                "Rebound Control Panel",
                "Rebound Service Host not found.",
                "Could not find Rebound Service Host. This process is required for multiple features to work properly.",
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
                                "Rebound Control Panel",
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

    private static async Task LaunchPageOrUriAsync(object? target)
    {
        await UIThread.QueueActionAsync(async () =>
        {
            // If it's a type (page), retrieve the root frame from the main window's content and navigate to that page
            if (target is Type pageType)
            {
                var frame = (MainWindow?.Content as Frame)?.Content as RootPage;
                if (frame?.RootFrame?.Content?.GetType() != pageType)
                {
                    _ = frame?.RootFrame?.Navigate(pageType);
                }
            }
            // If it's a URI, launch it
            else if (target is string uri)
            {
                await Launcher.LaunchUriAsync(new Uri(uri));
            }
        }).ConfigureAwait(false);
    }

    public void LaunchLegacy(string args)
        => LaunchLegacy(LegacyExecutableName, args);

    public void LaunchLegacy(string executable, string args)
    {
        Task.Run(async () =>
        {
            try
            {
                // Disable the IFEO entry via Rebound Service Host
                await (ReboundPipeClient?.SendAsync($"IFEOEngine::Pause#{executable}"))!.ConfigureAwait(false);

                // Launch the original application
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = true,
                    Arguments = args
                });

                // Resume the IFEO entry
                await (ReboundPipeClient?.SendAsync($"IFEOEngine::Resume#{executable}"))!.ConfigureAwait(false);
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
        // Create the window
        MainWindow = new()
        {
            IsPersistenceEnabled = true,
            PersistenceKey = "Rebound.ControlPanel.MainWindow",
            PersistenceFileName = "control",
        };

        // AppWindow init
        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "Control Panel";
            MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = IslandsWindow.ButtonHoverBackgroundColor;
            MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = IslandsWindow.ButtonPressedBackgroundColor;
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\ControlPanel.ico");
        };

        // Load main page
        MainWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(RootPage));
            MainWindow.Content = frame;
        };

        // Spawn the window
        MainWindow.Create();
    }

    public static IslandsWindow? MainWindow { get; set; }
}