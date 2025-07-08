using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Helpers;

public static class WindowHelper
{
    public static void ForceBringToFront(this WindowEx window)
    {
        var hWnd = new HWND(window.GetWindowHandle());

        var thisThreadId = PInvoke.GetCurrentThreadId();
        var foregroundHwnd = PInvoke.GetForegroundWindow();
        uint lpdwProcessId;
        unsafe
        {
            var foregroundThreadId = PInvoke.GetWindowThreadProcessId(foregroundHwnd, &lpdwProcessId);

            if (thisThreadId != foregroundThreadId)
            {
                // Attach input to foreground thread
                PInvoke.AttachThreadInput(foregroundThreadId, thisThreadId, true);

                // Ensure window is shown
                PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);

                // Try to bring it to foreground
                PInvoke.SetForegroundWindow(hWnd);

                // Detach input after done
                PInvoke.AttachThreadInput(foregroundThreadId, thisThreadId, false);
            }
            else
            {
                // Same thread, simpler path
                PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.SetForegroundWindow(hWnd);
            }
        }

        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_RESTORE);
        window.Activate();
        PInvoke.BringWindowToTop(hWnd);
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