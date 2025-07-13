// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Helpers;
using Rebound.Shell.Desktop;
using Rebound.Shell.ExperiencePack;
using Windows.Foundation;
using Windows.Win32;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    public App()
    {

    }

    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void Run()
    {
        // Background window
        BackgroundWindow = new() { SystemBackdrop = new TransparentTintBackdrop(), IsMaximizable = false };
        BackgroundWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
        BackgroundWindow.SetWindowStyle(WindowStyle.Visible);
        BackgroundWindow.MoveAndResize(0, 0, 0, 0);
        BackgroundWindow.Minimize();
        BackgroundWindow.SetWindowOpacity(0);
        BackgroundWindow.Activate();

        RunWindow = new Run.RunWindow(() =>
        {
            RunWindow = null;
        });

        ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
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
        }

        ReboundPipeClient = new ReboundPipeClient();
        await ReboundPipeClient.ConnectAsync();

        ReboundPipeClient.StartListening(async (msg) =>
        {
            switch (msg)
            {
                case "Shell::SpawnRunWindow":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowRunWindow);
                    break;
                case "Shell::SpawnShutdownDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowShutdownDialog);
                    break;
                case "Shell::SpawnCantRunDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowCantRunDialog);
                    break;
                default:
                    break;
            }
            // Handle server events here
        });
    }

    public static void ShowRunWindow()
    {
        BackgroundWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            if (RunWindow is null)
            {
                RunWindow = new Run.RunWindow(() =>
                {
                    RunWindow = null;
                });
            }
            RunWindow.Activate();
            RunWindow.ForceBringToFront();
        });
    }

    public static void ShowShutdownDialog()
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
    }

    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            Run();
        }
    }

    public static WindowEx? RunWindow { get; set; }
    public static WindowEx? ContextMenuWindow { get; set; }
    public static WindowEx? DesktopWindow { get; set; }
    public static WindowEx? ShutdownDialog { get; set; }
    public static WindowEx? BackgroundWindow { get; set; }
    public static WindowEx? CantRunDialog { get; set; }
}