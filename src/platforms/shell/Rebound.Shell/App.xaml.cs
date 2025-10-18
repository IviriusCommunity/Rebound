// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Shell.Run;
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
    private async void OnSingleInstanceLaunched(object? sender, Core.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            // Run pipe server in a dedicated background thread
            Thread pipeThread = new(async () =>
            {
                using var pipeServer = new PipeHost("REBOUND_SHELL", true);
                pipeServer.MessageReceived += PipeServer_MessageReceived;

                await pipeServer.StartAsync();
            })
            { IsBackground = true, Name = "ShellPipeServerThread" };

            pipeThread.Start();
        }
        else Process.GetCurrentProcess().Kill();
    }

    private void PipeServer_MessageReceived(PipeConnection connection, string arg)
    {
        Debug.WriteLine($"[Shell Experience Host] Received IPC message: {arg}");
        var parts = arg.Split("##");
        if (parts[0] == "Shell::SpawnRunWindow")
        {
            var windowTitle = parts.Length > 1 ? parts[1].Trim() : "Run";
            if (RunWindow is null)
            {
                Program.QueueAction(() =>
                {
                    ShowRunWindow(windowTitle);
                    return Task.CompletedTask;
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
        RunWindow = new();
        RunWindow.AppWindowInitialized += (s, e) =>
        {
            RunWindow.IsPersistenceEnabled = false;
            RunWindow.MoveAndResize(
                25,
                (int)(Display.GetAvailableRectForWindow(RunWindow.Handle).bottom / Display.GetScale(RunWindow.AppWindow)) - 265,
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

    public static void CloseRunWindow()
    {
        RunWindow?.Close();
    }

    public static IslandsWindow? RunWindow { get; set; }
    public static IslandsWindow? ContextMenuWindow { get; set; }
    public static IslandsWindow? DesktopWindow { get; set; }
    public static IslandsWindow? ShutdownDialog { get; set; }
    public static IslandsWindow? BackgroundWindow { get; set; }
    public static IslandsWindow? CantRunDialog { get; set; }
}