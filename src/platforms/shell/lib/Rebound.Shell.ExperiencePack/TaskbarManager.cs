// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using TerraFX.Interop.Windows;
using Rebound.Core;
using System.Threading.Tasks;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.SWP;
using Rebound.Core.Helpers;

#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.Shell.ExperiencePack;

public static class TaskbarManager
{
    /// <summary>
    /// Handle to the Windows taskbar.
    /// </summary>
    private static HWND _taskbarHandle = HWND.NULL;

    /// <summary>
    /// Cached visibility state of the taskbar before hiding.
    /// </summary>
    private static bool _wasTaskbarVisible;

    /// <summary>
    /// Original AppBar data before hiding the taskbar.
    /// </summary>
    private static APPBARDATA _originalAppBarData;

    /// <summary>
    /// Whether the taskbar is currently hidden.
    /// </summary>
    private static bool _isTaskbarHidden;

    /// <summary>
    /// Hides the Windows taskbar completely, including its reserved AppBar area.
    /// </summary>
    public static void HideTaskbar()
    {
        unsafe
        {
            // Find the taskbar window
            _taskbarHandle = FindWindowW("Shell_TrayWnd".ToPointer(), null);

            // Taskbar not found
            if (_taskbarHandle == HWND.NULL)
                return;

            // Check if taskbar is currently visible
            _wasTaskbarVisible = IsWindowVisible(_taskbarHandle);
        }

        // Only proceed if the taskbar was visible
        if (_wasTaskbarVisible)
        {
            unsafe
            {
                // Query the current AppBar state and store it
                APPBARDATA tempAbd = new()
                {
                    cbSize = (uint)sizeof(APPBARDATA),
                    hWnd = _taskbarHandle
                };
                SHAppBarMessage(ABM.ABM_GETTASKBARPOS, &tempAbd);

                // Store the original state
                _originalAppBarData = tempAbd;

                // Set taskbar to auto-hide state first (this releases the work area)
                APPBARDATA autoHideAbd = new()
                {
                    cbSize = (uint)sizeof(APPBARDATA),
                    hWnd = _taskbarHandle,
                    lParam = (LPARAM)1 // ABS_AUTOHIDE
                };
                SHAppBarMessage(ABM.ABM_SETSTATE, &autoHideAbd);

                // Small delay to let it process
                System.Threading.Thread.Sleep(50);

                // Now hide the window completely
                ShowWindow(_taskbarHandle, SW.SW_HIDE);

                // Remove the AppBar registration entirely
                APPBARDATA removeAbd = new()
                {
                    cbSize = (uint)sizeof(APPBARDATA),
                    hWnd = _taskbarHandle
                };
                SHAppBarMessage(ABM.ABM_REMOVE, &removeAbd);

                // Get the monitor bounds
                var monitorArea = Display.GetDisplayArea(_taskbarHandle);

                // Set work area to full screen
                SystemParametersInfoW(SPI.SPI_SETWORKAREA, 0, &monitorArea, SPIF_SENDCHANGE);
            }

            _isTaskbarHidden = true;

            Task.Run(async () =>
            {
                while (_isTaskbarHidden)
                {
                    unsafe
                    {
                        // Remove WS_VISIBLE
                        var style = GetWindowLongPtrW(_taskbarHandle, GWL.GWL_STYLE);
                        style &= ~WS.WS_VISIBLE;
                        style &= ~WS.WS_DLGFRAME;
                        SetWindowLongPtrW(_taskbarHandle, GWL.GWL_STYLE, (int)style);

                        // Hide the window
                        ShowWindow(_taskbarHandle, SW.SW_HIDE);

                        // Update the window to apply styles
                        SetWindowPos(_taskbarHandle, HWND.NULL, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }
            });
        }
    }

    /// <summary>
    /// Restores the Windows taskbar to its previous state.
    /// </summary>
    public static unsafe void ShowTaskbar()
    {
        _isTaskbarHidden = false;

        // Taskbar handle not stored
        if (_taskbarHandle == HWND.NULL)
            return;

        // If the taskbar was visible, restore it
        if (_wasTaskbarVisible)
        {
            // Re-register the AppBar first
            APPBARDATA abd = new()
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = _taskbarHandle
            };
            SHAppBarMessage(ABM.ABM_NEW, &abd);

            // Restore the AppBar position
            abd.uEdge = _originalAppBarData.uEdge;
            abd.rc = _originalAppBarData.rc;
            SHAppBarMessage(ABM.ABM_QUERYPOS, &abd);
            SHAppBarMessage(ABM.ABM_SETPOS, &abd);

            // Restore auto-hide state to off
            APPBARDATA stateAbd = new()
            {
                cbSize = (uint)sizeof(APPBARDATA),
                hWnd = _taskbarHandle,
                lParam = (LPARAM)0 // Remove auto-hide
            };
            SHAppBarMessage(ABM.ABM_SETSTATE, &stateAbd);

            // Show the taskbar window
            ShowWindow(_taskbarHandle, SW.SW_SHOW);

            RECT rectTemp;

            // Restore original work area
            SystemParametersInfoW(SPI.SPI_SETWORKAREA, 0, &rectTemp, SPIF_SENDCHANGE);
        }

        // Reset state
        _taskbarHandle = HWND.NULL;
        _wasTaskbarVisible = false;
    }

    /// <summary>
    /// Checks if the taskbar is currently hidden by this manager.
    /// </summary>
    public static bool IsTaskbarHidden()
    {
        return _taskbarHandle != HWND.NULL && _wasTaskbarVisible;
    }
}