// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.UI.Xaml;
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

    private void Run()
    {
        var thread = new Thread(() =>
        {
            var hook1 = new WindowHook("#32770", "Shut Down Windows", "explorer");
            hook1.WindowDetected += Hook_WindowDetected_Shutdown;

            var hook2 = new WindowHook("#32770", "Run", "explorer");
            hook2.WindowDetected += Hook_WindowDetected_Run;

            // Keep message pump alive so both hooks keep working
            NativeMessageLoop();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        // Background window
        BackgroundWindow = new() { SystemBackdrop = new TransparentTintBackdrop(), IsMaximizable = false };
        BackgroundWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
        BackgroundWindow.SetWindowStyle(WindowStyle.Visible);
        BackgroundWindow.Activate();
        BackgroundWindow.MoveAndResize(0, 0, 0, 0);
        BackgroundWindow.Minimize();
        BackgroundWindow.SetWindowOpacity(0);

        RunWindow = new Run.RunWindow(() =>
        {
            RunWindow = null;
        });

        ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
        {
            ShutdownDialog = null;
        });

        // Desktop window
        DesktopWindow = new DesktopWindow(ShowShutdownDialog, CreateContextMenu);
        DesktopWindow.Activate();
        DesktopWindow.AttachToProgMan();

        ContextMenuWindow = new ContextMenuWindow(DesktopWindow as DesktopWindow);
        ContextMenuWindow.Activate();
    }

    private void CreateContextMenu(Point pos)
    {
        (ContextMenuWindow as ContextMenuWindow).ShowContextMenu(pos);
    }

    private const uint WM_CLOSE = 0x10; // WM_CLOSE constant

    private void Hook_WindowDetected_Run(object? sender, WindowDetectedEventArgs e)
    {
        // Send WM_CLOSE message to close the window
        PInvoke.SendMessage(new(e.Handle), WM_CLOSE, 0, 0);

        // Make sure to update the UI (run window activation) on the UI thread
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
            RunWindow.BringToFront();
        });
    }

    private void Hook_WindowDetected_Shutdown(object? sender, WindowDetectedEventArgs e)
    {
        PInvoke.DestroyWindow(new(e.Handle));
        BackgroundWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            ShowShutdownDialog();
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
        ShutdownDialog.BringToFront();
    }

    private static void NativeMessageLoop()
    {
        while (true)
        {
            PInvoke.GetMessage(out var msg, Windows.Win32.Foundation.HWND.Null, 0, 0);
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
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
}