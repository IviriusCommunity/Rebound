// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Services;
using Rebound.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver*legacy*ms-appx:///Assets/Exe.ico")]
public partial class App : Application
{
    private static readonly List<IslandsWindow> _openWindows = new();

    public static ReboundPipeClient ReboundPipeClient { get; set; }

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

    private void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e) => Program._actions.Add(async () =>
    {
        bool reboundAvailable = false;

        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new ReboundPipeClient();

            // Start connecting asynchronously, fire-and-forget
            _ = Task.Run(async () =>
            {
                try
                {
                    var connectTask = ReboundPipeClient.ConnectAsync();
                    var delayTask = Task.Delay(1000);

                    var completedTask = await Task.WhenAny(connectTask, delayTask);

                    if (completedTask == delayTask)
                    {
                        await ReboundDialog.ShowAsync(
                            "Rebound Service Host Not Found",
                            "Rebound Service Host does not appear to be running.\nPlease start it and try again.",
                            DialogIcon.Warning
                        );
                    }

                    await connectTask; // await to propagate exceptions if needed
                    reboundAvailable = true;
                }
                catch
                {
                    reboundAvailable = false;
                }
            });
        }

        // Spawn or activate the main window immediately
        if (MainWindow != null)
            MainWindow.Activate();
        else
            CreateMainWindow();

        // Handle legacy launch
        if (e.Arguments == "legacy")
        {
            if (!reboundAvailable)
            {
                // Warn the user immediately if Rebound is unavailable
                await ReboundDialog.ShowAsync(
                    "Cannot Use Legacy Launch",
                    "Legacy launch feature is disabled because the Rebound Service Host is not running.",
                    DialogIcon.Warning
                );
                return;
            }

            // Safe to use legacy feature
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#winver.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "winver.exe",
                UseShellExecute = true,
            });
        }
    });

    public static unsafe void CreateMainWindow()
    {
        MainWindow = new();
        MainWindow.IsPersistenceEnabled = true;
        MainWindow.PersistenceKey = "Rebound.About.MainWindow";
        MainWindow.PersistanceFileName = "winver";
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
            //MainWindow.IsResizable = false;
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
}