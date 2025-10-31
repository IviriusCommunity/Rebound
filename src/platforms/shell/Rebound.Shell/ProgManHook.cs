// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rebound.Shell.ExperienceHost;

internal static class ProgManHook
{
    /*public static Bitmap CaptureScreenArea(Rectangle area)
    {
        var hWndProgman = PInvoke.FindWindow("Progman", null);
        var hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, HWND.Null, "SHELLDLL_DefView", null);
        var hdcSrc = PInvoke.GetDC(hSHELLDLL_DefView);
        var hdcDest = PInvoke.CreateCompatibleDC(hdcSrc);
        var hBitmap = PInvoke.CreateCompatibleBitmap(hdcSrc, area.Width, area.Height);
        var hOld = PInvoke.SelectObject(hdcDest, hBitmap);

        _ = PInvoke.BitBlt(
            hdcDest,
            0, 0,
            area.Width, area.Height,
            hdcSrc,
            area.X, area.Y,
            Windows.Win32.Graphics.Gdi.ROP_CODE.SRCCOPY
        );

        PInvoke.SelectObject(hdcDest, hOld);
        PInvoke.DeleteDC(hdcDest);
        _ = PInvoke.ReleaseDC(hSHELLDLL_DefView, hdcSrc);

        var bmp = System.Drawing.Image.FromHbitmap(hBitmap);
        PInvoke.DeleteObject(hBitmap);
        return bmp;
    }*/

    //public static async void AttachToProgMan(this IslandsWindow window)
    //{
    //    try
    //    {
    //        // Find Progman
    //        var hWndProgman = PInvoke.FindWindow("Progman", null);
    //        if (hWndProgman == HWND.Null)
    //        {
    //            return;
    //        }

    //        // Find the SHELLDLL_DefView window
    //        var hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, HWND.Null, "SHELLDLL_DefView", null);
    //        if (hSHELLDLL_DefView == HWND.Null)
    //        {
    //            // TODO: fallback search inside WorkerW windows
    //            return;
    //        }

    //        // Find the SysListView32 window inside SHELLDLL_DefView
    //        var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, HWND.Null, "SysListView32", "FolderView");
    //        if (hSysListView32 != HWND.Null)
    //        {
    //            PInvoke.ShowWindow(hSysListView32, SHOW_WINDOW_CMD.SW_HIDE);
    //        }

    //        // Current window's handle
    //        HWND hWndWindow = window.Handle.ToCsWin32HWND();

    //        // Set the parent of the current window to SHELLDLL_DefView
    //        PInvoke.SetParent(hWndWindow, hWndProgman);

    //        // Magic undocumented message
    //        unsafe
    //        {
    //            var hWorkerW = PInvoke.FindWindowEx(hWndProgman, HWND.Null, "WorkerW", null);
    //            if (SettingsHelper.GetValue($"EnableLivelyWallpaperCompatibility", "rshell.desktop", false) == false)
    //            {
    //                if (hWorkerW == HWND.Null)
    //                {
    //                    PInvoke.SendMessageTimeout(
    //                        hWndProgman,
    //                        0x052C, // 0x052C == "Spawn WorkerW"
    //                        new WPARAM(0),
    //                        new LPARAM(0),
    //                        SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL,
    //                        1000
    //                    );
    //                }
    //            }
    //        }

    //        /*// Set extended styles for the current window
    //        var style = (int)WINDOW_EX_STYLE.WS_EX_LAYERED | (int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
    //        _ = PInvoke.SetWindowLong(hWndWindow, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);

    //        // Enable transparency to allow rendering of the desktop wallpaper
    //        PInvoke.SetLayeredWindowAttributes(hWndWindow, new COLORREF(0), 0, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_COLORKEY);*/
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"AttachToProgMan failed: {ex.Message}");
    //    }
    //}
}