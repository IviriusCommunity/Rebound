using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    public static unsafe void AttachToProgMan(this WindowEx window)
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

        // Get SHELLDLL_DefView handle
        hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);

        // Send message to progman to create WorkerW
        _ = PInvoke.SendMessageTimeout(hWndProgman, 0x052C, 0, 0, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 250, null);
        Thread.Sleep(250);

        // Find WorkerW handle
        hWorkerW = version >= 26100 ? PInvoke.FindWindowEx(hWndProgman, new(null), "WorkerW", null) : GetWorkerW(out _, out _);

        _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000));
        _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0x20000880);

        // Set the parent of WorkerW to Progman
        _ = PInvoke.SetParent(hWorkerW, hWndProgman);

        // Set the parent of Rebound Desktop to SHELLDLL_DefView
        _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

        // Set the parent of SHELLDLL_DefView to WorkerW
        _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);

        // Find SysListView32 handle and hide it
        var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
        if (hSysListView32 != HWND.Null)
        {
            _ = PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
        }

        /*if (version >= 26100)
        {
            // Register for session notifications
            _ = PInvoke.WTSRegisterSessionNotification(new(window.GetWindowHandle()), NOTIFY_FOR_THIS_SESSION);

            // WndProc
            var winManager = WindowManager.Get(window);
            winManager.WindowMessageReceived += (sender, e) =>
            {
                if (e.Message.MessageId is WM_WTSSESSION_CHANGE or WM_SETTINGCHANGE)
                {
                    Thread.Sleep(250);

                    // Obtain new WorkerW handle
                    hWorkerW = GetWorkerW(out _, out _);

                    _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((nint)0x96000000));
                    _ = PInvoke.SetWindowLongPtr(hWorkerW, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0x20000880);

                    // Set the parent of WorkerW to Progman
                    _ = PInvoke.SetParent(hWorkerW, hWndProgman);

                    // Set the parent of Rebound Desktop to SHELLDLL_DefView
                    _ = PInvoke.SetParent(new(window.GetWindowHandle()), hSHELLDLL_DefView);

                    // Set the parent of SHELLDLL_DefView to WorkerW
                    _ = PInvoke.SetParent(hSHELLDLL_DefView, hWorkerW);
                }
            };
        }*/

        /*// Store the old window procedure
        var oldWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.GetWindowLongPtr(hWndProgman, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC));

        // Define the new window procedure
        LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            if (msg == 0x0047)
            {
                var hWndTaskbar = PInvoke.FindWindow("Shell_TrayWnd", null);
                PInvoke.SetWindowPos(hWndTaskbar, hWndProgman, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
            }

            return PInvoke.CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }

        // Ensure the delegate remains alive
        var newWndProc = new WNDPROC(WndProc);

        // Set the new window procedure
        PInvoke.SetWindowLongPtr(hWndProgman, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(newWndProc));*/

        /*var manager = WindowManager.Get(window);
        manager.WindowMessageReceived += (sender, e) =>
        {
            if (e.Message.MessageId == 0x0047)
            {
                var hWndTaskbar = PInvoke.FindWindow("Shell_TrayWnd", null);
                PInvoke.SetWindowPos(hWndTaskbar, hWndProgman, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
            }
        };*/

        /*var hook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_SHELL, new HOOKPROC(ShellProc), HINSTANCE.Null, 0);

        LRESULT ShellProc(int nCode, WPARAM wParam, LPARAM lParam)
        {
            if (nCode == 0x0047)
            {
                //var hWndTaskbar = PInvoke.FindWindow("Shell_TrayWnd", null);
                //PInvoke.SetWindowPos(hWndTaskbar, hSHELLDLL_DefView, 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
            }
            return PInvoke.CallNextHookEx(HHOOK.Null, nCode, wParam, lParam);
        };*/

        // On closed, restart explorer to revert changes
        window.Closed += Window_Closed;
        void Window_Closed(object sender, WindowEventArgs args)
        {
            //PInvoke.UnhookWindowsHookEx(hook);
            Process.GetProcessesByName("explorer")[0].Kill();
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