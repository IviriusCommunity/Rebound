// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Diagnostics;
using System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079
#pragma warning disable CA1515
#pragma warning disable CA1031

namespace Rebound.About;

[ReboundApp("Rebound.About")]
public partial class App : Application
{
    public static PipeClient? ReboundPipeClient { get; private set; }

    private async void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
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

                // Initialize pipe client if not already
                ReboundPipeClient ??= new();

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
                    await (ReboundPipeClient?.SendAsync("IFEOEngine::Pause#winver.exe"))!.ConfigureAwait(false);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "winver.exe",
                        UseShellExecute = true,
                    });

                    await (ReboundPipeClient?.SendAsync("IFEOEngine::Resume#winver.exe"))!.ConfigureAwait(false);
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
    }

    public static unsafe void CreateMainWindow()
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
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
            MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
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

    public static IslandsWindow? MainWindow { get; set; }
}