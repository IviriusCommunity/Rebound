// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using TerraFX.Interop.Windows;

namespace Rebound.Core.Helpers;

public static class Display
{
    /// <summary>
    /// Retrieves the available working area of the monitor that contains the specified window.
    /// </summary>
    /// <remarks>This method determines the monitor nearest to the specified window and returns its working
    /// area. The working area excludes system-reserved regions such as taskbars and docked toolbars. This is useful for
    /// positioning or sizing windows to avoid overlapping with system UI elements.</remarks>
    /// <param name="hwnd">A handle to the window for which to determine the available monitor working area.</param>
    /// <returns>A RECT structure representing the portion of the monitor's area available to normal application windows,
    /// excluding areas occupied by taskbars and docked windows.</returns>
    /// <exception cref="InvalidOperationException">Thrown if monitor information cannot be retrieved for the specified window.</exception>
    public unsafe static RECT GetAvailableRectForWindow(HWND hwnd)
    {
        // Find which monitor the window is on
        var hMonitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);

        // Get info about that monitor
        MONITORINFO mi = new()
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

    public static unsafe RECT GetDisplayArea(HWND hwnd)
    {
        var monitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
        MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
        TerraFX.Interop.Windows.Windows.GetMonitorInfoW(monitor, &monitorInfo);
        return monitorInfo.rcMonitor;
    }

    public static unsafe RECT GetDisplayWorkArea(HWND hwnd)
    {
        var monitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
        MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
        TerraFX.Interop.Windows.Windows.GetMonitorInfoW(monitor, &monitorInfo);
        return monitorInfo.rcWork;
    }

    public static double GetScale(HWND hwnd)
    {
        // Get the device context for the window
        var hdc = TerraFX.Interop.Windows.Windows.GetDC(hwnd);

        // Get the DPI
        var dpiX = TerraFX.Interop.Windows.Windows.GetDeviceCaps(hdc, 88); // LOGPIXELSX

        // Release the device context
        _ = TerraFX.Interop.Windows.Windows.ReleaseDC(hwnd, hdc);

        return dpiX / 96.0;
    }

    public static double GetScale()
    {
        // Get the handle to the current window
        var hWnd = new HWND();

        // Get the device context for the window
        var hdc = TerraFX.Interop.Windows.Windows.GetDC(hWnd);

        // Get the DPI
        var dpiX = TerraFX.Interop.Windows.Windows.GetDeviceCaps(hdc, 88); // LOGPIXELSX

        // Release the device context
        _ = TerraFX.Interop.Windows.Windows.ReleaseDC(hWnd, hdc);

        return dpiX / 96.0;
    }

    public static SIZE GetDisplayRect(HWND hWnd)
    {
        // Get the device context for the window
        var hdc = TerraFX.Interop.Windows.Windows.GetDC(hWnd);

        // Get the width and height of the display
        var width = TerraFX.Interop.Windows.Windows.GetDeviceCaps(hdc, 8); // HORZRES
        var height = TerraFX.Interop.Windows.Windows.GetDeviceCaps(hdc, 10); // VERTRES

        // Release the device context
        _ = TerraFX.Interop.Windows.Windows.ReleaseDC(hWnd, hdc);

        return new()
        {
            cx = width,
            cy = height
        };
    }
}