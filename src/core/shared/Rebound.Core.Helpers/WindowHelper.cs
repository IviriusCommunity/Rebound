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

    public static void ForceBringToFront(this IslandsWindow window)
    {
        var thisThreadId = TerraFX.Interop.Windows.Windows.GetCurrentThreadId();
        var foregroundHwnd = TerraFX.Interop.Windows.Windows.GetForegroundWindow();
        uint lpdwProcessId;
        unsafe
        {
            var foregroundThreadId = TerraFX.Interop.Windows.Windows.GetWindowThreadProcessId(foregroundHwnd, &lpdwProcessId);

            if (thisThreadId != foregroundThreadId)
            {
                // Attach input to foreground thread
                TerraFX.Interop.Windows.Windows.AttachThreadInput(foregroundThreadId, thisThreadId, true);

                // Ensure window is shown
                TerraFX.Interop.Windows.Windows.ShowWindow(window.Handle, TerraFX.Interop.Windows.SW.SW_SHOW);

                // Try to bring it to foreground
                TerraFX.Interop.Windows.Windows.SetForegroundWindow(window.Handle);

                // Detach input after done
                TerraFX.Interop.Windows.Windows.AttachThreadInput(foregroundThreadId, thisThreadId, false);
            }
            else
            {
                // Same thread, simpler path
                TerraFX.Interop.Windows.Windows.ShowWindow(window.Handle, TerraFX.Interop.Windows.SW.SW_SHOW);
                TerraFX.Interop.Windows.Windows.SetForegroundWindow(window.Handle);
            }
        }

        TerraFX.Interop.Windows.Windows.ShowWindow(window.Handle, TerraFX.Interop.Windows.SW.SW_RESTORE);
        if (TerraFX.Interop.Windows.Windows.IsIconic(window.Handle) != 0) // if minimized
        {
            TerraFX.Interop.Windows.Windows.ShowWindow(window.Handle, TerraFX.Interop.Windows.SW.SW_RESTORE); // restore window
        }
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(window.Handle);
        TerraFX.Interop.Windows.Windows.BringWindowToTop(window.Handle);
    }
}