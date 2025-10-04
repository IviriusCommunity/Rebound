// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Shell.Run;
using System;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

//[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    private static bool _runWindowQueued = false;

    public App()
    {
        Run();
    }

    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void Run()
    {
        var pipeServer = new TrustedPipeServer("REBOUND_SHELL");
        _ = pipeServer.StartAsync();
        pipeServer.MessageReceived += PipeServer_MessageReceived;

        ReboundPipeClient = new ReboundPipeClient();
        await ReboundPipeClient.ConnectAsync();

        ReboundPipeClient.StartListening(async (msg) =>
        {
            switch (msg)
            {
                case "Shell::SpawnRunWindow":
                    {
                        // Only enqueue if not already queued
                        if (!_runWindowQueued)
                        {
                            _runWindowQueued = true;

                            Program._actions.Add(() =>
                            {
                                ShowRunWindow();
                                // reset flag after action executed
                                _runWindowQueued = false;
                            });
                        }
                        break;
                    }
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

    private async Task PipeServer_MessageReceived(string arg)
    {
        var parts = arg.Split("##");
        if (parts[0] == "Shell::SpawnRunWindow")
        {
            if (RunWindow == null)
            {
                var windowTitle = parts.Length > 1 ? parts[1].Trim() : "Run";
                Program._actions.Add(() =>
                {
                    ShowRunWindow(windowTitle);
                    _runWindowQueued = false; // runs only when this queued action executes
                });
            }
            else
            {
                TerraFX.Interop.Windows.Windows.ShowWindow(RunWindow.Handle, SW.SW_SHOW);
                TerraFX.Interop.Windows.Windows.SetForegroundWindow(RunWindow.Handle);
                TerraFX.Interop.Windows.Windows.SetActiveWindow(RunWindow.Handle);
            }
        }

        return;
    }

    public static void ShowRunWindow(string title = "Run")
    {
        if (RunWindow is null)
        {
            RunWindow = new();
            RunWindow.AppWindowInitialized += (s, e) =>
            {
                RunWindow.MoveAndResize((int)(25 * Display.GetScale(RunWindow.AppWindow)), Display.GetAvailableRectForWindow(RunWindow.Handle).bottom - (int)(265 * Display.GetScale(RunWindow.AppWindow)), 450, 240);
                RunWindow.Title = title;
                RunWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico");
                RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                RunWindow.IsMaximizable = false;
                RunWindow.IsMinimizable = false;
                RunWindow.IsResizable = false;
                RunWindow.Closing += (sender, args) =>
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
        else
        {
            //RunWindow.ForceBringToFront();
            TerraFX.Interop.Windows.Windows.ShowWindow(RunWindow.Handle, SW.SW_SHOW);
            TerraFX.Interop.Windows.Windows.SetForegroundWindow(RunWindow.Handle);
            TerraFX.Interop.Windows.Windows.SetActiveWindow(RunWindow.Handle);
        }
    }

    public static void CloseRunWindow()
    {
        RunWindow?.Close();
        //RunWindow = null;
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

    private void OnSingleInstanceLaunched(object? sender, Core.Helpers.Services.SingleInstanceLaunchEventArgs e)
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