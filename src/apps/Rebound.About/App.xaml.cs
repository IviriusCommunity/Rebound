// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Services;
using Rebound.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079
#pragma warning disable CA1515

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver*legacy*ms-appx:///Assets/Exe.ico")]
public partial class App : Application
{
    private static readonly List<IslandsWindow> _openWindows = new();

    public static PipeClient ReboundPipeClient { get; private set; }

    private static void RegisterWindow(IslandsWindow window)
    {
        _openWindows.Add(window);
        window.Closed += (s, e) =>
        {
            _openWindows.Remove(window);
            if (_openWindows.Count == 0)
            {
                Current.Exit();
                Process.GetCurrentProcess().Kill();
            }
        };
    }

    private async void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
    {
        Debug.WriteLine($"[App] ===== OnSingleInstanceLaunched ENTRY ===== Args={e.Arguments}, IsFirst={e.IsFirstLaunch}");
        Debug.WriteLine($"[App] Current Thread ID: {Thread.CurrentThread.ManagedThreadId}");
        Debug.WriteLine($"[App] Current Thread IsBackground: {Thread.CurrentThread.IsBackground}");

        try
        {
            Debug.WriteLine($"[App] About to call Program.QueueAction");
            Debug.WriteLine($"[App] ===== INSIDE QueueAction ===== Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            if (e.IsFirstLaunch)
            {
                // Initialize pipe client if not already
                ReboundPipeClient ??= new();

                // Start listening (optional, for future messages)
                ReboundPipeClient.MessageReceived += OnPipeMessageReceived;

                // Pipe server thread
                var pipeThread = new Thread(async () =>
                {
                    await ReboundPipeClient.ConnectAsync();
                })
                {
                    IsBackground = true,
                    Name = "Pipe Server Thread"
                };
                pipeThread.SetApartmentState(ApartmentState.STA);
                pipeThread.Start();

                Program.QueueAction(async () =>
                {
                    // Spawn or activate the main window immediately
                    if (MainWindow != null)
                    {

                    }
                    else
                        CreateMainWindow();
                });
            }

            // Handle legacy launch
            if (e.Arguments == "legacy")
            {
                Debug.WriteLine("Legacy launch");

                try
                {
                    await ReboundPipeClient.SendAsync("IFEOEngine::Pause#winver.exe");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "winver.exe",
                        UseShellExecute = true,
                    });
                }
                catch
                {
                    Program.QueueAction(async () =>
                    {
                        await ReboundDialog.ShowAsync(
                            "Legacy Launch Failed",
                            "Could not communicate with Rebound Service Host.\nPlease ensure it is running and try again.",
                            DialogIcon.Warning
                        );
                    });
                }
            }
            Debug.WriteLine($"[App] Program.QueueAction returned");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] EXCEPTION in OnSingleInstanceLaunched: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[App] StackTrace: {ex.StackTrace}");
        }

        Debug.WriteLine($"[App] ===== OnSingleInstanceLaunched EXIT =====");
    }

    private static void OnPipeMessageReceived(string message)
    {

    }

    public static unsafe void CreateMainWindow()
    {
        MainWindow = new()
        {
            IsPersistenceEnabled = true,
            PersistenceKey = "Rebound.About.MainWindow",
            PersistanceFileName = "winver"
        };

        RegisterWindow(MainWindow);

        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "About Windows";
            MainWindow.Width = 520;
            MainWindow.Height = 740;
            MainWindow.MinWidth = 520;
            MainWindow.MinHeight = 440;
            MainWindow.MaxWidth = 920;
            MainWindow.MaxHeight = 1000;
            MainWindow.X = (int)(50 * Display.GetScale(MainWindow.AppWindow));
            MainWindow.Y = (int)(50 * Display.GetScale(MainWindow.AppWindow));
            MainWindow.IsMaximizable = false;
            MainWindow.IsMinimizable = false;
            MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\AboutWindows.ico");
        };

        MainWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(Views.MainPage));
            MainWindow.Content = frame;
        };

        MainWindow.Create();
    }

    public static IslandsWindow? MainWindow { get; set; } = null;
}
