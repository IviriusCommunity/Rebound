// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using TerraFX.Interop.Windows;
using HWND = TerraFX.Interop.Windows.HWND;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Core.UI;

public static class WindowHelper
{
    public static unsafe void Activate(this AppWindow window)
    {
        var hWnd = new HWND((void*)Win32Interop.GetWindowFromWindowId(window.Id));

        if (TerraFX.Interop.Windows.Windows.IsIconic(new(hWnd.Value)) != 0) // if minimized
        {
            TerraFX.Interop.Windows.Windows.ShowWindow(new(hWnd.Value), SW.SW_RESTORE); // restore window
        }
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(new(hWnd.Value)); // bring to front
    }

    public static void ForceBringToFront(this IslandsWindow window)
    {
        ForceBringToFront(window.Handle);
    }

    public static unsafe void ForceBringToFront(HWND hWnd)
    {
        // 1. Disable foreground lock timeout
        uint lockTimeout = 0;
        TerraFX.Interop.Windows.Windows.SystemParametersInfoW(0x2000, 0, &lockTimeout, 0);
        TerraFX.Interop.Windows.Windows.SystemParametersInfoW(0x2001, 0, (void*)0, 0x0002);

        // 2. Get current foreground window and its thread
        var currentForeground = TerraFX.Interop.Windows.Windows.GetForegroundWindow();
        uint currentForegroundThread = TerraFX.Interop.Windows.Windows.GetWindowThreadProcessId(currentForeground, null);
        uint currentThreadId = TerraFX.Interop.Windows.Windows.GetCurrentThreadId();
        uint targetThreadId = TerraFX.Interop.Windows.Windows.GetWindowThreadProcessId(hWnd, null);

        // 3. Attach to ALL relevant threads
        if (currentForegroundThread != 0 && currentThreadId != currentForegroundThread)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(currentThreadId, currentForegroundThread, true);
        }
        if (targetThreadId != 0 && currentThreadId != targetThreadId)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(currentThreadId, targetThreadId, true);
        }
        if (targetThreadId != 0 && currentForegroundThread != 0 && targetThreadId != currentForegroundThread)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(targetThreadId, currentForegroundThread, true);
        }

        // 4. Manipulate the window state aggressively
        TerraFX.Interop.Windows.Windows.ShowWindow(hWnd, SW.SW_HIDE);
        TerraFX.Interop.Windows.Windows.ShowWindow(hWnd, SW.SW_SHOWMINIMIZED);
        TerraFX.Interop.Windows.Windows.ShowWindow(hWnd, SW.SW_SHOWNORMAL);
        TerraFX.Interop.Windows.Windows.ShowWindow(hWnd, SW.SW_RESTORE);
        TerraFX.Interop.Windows.Windows.ShowWindow(hWnd, SW.SW_SHOW);

        // 5. Enable window
        TerraFX.Interop.Windows.Windows.EnableWindow(hWnd, true);

        // 6. Use AllowSetForegroundWindow (running as admin helps here)
        uint processId = 0;
        TerraFX.Interop.Windows.Windows.GetWindowThreadProcessId(hWnd, &processId);
        TerraFX.Interop.Windows.Windows.AllowSetForegroundWindow(processId);

        // 7. Force to topmost and back
        TerraFX.Interop.Windows.Windows.SetWindowPos(hWnd, new HWND((void*)-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        TerraFX.Interop.Windows.Windows.SetWindowPos(hWnd, new HWND((void*)-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);

        // 8. Multiple activation attempts
        TerraFX.Interop.Windows.Windows.BringWindowToTop(hWnd);
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(hWnd);
        TerraFX.Interop.Windows.Windows.SetActiveWindow(hWnd);
        TerraFX.Interop.Windows.Windows.SetFocus(hWnd);

        // 9. Send activation messages directly
        TerraFX.Interop.Windows.Windows.SendMessageW(hWnd, 0x0006, 1, 0); // WM_ACTIVATE (WA_ACTIVE)
        TerraFX.Interop.Windows.Windows.SendMessageW(hWnd, 0x0086, 1, 0); // WM_NCACTIVATE

        // 10. Simulate Alt key press (this can help grab focus)
        var inputs = stackalloc INPUT[4];

        // Alt down
        inputs[0].type = 1; // INPUT_KEYBOARD
        inputs[0].Anonymous.ki.wVk = 0x12; // VK_MENU (Alt)
        inputs[0].Anonymous.ki.dwFlags = 0;

        // Alt up
        inputs[1].type = 1;
        inputs[1].Anonymous.ki.wVk = 0x12;
        inputs[1].Anonymous.ki.dwFlags = 0x0002; // KEYEVENTF_KEYUP

        TerraFX.Interop.Windows.Windows.SendInput(2, inputs, sizeof(INPUT));

        // 11. Force focus again after Alt trick
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(hWnd);
        TerraFX.Interop.Windows.Windows.SetFocus(hWnd);

        // 12. Redraw the window
        TerraFX.Interop.Windows.Windows.RedrawWindow(hWnd, null, new HRGN((void*)0), 0x0001 | 0x0004); // RDW_INVALIDATE | RDW_UPDATENOW

        // 13. Detach all threads
        if (currentForegroundThread != 0 && currentThreadId != currentForegroundThread)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(currentThreadId, currentForegroundThread, false);
        }
        if (targetThreadId != 0 && currentThreadId != targetThreadId)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(currentThreadId, targetThreadId, false);
        }
        if (targetThreadId != 0 && currentForegroundThread != 0 && targetThreadId != currentForegroundThread)
        {
            TerraFX.Interop.Windows.Windows.AttachThreadInput(targetThreadId, currentForegroundThread, false);
        }

        // 14. Restore foreground lock timeout
        TerraFX.Interop.Windows.Windows.SystemParametersInfoW(0x2001, 0, &lockTimeout, 0x0002);
    }
}