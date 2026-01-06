// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Rebound.Core;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Generators;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.UserAccountControlSettings;

[ReboundApp("Rebound.UACSettings", "Legacy User Account Control Settings*legacy*ms-appx:///Assets/ActionCenterLegacy.ico")]
public partial class App : Application
{
    public static PipeClient ReboundPipeClient { get; set; }

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

                // Initialize pipe client if not already
                ReboundPipeClient ??= new();

                // Start listening (optional, for future messages)
                ReboundPipeClient.MessageReceived += OnPipeMessageReceived;

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
            if (!string.IsNullOrWhiteSpace(e.Arguments) || e.Arguments.Trim() == "legacy")
            {
                try
                {
                    await ReboundPipeClient.SendAsync("IFEOEngine::Pause#useraccountcontrolsettings.exe").ConfigureAwait(false);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "winver.exe",
                        UseShellExecute = true,
                    });

                    await ReboundPipeClient.SendAsync("IFEOEngine::Resume#useraccountcontrolsettings.exe").ConfigureAwait(false);
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

    private static void OnPipeMessageReceived(string message)
    {

    }

    private static void CreateMainWindow()
    {
        UIThreadQueue.QueueAction(() =>
        {
            MainWindow = new()
            {
                Width = 680,
                Height = 480
            };
            MainWindow.AppWindowInitialized += (s, e) =>
            {
                MainWindow.IsMaximizable = false;
                MainWindow.IsResizable = false;
                MainWindow.IsPersistenceEnabled = false;
                MainWindow.Title = "User Account Control Settings";
                MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
                MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
                MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\Admin.ico");
                MainWindow.CenterWindow();
            };
            MainWindow.XamlInitialized += (s, e) =>
            {
                var frame = new Frame();
                frame.Navigate(typeof(Views.MainPage));
                MainWindow.Content = frame;
            };
            MainWindow.Create();
            MainWindow.CenterWindow();
            return Task.CompletedTask;
        });
    }

    public static IslandsWindow? MainWindow { get; set; }
}