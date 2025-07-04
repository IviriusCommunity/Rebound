﻿// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.WindowsAppSDK.Runtime;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    public static int GetWindowsBuildNumberFromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null && key.GetValue("CurrentBuildNumber") is string build && int.TryParse(build, out var buildNumber))
        {
            return buildNumber;
        }
        return -1;
    }

    public const uint EVENT_OBJECT_CREATE = 0x8000;  // Object creation event

    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;  // Out of context event (no callback filtering based on the context)

    static HWND hWndProgman;
    static HWND hWorkerW;
    static HWND hSHELLDLL_DefView;

    public static async void AttachToProgMan(this WindowEx window)
    {
        // Constants
        const int WM_WTSSESSION_CHANGE = 0x02B1;
        const int WM_SETTINGCHANGE = 0x001A;
        const int NOTIFY_FOR_THIS_SESSION = 0;

        // Variables
        var version = GetWindowsBuildNumberFromRegistry();

        try
        {
            // Get progman handle
            hWndProgman = PInvoke.FindWindow("Progman", null);

            unsafe
            {
                // Get SHELLDLL_DefView handle
                hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);
            }
            HWND hSysListView32;
            unsafe
            {
                hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
            }
            _ = PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
            _ = PInvoke.SetParent(new(window.GetWindowHandle()), hWndProgman);
        }
        catch
        {

        }
        // Send message to progman to create WorkerW
        /*unsafe
        {
            _ = PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        }

        await Task.Delay(250).ConfigureAwait(true);*/

        /*if (version >= 26100)
        {
            unsafe
            {
                // Find WorkerW handle
                hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);
            }

            _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000));
            _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0x20000880);

            // Set the parent of WorkerW to Progman
            _ = PInvoke.SetParent(hWorkerW, hWndProgman);

            // Set the parent of Rebound Desktop to SHELLDLL_DefView
            _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

            // Set the parent of SHELLDLL_DefView to WorkerW
            _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
        }
        else
        {
            // Send message to Progman to create WorkerW
            unsafe
            {
                _ = PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
            }

            await Task.Delay(250).ConfigureAwait(true);

            // Find the parent of SHELLDLL_DefView (which should be WorkerW)
            unsafe
            {
                hWorkerW = PInvoke.GetParent(hSHELLDLL_DefView);
            }

            if (hWorkerW != HWND.Null)
            {
                _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

                _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
            }
        }

        unsafe
        {
            // Find SysListView32 handle and hide it
            var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
            if (hSysListView32 != HWND.Null)
            {
                _ = PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        if (version >= 26100)
        {
            // Register for session notifications
            _ = PInvoke.WTSRegisterSessionNotification(new(window.GetWindowHandle()), NOTIFY_FOR_THIS_SESSION);

            // WndProc with delay
            var winManager = WindowManager.Get(window);

            // Introduce a delay of 5 seconds
            await Task.Delay(5000).ConfigureAwait(true);

            // After the delay, subscribe to the window message handler
            winManager.WindowMessageReceived += WinManager_WindowMessageReceived;
            unsafe void WinManager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
            {
                switch (e.Message.MessageId)
                {
                    case WM_WTSSESSION_CHANGE:
                    case WM_SETTINGCHANGE:
                        {
                            unsafe
                            {
                                if (e.Message.WParam == 0x7) Perform24H2Fixes(false, version, hWorkerW);
                                if (e.Message.WParam == 0x8 && version >= 26002) WallpaperHelper24H2(hWorkerW);
                            }
                            break;
                        }
                }
            }
        }*/
    }

    public static unsafe ulong WallpaperHelper24H2(HWND hWorkerW)
    {
        var hWndProgman = PInvoke.FindWindow("Progman", "Program Manager");
        hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, (HWND)null, "SHELLDLL_DefView", null);
        PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000L));
        PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)0x20000880L);
        return 0;
    }

    static unsafe void Perform24H2Fixes(bool full, int buildNumber, HWND hWorkerW)
    {
        if (buildNumber >= 26002)
        {
            var hWndProgman = PInvoke.FindWindow("Progman", "Program Manager");
            PInvoke.SetParent(hSHELLDLL_DefView, hWndProgman);
            if (full)
            {
                WallpaperHelper24H2(hWorkerW);
            }
        }
    }

    private static DateTime lastSettingChangeTime = DateTime.MinValue; // Store the last time

    public static unsafe HWND GetVisibleWorkerW(HWND hWndProgman)
    {
        var hWorkerW = HWND.Null;

        PInvoke.EnumChildWindows(hWndProgman, EnumChildWindowsProc, IntPtr.Zero);

        return hWorkerW;

        unsafe BOOL EnumChildWindowsProc(HWND hwnd, LPARAM param)
        {
            Span<char> className = new char[64];

            _ = PInvoke.GetClassName(hwnd, className);

            // Check if it's a WorkerW and it's not hidden
            if (className.ToString() == "WorkerW" && PInvoke.IsWindowVisible(hwnd))
            {
                hWorkerW = hwnd;
                return false; // Stop enumeration once we find the visible WorkerW
            }
            return true; // Continue enumeration
        }
    }

    public static unsafe HWND GetWorkerW(out int x, out int y)
    {
        var foundWorkerW = new LPARAM(0);
        _ = PInvoke.EnumWindows(EnumWindowsProc, foundWorkerW);
        x = 0;
        y = 0;
        return new HWND(foundWorkerW.Value);
    }

    private static unsafe BOOL EnumWindowsProc(HWND param0, LPARAM param1)
    {
        const string WORKERW = "WorkerW";
        const string PROGMAN = "Progman";
        const uint WM_SPAWN_WORKERW = 0x052C;
        const int SMTO_NORMAL = 0x0;

        Span<char> className = new char[64];

        _ = PInvoke.GetClassName(param0, className);

        if (className.ToString() == WORKERW)
        {
            var hProgman = PInvoke.FindWindow(PROGMAN, null);
            _ = PInvoke.SendMessageTimeout(hProgman, WM_SPAWN_WORKERW, new(0), IntPtr.Zero, SMTO_NORMAL, 250);

            uint pid1;
            uint pid2;

            _ = PInvoke.GetWindowThreadProcessId(hProgman, &pid1);
            _ = PInvoke.GetWindowThreadProcessId(param0, &pid2);

            if (pid1 == pid2)
            {
                _ = PInvoke.GetWindowRect(param0, out var rect);
                var screenRight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN); // SM_CXSCREEN
                var screenBottom = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN); // SM_CYSCREEN

                if (rect.right >= screenRight && rect.bottom >= screenBottom)
                {
                    return false; // Stop enumeration
                }
            }
        }

        return true; // Continue enumeration
    }
}