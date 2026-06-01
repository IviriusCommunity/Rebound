// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Rebound.ControlPanel.Views;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Core.UI.Application;
using Rebound.Core.UI.Windowing;
using Rebound.Forge.Engines;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI;
using WinUIEx;

#pragma warning disable CA1031

namespace Rebound.ControlPanel;

public partial class App : Application, IReboundLegacySupportApp, IReboundPipeClientApp
{
    private object? _validatedItem;

    public PipeClient? ReboundPipeClient { get; private set; }

    public static SingleInstanceAppService SingleInstanceAppService { get; } = new("Rebound.ControlPanel");

    public string LegacyExecutableName { get; } = "control.exe";

    public App() => InitializeComponent();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
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
        SingleInstanceAppService.Launch(); // no args - service reads them from AppInstance itself
    }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        try
        {
            // Extract the raw argument string from the activation payload
            var arguments = e.LaunchArguments;

            _validatedItem = ResolveFromArgs(arguments);

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

        DirectStartup:

            // Handle non-launch activation kinds (protocol, file, etc.)
            // directly without going through the legacy/page resolution path
            switch (e.ActivationArguments.Kind)
            {
                case ExtendedActivationKind.Protocol:
                    {
                        var protocol = (IProtocolActivatedEventArgs)e.ActivationArguments.Data;
                        // TODO: handle protocol activation (e.g. rebound-controlpanel://)
                        await HandleProtocolActivationAsync(protocol).ConfigureAwait(false);
                        return;
                    }
                case ExtendedActivationKind.File:
                    {
                        var file = (IFileActivatedEventArgs)e.ActivationArguments.Data;
                        // TODO: handle file activation
                        await HandleFileActivationAsync(file).ConfigureAwait(false);
                        return;
                    }
            }

            // Legacy launch
            // with no validated item
            if (_validatedItem == null)
            {
                LaunchLegacy(arguments);

                // The application itself shouldn't handle more logic from here
                return;
            }
            // with arguments
            if (arguments.StartsWith(Variables.LegacyLaunchArgument, StringComparison.InvariantCultureIgnoreCase)
                // without arguments
                || arguments.StartsWith(Variables.LegacyLaunchArgument.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                LaunchLegacy(arguments.Length > Variables.LegacyLaunchArgument.Length - 1 ?
                    // with arguments
                    arguments[Variables.LegacyLaunchArgument.Length..] :
                    // without arguments
                    string.Empty);

                // The application itself shouldn't handle more logic from here
                return;
            }

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

            // Launch validated item
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

    // Idk if we'll need this
    private static Task HandleProtocolActivationAsync(IProtocolActivatedEventArgs args)
    {
        return Task.CompletedTask;
    }

    // This one's definitely gonna be needed for .cpl files
    private static Task HandleFileActivationAsync(IFileActivatedEventArgs args)
    {
        return Task.CompletedTask;
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

    public async void RunServiceHostFailedToLaunchFallback()
    {
        // Request an action from the user
        var result = await ReboundDialog.ShowAsync(
            title: "Rebound Service Host not found.",
            content: "Could not find Rebound Service Host. This process is required for multiple features to work properly.",
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
                            title: "Couldn't launch Rebound Service Host.",
                            content: "Your Rebound installation might be corrupted. Please open Rebound Hub and check.",
                            closeButtonText: "Ok").ConfigureAwait(false);
                    }
                    break;
                }

            // Ok button
            default:
                break;
        }
    }

    private static async Task LaunchPageOrUriAsync(object? target)
    {
        // If it's a type (page), retrieve the root frame from the main window's content and navigate to that page
        if (target is Type pageType)
        {
            var frame = (App.MainWindow as MainWindow)?.RootFrame.Content as RootPage;
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
        try
        {
            MainWindow = new MainWindow
            {
                PersistenceId = "Rebound.ControlPanel.MainWindow",
                Title = "Control Panel"
            };

            // Spawn the window
            MainWindow.Activate();

            // Window properties
            MainWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow.TitleBar.ButtonBackgroundColor =
            MainWindow.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(80, 120, 120, 120);
            MainWindow.AppWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 120, 120, 120);
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\ControlPanel.ico");
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