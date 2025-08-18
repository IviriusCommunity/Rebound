// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rebound.Helpers;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopWindow : WindowEx
{
    private readonly Action? onClosedCallback;

    public DesktopWindow(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        this.ToggleWindowStyle(false, 
            WindowStyle.SizeBox | WindowStyle.Caption | 
            WindowStyle.MaximizeBox | WindowStyle.MinimizeBox | 
            WindowStyle.Border | WindowStyle.Iconic | 
            WindowStyle.SysMenu);
        this.MoveAndResize(0, 0, Display.GetDPIAwareDisplayRect(this).Width, Display.GetDPIAwareDisplayRect(this).Height);
        RootFrame.Navigate(typeof(DesktopPage), this);
        SystemBackdrop = new TransparentTintBackdrop();
        //this.ZOrderChanged += DesktopWindow_ZOrderChanged;
    }

    private void DesktopWindow_ZOrderChanged(object? sender, ZOrderInfo e)
    {
        if (!e.IsZOrderAtTop)
        {
            // Find Progman
            var hWndProgman = PInvoke.FindWindow("Progman", null);

            PInvoke.SetWindowPos(
                new(this.GetWindowHandle()),
                PInvoke.GetTopWindow(hWndProgman),
                0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }
    }

    public bool canClose;

    public void RestoreExplorerDesktop()
    {
        canClose = true;
        Close();
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        args.Handled = true;

        if (!canClose)
        {
            onClosedCallback?.Invoke();
            return;
        }

        try
        {
            // Get Progman handle
            var hWndProgman = PInvoke.FindWindow("Progman", null);
            if (hWndProgman == HWND.Null)
            {
                return;
            }

            // Get SHELLDLL_DefView handle
            var hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, HWND.Null, "SHELLDLL_DefView", null);
            if (hSHELLDLL_DefView == HWND.Null)
            {
                return;
            }

            // Get SysListView32 ("FolderView") handle
            var hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, HWND.Null, "SysListView32", "FolderView");
            if (hSysListView32 != HWND.Null)
            {
                PInvoke.ShowWindow(hSysListView32, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOW);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WindowEx_Closed error: {ex.Message}");
        }

        // Destroy this window
        HWND hWnd = new(this.GetWindowHandle());
        PInvoke.DestroyWindow(hWnd);
    }
}