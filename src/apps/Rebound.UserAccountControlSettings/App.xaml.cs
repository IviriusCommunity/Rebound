// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Generators;
using System;
using System.Diagnostics;
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
        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new();
            await ReboundPipeClient.ConnectAsync().ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(e.Arguments) || e.Arguments.Trim() == "legacy")
        {
            await ReboundPipeClient.SendAsync("IFEOEngine::Pause#useraccountcontrolsettings.exe").ConfigureAwait(false);
            Process.Start(new ProcessStartInfo
            {
                FileName = "useraccountcontrolsettings.exe",
                UseShellExecute = true,
                Arguments = e.Arguments == "legacy" ? string.Empty : e.Arguments
            });
            await ReboundPipeClient.SendAsync("IFEOEngine::Resume#useraccountcontrolsettings.exe").ConfigureAwait(false);
            return;
        }

        if (e.IsFirstLaunch)
        {
            UIThreadQueue.QueueAction(() =>
            {
                MainAppWindow = new()
                {
                    Width = 680,
                    Height = 480
                };
                MainAppWindow.AppWindowInitialized += (s, e) =>
                {
                    MainAppWindow.IsMaximizable = false;
                    MainAppWindow.IsResizable = false;
                    MainAppWindow.IsPersistenceEnabled = false;
                    MainAppWindow.Title = "User Account Control Settings";
                    MainAppWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                    MainAppWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                    MainAppWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
                    MainAppWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
                    MainAppWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    MainAppWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\Admin.ico");
                    MainAppWindow.CenterWindow();
                };
                MainAppWindow.XamlInitialized += (s, e) =>
                {
                    var frame = new Frame();
                    frame.Navigate(typeof(Views.MainPage));
                    MainAppWindow.Content = frame;
                };
                MainAppWindow.Create();
                MainAppWindow.CenterWindow();
                return Task.CompletedTask;
            });
        }
        else
        {
            UIThreadQueue.QueueAction(() =>
            {
                MainAppWindow?.BringToFront();
                return Task.CompletedTask;
            });
        }
    }

    public static IslandsWindow? MainAppWindow { get; set; }
}