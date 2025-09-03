// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Helpers;
using Rebound.Shell.Desktop;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

//[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    public App()
    {
        Run();
    }

    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void Run()
    {
        ReboundPipeClient = new ReboundPipeClient();
        await ReboundPipeClient.ConnectAsync();

        ReboundPipeClient.StartListening(async (msg) =>
        {
            switch (msg)
            {
                case "Shell::SpawnRunWindow":
                    Program._actions.Add(ShowRunWindow);
                    break;
                /*case "Shell::SpawnShutdownDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowShutdownDialog);
                    break;
                case "Shell::SpawnCantRunDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowCantRunDialog);
                    break;*/
                default:
                    break;
            }
        });
        /*ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
        {
            ShutdownDialog = null;
        });

        CantRunDialog = new CantRunDialog.CantRunDialog(() =>
        {
            CantRunDialog = null;
        });

        if (SettingsHelper.GetValue("AllowDesktopFeature", "rebound", false))
        {
            // Desktop window
            DesktopWindow = new DesktopWindow(ShowShutdownDialog);
            DesktopWindow.Activate();
            DesktopWindow.AttachToProgMan();
        }*/

        // Start your ReboundPipeClient
    }

    public static void ShowRunWindow()
    {
        if (RunWindow is null)
        {
            RunWindow = new();
            RunWindow.AppWindowInitialized += (s, e) =>
            {
                RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                RunWindow.IsMaximizable = false;
                RunWindow.IsMinimizable = false;
                RunWindow.IsResizable = false;
                RunWindow.MoveAndResize(25, Display.GetAvailableRectForWindow(RunWindow.Handle).bottom - 265, 450, 240);
                RunWindow.Closed += (s, e) =>
                {
                    DestroyRunWindow();
                };
            };
            RunWindow.XamlInitialized += (s, e) =>
            {
                var frame = new Frame();
                frame.Navigate(typeof(Run.RunWindow));
                RunWindow.Content = frame;
            };
            RunWindow.Create();
        }
        else
        {
            RunWindow.Activate();
            RunWindow.ForceBringToFront();
        }
    }

    public static void DestroyRunWindow()
    {
        //RunWindow?.Close();
        RunWindow = null;
    }

    public static void CloseRunWindow()
    {
        RunWindow?.Close();
        RunWindow = null;
    }

    /*public static void ShowShutdownDialog()
    {
        if (ShutdownDialog is null)
        {
            ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
            {
                ShutdownDialog = null;
            });
        }
        ShutdownDialog.Activate();
        ShutdownDialog.ForceBringToFront();
    }

    public static void ShowCantRunDialog()
    {
        if (CantRunDialog is null)
        {
            CantRunDialog = new CantRunDialog.CantRunDialog(() =>
            {
                CantRunDialog = null;
            });
        }
        CantRunDialog.Activate();
        CantRunDialog.ForceBringToFront();
    }*/

    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            Run();
        }
    }

    public static IslandsWindow? RunWindow { get; set; }
    public static IslandsWindow? ContextMenuWindow { get; set; }
    public static IslandsWindow? DesktopWindow { get; set; }
    public static IslandsWindow? ShutdownDialog { get; set; }
    public static IslandsWindow? BackgroundWindow { get; set; }
    public static IslandsWindow? CantRunDialog { get; set; }
}