using System;
using Rebound.Helpers;
using WinRT.Interop;
using WinUIEx;
using static Rebound.Helpers.User32;

namespace Rebound.Shell.ExperienceHost;

public static class ProgManHook
{
    // ProgMan class name (desktop window)
    private const string ProgManClassName = "Progman";

    public static void AttachToProgMan(this WindowEx window)
    {
        // Get the ProgMan window handle
        var progManHwnd = new WndHandle(FindWindow(ProgManClassName, null));

        if (progManHwnd.Handle != IntPtr.Zero)
        {
            // Get the current window handle (this app's window)
            var appWnd = new WndHandle(WindowNative.GetWindowHandle(window));

            // Set the parent of the current app window to be the ProgMan window
            SetParent(appWnd, progManHwnd);

            window.SetWindowOpacity(255);
        }
    }

    public static void AttachToWrapperWindow(this WindowEx childWindow, WindowEx window)
    {
        // Get the current window handle (this app's window)
        var appWnd = new WndHandle(WindowNative.GetWindowHandle(window));

        // Get the current window handle (this app's window)
        var childAppWnd = new WndHandle(WindowNative.GetWindowHandle(childWindow));

        // Set the parent of the current app window to be the ProgMan window
        SetParent(childAppWnd, appWnd);
    }
}