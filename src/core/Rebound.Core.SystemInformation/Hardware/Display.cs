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
    [Obsolete("Use GetDisplayWorkArea instead.")]
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

    /// <summary>
    /// Retrieves the bounding rectangle of the display area that contains the specified window.
    /// </summary>
    /// <remarks>This method returns the full monitor area, not the work area. If the specified window is not
    /// associated with a display, the display area of the nearest monitor is returned.</remarks>
    /// <param name="hwnd">A handle to the window for which to retrieve the display area. If the window is minimized or not associated with
    /// a display, the nearest display area is used.</param>
    /// <returns>A <see cref="RECT"/> structure that specifies the coordinates of the display area containing the window.</returns>
    public static unsafe RECT GetDisplayArea(HWND hwnd)
    {
        var monitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
        MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
        TerraFX.Interop.Windows.Windows.GetMonitorInfoW(monitor, &monitorInfo);
        return monitorInfo.rcMonitor;
    }

    /// <summary>
    /// Retrieves the work area rectangle of the display that contains the specified window.
    /// </summary>
    /// <remarks>The work area is the portion of the display not obscured by system taskbars or application
    /// desktop toolbars. This method is useful for positioning windows within the usable area of the screen.</remarks>
    /// <param name="hwnd">A handle to the window whose display work area is to be retrieved. If the window is not associated with a
    /// display, the nearest display is used.</param>
    /// <returns>A <see cref="RECT"/> structure that specifies the work area of the display containing the specified window. The
    /// work area excludes taskbars, docked windows, and other app bars.</returns>
    public static unsafe RECT GetDisplayWorkArea(HWND hwnd)
    {
        var monitor = TerraFX.Interop.Windows.Windows.MonitorFromWindow(hwnd, MONITOR.MONITOR_DEFAULTTONEAREST);
        MONITORINFO monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFO) };
        TerraFX.Interop.Windows.Windows.GetMonitorInfoW(monitor, &monitorInfo);
        return monitorInfo.rcWork;
    }

    /// <summary>
    /// Calculates the DPI scaling factor for the specified window handle.
    /// </summary>
    /// <remarks>This method queries the device context associated with the specified window to determine its
    /// horizontal DPI. The returned scaling factor can be used to adjust rendering or layout for high-DPI
    /// displays.</remarks>
    /// <param name="hwnd">The handle to the window for which to retrieve the DPI scaling factor.</param>
    /// <returns>The scaling factor as a double, where 1.0 represents 96 DPI (100% scaling). Values greater than 1.0 indicate
    /// higher DPI settings.</returns>
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

    [Obsolete("Use GetDisplayArea instead.")]
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