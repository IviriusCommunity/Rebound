// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using Windows.Foundation;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopWindow : WindowEx
{
    private readonly Action? onClosedCallback;

    private readonly Action<Point>? createContextMenuCallback;

    public DesktopWindow(Action? onClosed = null, Action<Point>? createContextMenu = null)
    {
        onClosedCallback = onClosed;
        createContextMenuCallback = createContextMenu;
        InitializeComponent();
        this.ToggleWindowStyle(false, 
            WindowStyle.SizeBox | WindowStyle.Caption | 
            WindowStyle.MaximizeBox | WindowStyle.MinimizeBox | 
            WindowStyle.Border | WindowStyle.Iconic | 
            WindowStyle.SysMenu);
        this.MoveAndResize(0, 0, Display.GetDPIAwareDisplayRect(this).Width, Display.GetDPIAwareDisplayRect(this).Height);
        RootFrame.Navigate(typeof(DesktopPage), this);
        SystemBackdrop = new TransparentTintBackdrop();
    }

    private bool canClose;

    public void CreateContextMenuAtPosition(Point position)
    {
        createContextMenuCallback?.Invoke(position);
    }

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
        }
        else
        {
            try
            {
                // Get progman handle
                var hWndProgman = PInvoke.FindWindow("Progman", null);

                Windows.Win32.Foundation.HWND hSHELLDLL_DefView;
                unsafe
                {
                    // Get SHELLDLL_DefView handle
                    hSHELLDLL_DefView = PInvoke.FindWindowEx(hWndProgman, new(null), "SHELLDLL_DefView", null);
                }
                Windows.Win32.Foundation.HWND hSysListView32;
                unsafe
                {
                    hSysListView32 = PInvoke.FindWindowEx(hSHELLDLL_DefView, new(null), "SysListView32", "FolderView");
                }
                _ = PInvoke.ShowWindow(hSysListView32, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOW);
            }
            catch
            {

            }
            //Process.GetProcessesByName("explorer").FirstOrDefault()?.Kill();
            PInvoke.DestroyWindow(new(this.GetWindowHandle()));
        }
    }
}