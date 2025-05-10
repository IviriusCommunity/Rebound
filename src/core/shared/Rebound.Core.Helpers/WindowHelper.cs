using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Helpers;

public static class WindowHelper
{
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