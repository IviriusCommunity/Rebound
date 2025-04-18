// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
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
        this.MoveAndResize(0, 0, Display.GetDisplayRect(this).Width, Display.GetDPIAwareDisplayRect(this).Height);
        RootFrame.Navigate(typeof(DesktopPage), this);
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
            Process.GetProcessesByName("explorer").FirstOrDefault()?.Kill();
            PInvoke.DestroyWindow(new(this.GetWindowHandle()));
        }
    }
}