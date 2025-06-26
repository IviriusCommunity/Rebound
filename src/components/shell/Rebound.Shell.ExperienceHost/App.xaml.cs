// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
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
            if (SettingsHelper.GetValue<bool>("InstallShutdownDialog", "rebound", true)) hook1.WindowDetected += Hook_WindowDetected_Shutdown;

            var hook2 = new WindowHook("#32770", "Run", "explorer");
            if (SettingsHelper.GetValue<bool>("InstallRun", "rebound", true)) hook2.WindowDetected += Hook_WindowDetected_Run;

            /*var hook3 = new WindowHook("Shell_Dialog", "This app can't run on your PC", "explorer");
            hook3.WindowDetected += Hook_WindowDetected_CantRun;*/

            var hook4 = new WindowHook("Shell_Dialog", "This app can’t run on your PC", "explorer");
            if (SettingsHelper.GetValue<bool>("InstallThisAppCantRunOnYourPC", "rebound", true)) hook4.WindowDetected += Hook_WindowDetected_Dim;

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

        CantRunDialog = new CantRunDialog.CantRunDialog(() =>
        {
            CantRunDialog = null;
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
        BackgroundWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            PInvoke.PostMessage(new(e.Handle), WM_CLOSE, new Windows.Win32.Foundation.WPARAM(0), IntPtr.Zero);
            PInvoke.DestroyWindow(new(e.Handle));
            ShowShutdownDialog();
        });
    }

    private async void Hook_WindowDetected_Dim(object? sender, WindowDetectedEventArgs e)
    {
        if (PInvoke.IsWindow(new(e.Handle)))
        {
            // Send WM_CLOSE asynchronously, non-blocking
            PInvoke.PostMessage(new(e.Handle), WM_CLOSE, new Windows.Win32.Foundation.WPARAM(0), IntPtr.Zero);
        }
        // Optional: you can check if window is still there, and if so, try again or just move on

        await BackgroundWindow.DispatcherQueue.EnqueueAsync(() =>
        {
            ShowCantRunDialog();
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
        CantRunDialog.BringToFront();
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
    public static WindowEx? CantRunDialog { get; set; }
}