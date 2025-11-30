// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Generators;
using Rebound.Shell.Run;
using Rebound.Shell.ShutdownDialog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    public static PipeClient? ReboundPipeClient { get; private set; }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            WindowList.KeepAlive = true;

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

            // Run pipe server in a dedicated background thread
            Thread pipeServerThread = new(async () =>
            {
                using var pipeServer = new PipeHost("REBOUND_SHELL", AccessLevel.Everyone);
                pipeServer.MessageReceived += PipeServer_MessageReceived;

                await pipeServer.StartAsync();
            })
            {
                IsBackground = true,
                Name = "ShellPipeServerThread"
            };

            pipeServerThread.Start();
        }
        else Process.GetCurrentProcess().Kill();
    }

    private static void OnPipeMessageReceived(string message)
    {

    }

    private void PipeServer_MessageReceived(PipeConnection connection, string arg)
    {
        var parts = arg.Split("##");
        if (parts[0] == "Shell::SpawnRunWindow")
        {
            var windowTitle = parts.Length > 1 ? parts[1].Trim() : "Run";
            if (string.IsNullOrWhiteSpace(windowTitle)) windowTitle = "Run";
            if (RunWindow is null)
            {
                UIThreadQueue.QueueAction(() =>
                {
                    ShowRunWindow(windowTitle);
                    return Task.CompletedTask;
                });
            }
            else
            {
                RunWindow.BringToFront();
            }
        }
        if (parts[0] == "Shell::SpawnShutdownWindow")
        {
            if (RunWindow is null)
            {
                UIThreadQueue.QueueAction(() =>
                {
                    ShowShutdownWindow();
                    return Task.CompletedTask;
                });
            }
            else
            {
                RunWindow.BringToFront();
            }
        }

        return;
    }

    public static void ShowRunWindow(string title = "Run")
    {
        RunWindow = new();
        RunWindow.AppWindowInitialized += (s, e) =>
        {
            RunWindow.IsPersistenceEnabled = false;
            RunWindow.MoveAndResize(
                25,
                (int)(Display.GetAvailableRectForWindow(RunWindow.Handle).bottom / Display.GetScale(RunWindow.Handle)) - 265,
                450,
                240);
            RunWindow.Title = title;
            RunWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico");
            RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            RunWindow.IsMaximizable = false;
            RunWindow.IsMinimizable = false;
            RunWindow.IsResizable = false;
            RunWindow.OnClosing += (sender, args) =>
            {
                RunWindow = null;
            };
        };
        RunWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(RunWindow));
            RunWindow.Content = frame;
            (frame.Content as RunWindow).WindowTitle.Text = title;
        };
        RunWindow.Create();
    }

    public static void ShowShutdownWindow()
    {
        ShutdownWindow = new();
        ShutdownWindow.AppWindowInitialized += (s, e) =>
        {
            ShutdownWindow.Resize(480, 344);
            ShutdownWindow.IsPersistenceEnabled = false;
            ShutdownWindow.Title = "Power options";
            ShutdownWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Shutdown.ico");
            ShutdownWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            ShutdownWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            ShutdownWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            ShutdownWindow.IsMaximizable = false;
            ShutdownWindow.IsMinimizable = false;
            ShutdownWindow.IsResizable = false;
            ShutdownWindow.CenterWindow();
            ShutdownWindow.OnClosing += (sender, args) =>
            {
                ShutdownWindow = null;
            };
        };
        ShutdownWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(ShutdownDialog.ShutdownDialog));
            ShutdownWindow.Content = frame;
            (frame.Content as ShutdownDialog.ShutdownDialog).WindowTitle.Text = "Power options";
        };
        ShutdownWindow.Create();
        ShutdownWindow.CenterWindow();
    }

    public static void CloseRunWindow()
    {
        UIThreadQueue.QueueAction(() =>
        {
            RunWindow?.Close();
            return Task.CompletedTask;
        });
    }

    public static IslandsWindow? RunWindow { get; set; }
    public static IslandsWindow? ContextMenuWindow { get; set; }
    public static IslandsWindow? DesktopWindow { get; set; }
    public static IslandsWindow? ShutdownWindow { get; set; }
    public static IslandsWindow? BackgroundWindow { get; set; }
    public static IslandsWindow? CantRunDialog { get; set; }
}