// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Rebound.ControlPanel.Views;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel;

[ReboundApp("Rebound.Control", "Legacy Control Panel*legacy*ms-appx:///Assets/ControlPanelLegacy.ico")]
public partial class App : Application
{
    public static PipeClient? ReboundPipeClient { get; private set; }

    internal struct CplEntry()
    {
        public object? Type { get; set; } = null;
        public List<string> Args { get; set; } = [];
    }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        try
        {
            if (e.IsFirstLaunch)
            {
                UIThreadQueue.QueueAction(async () =>
                {
                    // Spawn or activate the main window immediately
                    if (MainWindow != null)
                        MainWindow.BringToFront();
                    else
                        CreateMainWindow();
                });

                return;

                // Initialize pipe client if not already
                ReboundPipeClient ??= new();

                // Start listening (optional, for future messages)
                ReboundPipeClient.MessageReceived += OnPipeMessageReceived;

                // Service host watchdog thread
                var serviceHostWatchdogThread = new Thread(async () =>
                {
                    ServiceHostWatchdogEngine.Start();
                })
                {
                    IsBackground = true,
                    Name = "Service Host Watchdog Thread"
                };
                serviceHostWatchdogThread.SetApartmentState(ApartmentState.STA);
                serviceHostWatchdogThread.Start();

                // Pipe server thread
                var pipeThread = new Thread(async () =>
                {
                    try
                    {
                        await ReboundPipeClient.ConnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        UIThreadQueue.QueueAction(async () =>
                        {
                            await ReboundDialog.ShowAsync(
                                "Rebound Service Host not found.",
                                "Could not find Rebound Service Host.\nPlease ensure it is running in the background.",
                                DialogIcon.Warning
                            ).ConfigureAwait(false);
                        });
                    }
                })
                {
                    IsBackground = true,
                    Name = "Pipe Server Thread"
                };
                pipeThread.SetApartmentState(ApartmentState.STA);
                pipeThread.Start();
            }
            else
            {
                if (MainWindow != null)
                    MainWindow.BringToFront();
            }

            // Handle legacy launch
            if (e.Arguments == "legacy")
            {
                try
                {
                    await (ReboundPipeClient?.SendAsync("IFEOEngine::Pause#control.exe"))!.ConfigureAwait(false);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        UseShellExecute = true,
                    });

                    await (ReboundPipeClient?.SendAsync("IFEOEngine::Resume#control.exe"))!.ConfigureAwait(false);
                }
                catch
                {
                    UIThreadQueue.QueueAction(async () =>
                    {
                        await ReboundDialog.ShowAsync(
                            "Legacy Launch Failed",
                            "Could not communicate with Rebound Service Host.\nPlease ensure it is running and try again.",
                            DialogIcon.Warning
                        ).ConfigureAwait(false);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[Rebound About] Single instance launch error", ex);
        }

        var args = e.Arguments?.Trim() ?? string.Empty;
        var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var controlPanelPath = Path.Combine(systemFolder, "control.exe");
        var intlCplPath = Path.Combine(systemFolder, "intl.cpl");
        var appWizCplPath = Path.Combine(systemFolder, "appwiz.cpl");

        var knownArgMappings = new List<CplEntry>()
        {
            // Rebound Settings
            new()
            {
                Type = typeof(ReboundSettingsPage),
                Args = [ "reboundsettings", "/name Rebound.Settings" ]
            },

            // Home page
            new()
            {
                Type = typeof(HomePage),
                Args = [ controlPanelPath, "control", "" ]
            },

            // Date and Time
            new()
            {
                Type = typeof(WindowsToolsPage),
                Args = [ intlCplPath, $"{intlCplPath},,/p:date", $"{intlCplPath}," ]
            },
        };

        // Apps and Features
        if (SettingsManager.GetValue("InstallAppwiz", "control", true))
        {
            knownArgMappings.Add(
            new()
            {
                Type = "ms-settings:appsfeatures",
                Args = [appWizCplPath, $"{appWizCplPath},"]
            });
        }

        // Windows Tools
        if (SettingsManager.GetValue("InstallWindowsTools", "control", true))
        {
            knownArgMappings.Add(
            new()
            {
                Type = typeof(WindowsToolsPage),
                Args = ["admintools", "/name Microsoft.AdministrativeTools"]
            });
        }

        object? pageToLaunch = null;
        
        foreach (var mapping in knownArgMappings)
        {
            if (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "control.exe").ArgsMatchKnownEntries(mapping.Args, args))
            {
                pageToLaunch = mapping.Type;
                break;
            }
        }

        // Launch the legacy cpl if no known arguments match
        if (pageToLaunch == null || e.Arguments == "legacy")
        {
            await ReboundPipeClient!.SendAsync("IFEOEngine::Pause#control.exe").ConfigureAwait(false);
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                UseShellExecute = true,
                Arguments = e.Arguments == "legacy" ? string.Empty : args
            });
            return;
        }
        else
        {
            if (e.IsFirstLaunch)
            {
                if (string.IsNullOrEmpty(args) || pageToLaunch is Type)
                {
                    CreateMainWindow();

                    if (pageToLaunch is Type)
                        await LaunchPageOrUriAsync(pageToLaunch, args).ConfigureAwait(false);
                }
                else if (pageToLaunch is string uri)
                {
                    await Launcher.LaunchUriAsync(new Uri(uri));
                }
            }
            else
            {
                MainWindow?.BringToFront();

                if (string.IsNullOrEmpty(args) || pageToLaunch is Type or string)
                    await LaunchPageOrUriAsync(pageToLaunch, args).ConfigureAwait(false);
            }

            async Task LaunchPageOrUriAsync(object? target, string arguments)
            {
                await UIThreadQueue.QueueActionAsync(async () =>
                {
                    if (target is Type pageType)
                    {
                        var frame = (MainWindow?.Content as Frame)?.Content as RootPage;
                        if (frame?.RootFrame?.Content?.GetType() != pageType)
                        {
                            _ = frame?.RootFrame?.Navigate(pageType);
                        }
                    }
                    else if (target is string uri)
                    {
                        await Launcher.LaunchUriAsync(new Uri(uri));
                    }
                });
            }
        }
    }

    private static void OnPipeMessageReceived(string message)
    {

    }

    public static unsafe void CreateMainWindow()
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
            MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
            MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
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