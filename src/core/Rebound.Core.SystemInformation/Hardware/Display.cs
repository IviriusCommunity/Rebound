// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class Display
{
    private const uint ENUM_CURRENT_SETTINGS = unchecked((uint)-1);

    /// <summary>
    /// Retrieves the current display resolution by querying the system's display settings.
    /// </summary>
    /// <returns>
    /// A <see cref="SIZE"/> structure containing the width (<see cref="SIZE.cx"/>) and height (<see cref="SIZE.cy"/>) of the current display resolution in pixels.
    /// If the display settings cannot be retrieved, a <see cref="SIZE"/> with both dimensions set to 0 is returned.
    /// </returns>
    public static unsafe SIZE GetDisplayResolution()
    {
        var dm = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };
        if (TerraFX.Interop.Windows.Windows.EnumDisplaySettingsExW(null, ENUM_CURRENT_SETTINGS, &dm, 0x00000002))
            return new((int)dm.dmPelsWidth, (int)dm.dmPelsHeight);
        return new(0, 0);
    }

    /// <summary>
    /// Retrieves the current display resolution as a formatted string by querying the system's display settings.
    /// </summary>
    /// <returns>
    /// A string in the format "WIDTHxHEIGHT" representing the current display resolution (e.g., "1920x1080"). 
    /// If the display settings cannot be retrieved, "Unknown" is returned.
    /// </returns>
    public static string GetDisplayResolutionString()
    {
        var resolution = GetDisplayResolution();
        if (resolution.cx == 0 || resolution.cy == 0)
            return "Unknown";
        return $"{resolution.cx}x{resolution.cy}";
    }

    /// <summary>
    /// Retrieves the current display refresh rate by querying the system's display settings.
    /// </summary>
    /// <returns>
    /// A uint representing the current display refresh rate in hertz (Hz). If the display settings cannot be retrieved, 0 is returned.
    /// </returns>
    public static unsafe uint GetDisplayRefreshRate()
    {
        var dm = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };
        if (TerraFX.Interop.Windows.Windows.EnumDisplaySettingsExW(null, ENUM_CURRENT_SETTINGS, &dm, 0x00000002))
            return dm.dmDisplayFrequency;
        return 0;
    }

    /// <summary>
    /// Retrieves the current display color depth (bits per pixel) by querying the system's display settings.
    /// </summary>
    /// <returns>
    /// A uint representing the current display color depth in bits per pixel (e.g., 24, 32). 
    /// If the display settings cannot be retrieved, 0 is returned.
    /// </returns>
    public static unsafe uint GetDisplayBitsPerPixel()
    {
        var dm = new DEVMODEW { dmSize = (ushort)sizeof(DEVMODEW) };
        if (TerraFX.Interop.Windows.Windows.EnumDisplaySettingsW(null, ENUM_CURRENT_SETTINGS, &dm))
            return dm.dmBitsPerPel;
        return 0;
    }

    /// <summary>
    /// Retrieves the current display refresh rate as a formatted string by querying the system's display settings.
    /// </summary>
    /// <returns>
    /// A string representing the current display refresh rate in hertz (Hz) formatted as "{REFRESH_RATE} Hz" (e.g., "60 Hz").
    /// </returns>
    public static string GetDisplayRefreshRateString()
    {
        var refreshRate = GetDisplayRefreshRate();
        if (refreshRate == 0)
            return "Unknown";
        return $"{refreshRate} Hz";
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

    /// <summary>
    /// Retrieves the current display scaling percentage as a formatted string by calculating the DPI scaling factor for the primary display.
    /// </summary>
    /// <returns>
    /// A string representing the current display scaling percentage (e.g., "100%", "125%", "150%") based on the DPI scaling factor of the primary display.
    /// </returns>
    public static string GetScaleString()
    {
        return GetScale(HWND.NULL) * 100 + "%";
    }
}