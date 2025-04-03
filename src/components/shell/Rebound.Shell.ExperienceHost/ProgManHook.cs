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
        hWorkerW = GetWorkerW2();
        hWndProgman = PInvoke.FindWindow("Progman", null);
        var WindowsBuild = (int?)Registry.GetValue("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuildNumber", string.Empty) ?? 0;

        if (hWndProgman != HWND.Null)
        {
            hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);
            PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
            Thread.Sleep(250);

            if (hSHELLDLL_DefView != HWND.Null)
            {
                _ = PlaceDesktopInPos(WindowsBuild, hWndProgman, hWorkerW, hSHELLDLL_DefView, false);
            }
        }

        _ = PlaceDesktopInPos(WindowsBuild, hWndProgman, hWorkerW, hSHELLDLL_DefView, true);

        var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
        if (hSysListView32 != HWND.Null)
        {
            PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
        }

        // For 24H2, listen for WorkerW changes
        if (WindowsBuild >= 26002) // Assuming 24H2 and above
        {
            MonitorWorkerWChanges(hWndProgman, hSHELLDLL_DefView);
        }
    }

    private static unsafe HWND GetWorkerW2()
    {
        HWND hWorkerW = new();
        PInvoke.EnumWindows(EnumWindowsProc2, new((nint)hWorkerW.Value));
        return hWorkerW;
    }

    private static unsafe bool PlaceDesktopInPos(int WindowsBuild, HWND hWndProgman, HWND hWorkerW, HWND hSHELLDLL_DefView, bool findSHELLDLL_DefView)
    {
        if (WindowsBuild < 26002) hWorkerW = GetWorkerW2();
        else hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);

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

    private static unsafe BOOL EnumWindowsProc2(HWND hwnd, LPARAM lParam)
    {
        var className = new Span<char>(new char[64]);
        var hWndProgman = PInvoke.FindWindow("Progman", "Program Manager");

        PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        PInvoke.GetClassName(hwnd, className);

        if (className.ToString() == "WorkerW")
        {
            var threadId = PInvoke.GetWindowThreadProcessId(hWndProgman, null);
            var threadId2 = PInvoke.GetWindowThreadProcessId(hwnd, null);
            if (threadId == threadId2)
            {
                RECT dimensions;
                PInvoke.GetWindowRect(hwnd, out dimensions);
                var right = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
                var bottom = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
                if (dimensions.right >= right && dimensions.bottom >= bottom)
                {
                    return true;
                }
            }
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