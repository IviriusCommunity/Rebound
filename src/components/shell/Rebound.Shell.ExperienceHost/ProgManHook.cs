using System;
using System.Threading;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    public static unsafe void AttachToProgMan(this WindowEx window)
    {
        HWND hWndProgman;
        HWND hWorkerW;
        HWND hSHELLDLL_DefView = new();
        hWndProgman = PInvoke.FindWindow("Progman", null);
        hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);
        hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);
        PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        Thread.Sleep(250);

        var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
        if (hSysListView32 != HWND.Null)
        {
            PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
        }

        PInvoke.SetWindowLongPtr(new(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hSHELLDLL_DefView);
    }

    private static unsafe bool PlaceDesktopInPos(int WindowsBuild, HWND hWndProgman, HWND hWorkerW, HWND hSHELLDLL_DefView, bool findSHELLDLL_DefView)
    {
        hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);

        if (findSHELLDLL_DefView) hSHELLDLL_DefView = PInvoke.FindWindowEx(hWorkerW, new(null), "SHELLDLL_DefView", null);

        if (hSHELLDLL_DefView != HWND.Null) _ = PInvoke.SetWindowLongPtr(hSHELLDLL_DefView, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hWorkerW);

        // Special handling for 24H2 and above
        if (WindowsBuild >= 26002)
        {
            // Set window styles for 24H2
            PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)WINDOW_STYLE.WS_OVERLAPPEDWINDOW); // example style
            PInvoke.SetWindowLongPtr(hSHELLDLL_DefView, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)WINDOW_STYLE.WS_OVERLAPPEDWINDOW); // example style
        }

        return false;
    }

    private static unsafe void MonitorWorkerWChanges(HWND hWndProgman, HWND hSHELLDLL_DefView)
    {
        // Monitor WorkerW for destruction and reattach
        PInvoke.EnumWindows((hwnd, lParam) =>
        {
            var className = new Span<char>(new char[64]);
            PInvoke.GetClassName(hwnd, className);

            if (className.ToString() == "WorkerW")
            {
                // If WorkerW gets destroyed, recreate it and reset the parent
                PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
                var hNewWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);

                if (hNewWorkerW != HWND.Null)
                {
                    _ = PInvoke.SetWindowLongPtr(hSHELLDLL_DefView, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hNewWorkerW);
                }
            }
            return true;
        }, new LPARAM(0)); // Pass 0 or an appropriate param
    }
}