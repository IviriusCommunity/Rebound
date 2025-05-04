using Windows.Foundation;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Helpers;

public static class Display
{
    public static double GetScale(WindowEx win)
    {
        // Get the handle to the current window
        var hWnd = new Windows.Win32.Foundation.HWND(win.GetWindowHandle());

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the DPI
        var dpiX = PInvoke.GetDeviceCaps(hdc, Windows.Win32.Graphics.Gdi.GET_DEVICE_CAPS_INDEX.LOGPIXELSX);

        // Release the device context
        _ = PInvoke.ReleaseDC(hWnd, hdc);

        return dpiX / 96.0;
    }

    public static Rect GetDisplayRect(WindowEx win)
    {
        // Get the handle to the current window
        var hWnd = new Windows.Win32.Foundation.HWND(win.GetWindowHandle());

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the width and height of the display
        var width = PInvoke.GetDeviceCaps(hdc, Windows.Win32.Graphics.Gdi.GET_DEVICE_CAPS_INDEX.HORZRES);
        var height = PInvoke.GetDeviceCaps(hdc, Windows.Win32.Graphics.Gdi.GET_DEVICE_CAPS_INDEX.VERTRES);

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

    public static Rect GetDPIAwareDisplayRect(WindowEx win)
    {
        // Get the handle to the current window
        var hWnd = new Windows.Win32.Foundation.HWND(win.GetWindowHandle());

        // Get the device context for the window
        var hdc = PInvoke.GetDC(hWnd);

        // Get the width and height of the display
        var width = PInvoke.GetDeviceCaps(hdc, Windows.Win32.Graphics.Gdi.GET_DEVICE_CAPS_INDEX.HORZRES) / GetScale(win);
        var height = PInvoke.GetDeviceCaps(hdc, Windows.Win32.Graphics.Gdi.GET_DEVICE_CAPS_INDEX.VERTRES) / GetScale(win);

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
