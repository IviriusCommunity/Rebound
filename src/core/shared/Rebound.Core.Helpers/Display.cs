// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Rebound.Core.Helpers;

public static class Display
{
    public unsafe static TerraFX.Interop.Windows.RECT GetAvailableRectForWindow(TerraFX.Interop.Windows.HWND hwnd)
    {
        // Find which monitor the window is on
        var hMonitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, TerraFX.Interop.Windows.MONITOR.MONITOR_DEFAULTTONEAREST);

        // Get info about that monitor
        TerraFX.Interop.Windows.MONITORINFO mi = new()
        {
            cbSize = (uint)sizeof(MONITORINFO)
        };

        if (TerraFX.Interop.Windows.Windows.GetMonitorInfo(hMonitor, &mi))
        {
            // rcWork is the area available to normal app windows
            return mi.rcWork;
        }

        throw new InvalidOperationException("Failed to get monitor info.");
    }

    public static double GetScale(AppWindow win)
    {
        // Get the handle to the current window
        var hWnd = new HWND(Win32Interop.GetWindowFromWindowId(win.Id));

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the DPI
        var dpiX = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.LOGPIXELSX);

        // Release the device context
        _ = PInvoke.ReleaseDC(hWnd, hdc);

        return dpiX / 96.0;
    }

    public static double GetScale()
    {
        // Get the handle to the current window
        var hWnd = new HWND(0);

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the DPI
        var dpiX = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.LOGPIXELSX);

        // Release the device context
        _ = PInvoke.ReleaseDC(hWnd, hdc);

        return dpiX / 96.0;
    }

    public static Rect GetDisplayRect(AppWindow win)
    {
        // Get the handle to the current window
        var hWnd = new HWND(Win32Interop.GetWindowFromWindowId(win.Id));

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the width and height of the display
        var width = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.HORZRES);
        var height = PInvoke.GetDeviceCaps(hdc, GET_DEVICE_CAPS_INDEX.VERTRES);

        // Release the device context
        _ = PInvoke.ReleaseDC(hWnd, hdc);

        return new Rect()
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height
        };
    }
}