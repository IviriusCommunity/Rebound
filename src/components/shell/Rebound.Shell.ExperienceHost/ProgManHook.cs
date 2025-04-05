using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;
using static Rebound.Helpers.User32;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    public static int GetWindowsBuildNumberFromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null && key.GetValue("CurrentBuildNumber") is string build && int.TryParse(build, out var buildNumber))
        {
            return buildNumber;
        }
        return -1;
    }

    // Event constants for SetWinEventHook
    public const uint EVENT_OBJECT_CREATE = 0x8000;  // Object creation event

    // Constants for the WinEventHook function
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;  // Out of context event (no callback filtering based on the context)

    public static async void AttachToProgMan(this WindowEx window)
    {
        // Constants
        const int WM_WTSSESSION_CHANGE = 0x02B1;
        const int WM_SETTINGCHANGE = 0x001A;
        const int NOTIFY_FOR_THIS_SESSION = 0;

        // Variables
        var version = GetWindowsBuildNumberFromRegistry();
        HWND hWndProgman;
        HWND hWorkerW;
        HWND hSHELLDLL_DefView;

        // Get progman handle
        hWndProgman = PInvoke.FindWindow("Progman", null);

        unsafe
        {
            // Get SHELLDLL_DefView handle
            hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);
        }

        // Send message to progman to create WorkerW
        unsafe
        {
            _ = PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        }

        await Task.Delay(250);

        if (version >= 26100)
        {
            unsafe
            {
                // Find WorkerW handle
                hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);
            }

            _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000));
            _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0x20000880);

            // Set the parent of WorkerW to Progman
            _ = PInvoke.SetParent(hWorkerW, hWndProgman);

            // Set the parent of Rebound Desktop to SHELLDLL_DefView
            _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

            // Set the parent of SHELLDLL_DefView to WorkerW
            _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
        }
        else
        {
            // Send message to Progman to create WorkerW
            unsafe
            {
                _ = PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
            }

            await Task.Delay(250);

            // Find the parent of SHELLDLL_DefView (which should be WorkerW)
            unsafe
            {
                hWorkerW = PInvoke.GetParent(hSHELLDLL_DefView);
            }

            if (hWorkerW != HWND.Null)
            {
                _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

                _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
            }
        }

        unsafe
        {
            // Find SysListView32 handle and hide it
            var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
            if (hSysListView32 != HWND.Null)
            {
                _ = PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        if (version >= 26100)
        {
            // Register for session notifications
            _ = PInvoke.WTSRegisterSessionNotification(new(window.GetWindowHandle()), NOTIFY_FOR_THIS_SESSION);

            // WndProc with delay
            var winManager = WindowManager.Get(window);

            // Introduce a delay of 5 seconds
            await Task.Delay(5000);

            // After the delay, subscribe to the window message handler
            winManager.WindowMessageReceived += async (sender, e) =>
            {
                switch (e.Message.MessageId)
                {
                    case WM_WTSSESSION_CHANGE:
                    case WM_SETTINGCHANGE:
                        {
                            // Only run the logic if at least 2 seconds have passed
                            if (DateTime.Now - lastSettingChangeTime >= TimeSpan.FromSeconds(2))
                            {
                                lastSettingChangeTime = DateTime.Now; // Update the timestamp

                                unsafe
                                {
                                    // Obtain the new visible WorkerW handle
                                    hWorkerW = PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null);
                                }

                                _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000));
                                _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0x20000880);

                                await Task.Delay(500);

                                // Set the parent of SHELLDLL_DefView to WorkerW
                                _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
                            }
                            break;
                        }
                }
            };
        }
    }

    private static DateTime lastSettingChangeTime = DateTime.MinValue; // Store the last time

    public static unsafe HWND GetVisibleWorkerW(HWND hWndProgman)
    {
        HWND hWorkerW = HWND.Null;

        PInvoke.EnumChildWindows(hWndProgman, EnumChildWindowsProc, IntPtr.Zero);

        return hWorkerW;

        unsafe BOOL EnumChildWindowsProc(HWND hwnd, LPARAM param)
        {
            Span<char> className = new char[64];

            _ = PInvoke.GetClassName(hwnd, className);

            // Check if it's a WorkerW and it's not hidden
            if (className.ToString() == "WorkerW" && PInvoke.IsWindowVisible(hwnd))
            {
                hWorkerW = hwnd;
                return false; // Stop enumeration once we find the visible WorkerW
            }
            return true; // Continue enumeration
        }
    }

    public static unsafe HWND GetWorkerW(out int x, out int y)
    {
        var foundWorkerW = new LPARAM(0);
        _ = PInvoke.EnumWindows(EnumWindowsProc, foundWorkerW);
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

        _ = PInvoke.GetClassName(param0, className);

        if (className.ToString() == WORKERW)
        {
            var hProgman = PInvoke.FindWindow(PROGMAN, null);
            _ = PInvoke.SendMessageTimeout(hProgman, WM_SPAWN_WORKERW, new(0), IntPtr.Zero, SMTO_NORMAL, 250);

            uint pid1;
            uint pid2;

            _ = PInvoke.GetWindowThreadProcessId(hProgman, &pid1);
            _ = PInvoke.GetWindowThreadProcessId(param0, &pid2);

            if (pid1 == pid2)
            {
                _ = PInvoke.GetWindowRect(param0, out var rect);
                var screenRight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN); // SM_CXSCREEN
                var screenBottom = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN); // SM_CYSCREEN

                if (rect.right >= screenRight && rect.bottom >= screenBottom)
                {
                    return false; // Stop enumeration
                }
            }
        }

        return true; // Continue enumeration
    }
}