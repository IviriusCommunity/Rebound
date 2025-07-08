using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Helpers;

public static class WindowHelper
{
    public static unsafe void ForceBringToFront(this WindowEx window)
    {
        var hWnd = new HWND(window.GetWindowHandle());

        // Restore if minimized
        if (PInvoke.IsIconic(hWnd))
        {
            PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_RESTORE);
        }

        var foregroundHwnd = PInvoke.GetForegroundWindow();
        var currentThreadId = PInvoke.GetCurrentThreadId();
        uint lpdwProcessId;
        var foregroundThreadId = PInvoke.GetWindowThreadProcessId(foregroundHwnd, &lpdwProcessId);
        // Attach input to foreground thread if needed
        if (currentThreadId != foregroundThreadId)
        {
            PInvoke.AttachThreadInput(foregroundThreadId, currentThreadId, true);
            PInvoke.SetForegroundWindow(hWnd);
            PInvoke.AttachThreadInput(foregroundThreadId, currentThreadId, false);
        }
        else
        {
            PInvoke.SetForegroundWindow(hWnd);
        }

        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW); // Show if hidden
    }

    public static void RemoveTitleBarIcon(this WindowEx window)
    {
        var exStyle = PInvoke.GetWindowLongPtr(new HWND(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        PInvoke.SetWindowLongPtr(new HWND(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | (nint)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME);

        PInvoke.SetWindowPos(new HWND(window.GetWindowHandle()), HWND.Null, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }
}