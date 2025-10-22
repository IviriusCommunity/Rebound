// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Core.Helpers.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Colors = Windows.UI.Colors;

namespace Rebound.Hub;

[ReboundApp("Rebound.Hub", "")]
public partial class App : Application
{
    private void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
    {
        Program.QueueAction(async () =>
        {
            if (MainWindow != null)
                MainWindow.Activate();
            else
                CreateMainWindow();
        });
    }

    public static unsafe void CreateMainWindow()
    {
        MainWindow = new();
        
        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "Rebound Hub";
            MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow?.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\AppIcons\\ReboundHub.ico");
        };
        MainWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(Views.ShellPage));
            MainWindow.Content = frame;
        };
        MainWindow.Create();
    }

    public static IslandsWindow MainWindow { get; set; }
}