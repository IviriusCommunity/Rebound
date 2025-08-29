// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Core.Helpers;

public static class WindowHelper
{
    public static void Activate(this AppWindow window)
    {
        var hWnd = new HWND(Win32Interop.GetWindowFromWindowId(window.Id));

        if (TerraFX.Interop.Windows.Windows.IsIconic(hWnd.ToTerraFXHWND()) != 0) // if minimized
        {
            TerraFX.Interop.Windows.Windows.ShowWindow(hWnd.ToTerraFXHWND(), TerraFX.Interop.Windows.SW.SW_RESTORE); // restore window
        }
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(hWnd.ToTerraFXHWND()); // bring to front
    }

    public static void ForceBringToFront(this AppWindow window)
    {
        var hWnd = new HWND(Win32Interop.GetWindowFromWindowId(window.Id));

        var thisThreadId = PInvoke.GetCurrentThreadId();
        var foregroundHwnd = PInvoke.GetForegroundWindow();
        uint lpdwProcessId;
        unsafe
        {
            var foregroundThreadId = PInvoke.GetWindowThreadProcessId(foregroundHwnd, &lpdwProcessId);

            if (thisThreadId != foregroundThreadId)
            {
                // Attach input to foreground thread
                PInvoke.AttachThreadInput(foregroundThreadId, thisThreadId, true);

                // Ensure window is shown
                PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);

                // Try to bring it to foreground
                PInvoke.SetForegroundWindow(hWnd);

                // Detach input after done
                PInvoke.AttachThreadInput(foregroundThreadId, thisThreadId, false);
            }
            else
            {
                // Same thread, simpler path
                PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.SetForegroundWindow(hWnd);
            }
        }

        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_RESTORE);
        window.Activate();
        PInvoke.BringWindowToTop(hWnd);
    }
}