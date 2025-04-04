using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    public static int GetWindowsBuildNumberFromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null && key.GetValue("CurrentBuildNumber") is string build)
        {
            if (int.TryParse(build, out var buildNumber))
                return buildNumber;
        }
        return -1;
    }

    public static unsafe void AttachToProgMan(this WindowEx window)
    {
        // Constants
        const int WM_WTSSESSION_CHANGE = 0x02B1;
        const int WM_SETTINGCHANGE = 0x001A;
        const int NOTIFY_FOR_THIS_SESSION = 0;

        // Variables
        var version = GetWindowsBuildNumberFromRegistry();
        HWND hWndProgman;
        HWND hWndProgmanWinUI;
        HWND hWorkerW;
        HWND hSHELLDLL_DefView = new();

        // Get progman handle
        hWndProgman = PInvoke.FindWindow("Progman", null);
        hWndProgmanWinUI = PInvoke.FindWindow("WinUIDesktopWin32WindowClass", "ProgmanWinUI");

        // Get SHELLDLL_DefView handle
        hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);

        // Send message to progman to create WorkerW
        PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        Thread.Sleep(250);

        // Find WorkerW handle
        if (version >= 26100) hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);
        else hWorkerW = GetWorkerW(out _, out _);

        // Set the parent of SHELLDLL_DefView to WorkerW
        PInvoke.SetWindowLongPtr(hSHELLDLL_DefView, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hWorkerW);

        // Find SysListView32 handle and hide it
        var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
        if (hSysListView32 != HWND.Null)
        {
            PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
        }

        Thread.Sleep(250);

        // Set the parent of Rebound Desktop to SHELLDLL_DefView
        PInvoke.SetWindowLongPtr(hWndProgmanWinUI, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hSHELLDLL_DefView);

        if (version >= 26100)
        {
            // Register for session notifications
            PInvoke.WTSRegisterSessionNotification(hWndProgmanWinUI, NOTIFY_FOR_THIS_SESSION);

            // WndProc
            var winManager = WindowManager.Get(window);
            winManager.WindowMessageReceived += (sender, e) =>
            {
                if (e.Message.MessageId is WM_WTSSESSION_CHANGE or WM_SETTINGCHANGE)
                {
                    Thread.Sleep(250);

                    // Obtain new WorkerW handle
                    hWorkerW = GetWorkerW(out _, out _);

                    // Set the parent of SHELLDLL_DefView to WorkerW
                    PInvoke.SetWindowLongPtr(hSHELLDLL_DefView, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hWorkerW);

                    Thread.Sleep(250);

                    // Set the parent of Rebound Desktop to SHELLDLL_DefView
                    PInvoke.SetWindowLongPtr(hWndProgmanWinUI, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, hSHELLDLL_DefView);
                }
            };
        }

        // On closed, restart explorer to revert changes
        window.Closed += Window_Closed;
        static void Window_Closed(object sender, WindowEventArgs args)
        {
            Process.GetProcessesByName("explorer")[0].Kill();
        }
    }

    public static unsafe HWND GetWorkerW(out int x, out int y)
    {
        var foundWorkerW = new LPARAM(0);
        PInvoke.EnumWindows(EnumWindowsProc, foundWorkerW);
        x = 0;
        y = 0;
        return new HWND(foundWorkerW.Value);
    }

    private static unsafe BOOL EnumWindowsProc(HWND param0, LPARAM param1)
    {
        const string WORKERW = "WorkerW";
        const string PROGMAN = "Progman";
        const uint WM_SPAWN_WORKERW = 0x052C;
        const int SMTO_NORMAL = 0x0;

        Span<char> className = new char[64];

        PInvoke.GetClassName(param0, className);

        if (className.ToString() == WORKERW)
        {
            var hProgman = PInvoke.FindWindow(PROGMAN, null);
            PInvoke.SendMessageTimeout(hProgman, WM_SPAWN_WORKERW, new(0), IntPtr.Zero, SMTO_NORMAL, 250);

            uint pid1;
            uint pid2;

            _ = PInvoke.GetWindowThreadProcessId(hProgman, &pid1);
            _ = PInvoke.GetWindowThreadProcessId(param0, &pid2);

            if (pid1 == pid2)
            {
                PInvoke.GetWindowRect(param0, out var rect);
                var screenRight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN); // SM_CXSCREEN
                var screenBottom = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN); // SM_CYSCREEN

                if (rect.right >= screenRight && rect.bottom >= screenBottom)
                {
                    param1 = new((nint)param0.Value);
                    return false; // Stop enumeration
                }
            }
        }

        return true; // Continue enumeration
    }
}