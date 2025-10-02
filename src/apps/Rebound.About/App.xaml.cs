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

    private void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
    {
        Program._actions.Add(async () =>
        {
            /*if (e.IsFirstLaunch)
            {
                ReboundPipeClient = new ReboundPipeClient();
                await ReboundPipeClient.ConnectAsync();
            }*/

            if (e.Arguments == "legacy")
            {
                await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#winver.exe");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "winver.exe",
                    UseShellExecute = true,
                });
                return;
            }

            if (MainWindow != null)
                MainWindow.Activate();
            else
                CreateMainWindow();
        });
    }

    public static unsafe void CreateMainWindow()
    {
        MainWindow = new();
        RegisterWindow(MainWindow);
        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "About Windows";
            MainWindow.IsMaximizable = false;
            MainWindow.IsMinimizable = false;
            MainWindow.IsResizable = false;
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