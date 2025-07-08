// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    public static unsafe void AttachToProgMan(this WindowEx window)
    {
        try
        {
            // Find Progman
            var hWndProgman = PInvoke.FindWindow("Progman", null);
            if (hWndProgman == HWND.Null)
            {
                return;
            }

            // Find the SHELLDLL_DefView window
            var hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, HWND.Null, "SHELLDLL_DefView", null);
            if (hSHELLDLL_DefView == HWND.Null)
            {
                // TODO: fallback search inside WorkerW windows
                return;
            }

            // Find the SysListView32 window inside SHELLDLL_DefView
            var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, HWND.Null, "SysListView32", "FolderView");
            if (hSysListView32 != HWND.Null)
            {
                PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
            }

            // Current window's handle
            HWND hWndWindow = new(window.GetWindowHandle());

            // Set the parent of the current window to SHELLDLL_DefView
            PInvoke.SetParent(hWndWindow, hWndProgman);

            // Set extended styles for the current window
            var style = PInvoke.GetWindowLong(hWndWindow, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            style |= (int)WINDOW_EX_STYLE.WS_EX_LAYERED;
            _ = PInvoke.SetWindowLong(hWndWindow, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);

            // Enable transparency to allow rendering of the desktop wallpaper
            PInvoke.SetLayeredWindowAttributes(hWndWindow, new COLORREF(0), 0, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_COLORKEY);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AttachToProgMan failed: {ex.Message}");
        }
    }
}