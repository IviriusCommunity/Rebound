using System;
using System.Diagnostics;
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
        window.Closed += Window_Closed;

        void Window_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
        {
            Process.GetProcessesByName("explorer")[0].Kill();
        }
    }
}