using Rebound.Helpers;
using WinRT.Interop;
using WinUIEx;

#nullable enable

namespace Rebound.ShellExperiencePack;

public static class ProgManHook
{
    // ProgMan class name (desktop window)
    private const string ProgManClassName = "Progman";

    public static void AttachToProgMan(this WindowEx window)
    {
        // Get the ProgMan window handle
        var progManHwnd = User32.FindWindow(ProgManClassName, null);

        if (progManHwnd != IntPtr.Zero)
        {
            // Get the current window handle (this app's window)
            var appWnd = WindowNative.GetWindowHandle(window);

            // Set the parent of the current app window to be the ProgMan window
            User32.SetParent(appWnd, progManHwnd);
        }
    }
}