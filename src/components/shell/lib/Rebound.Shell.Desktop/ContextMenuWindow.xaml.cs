// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class ContextMenuWindow : WindowEx
{
    private DesktopWindow DesktopWindow { get; set; }

    public DesktopPage DesktopPage { get; set; }

    public ContextMenuWindow(DesktopWindow win)
    {
        DesktopWindow = win;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        IsShownInSwitchers = false;
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
        this.MoveAndResize(0, 0, 0, 0);
        this.ToggleWindowStyle(false,
            WindowStyle.SizeBox | WindowStyle.Caption |
            WindowStyle.MaximizeBox | WindowStyle.MinimizeBox |
            WindowStyle.Border | WindowStyle.Iconic |
            WindowStyle.SysMenu);
        this.ToggleExtendedWindowStyle(true, ExtendedWindowStyle.ToolWindow);
        SystemBackdrop = new TransparentTintBackdrop();
        HWND hWndWindow = new(this.GetWindowHandle());
        var style = PInvoke.GetWindowLong(hWndWindow, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        style |= (int)(WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_TRANSPARENT);
        _ = PInvoke.SetWindowLong(hWndWindow, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);

        PInvoke.SetLayeredWindowAttributes(hWndWindow, new COLORREF(0), 0, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_COLORKEY);
    }

    public void ShowContextMenu(Point pos)
    {
        this.MoveAndResize(pos.X, pos.Y - 36, 0, 0);
        BringToFront();
        Menu.ShowAt(StartPoint, new FlyoutShowOptions()
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
        });
    }

    private bool canClose;

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        canClose = true;
        Close();
        DesktopWindow.RestoreExplorerDesktop();
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        args.Handled = !canClose;
    }

    private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
    {
        canClose = true;
        Close();
        DesktopWindow.RestoreExplorerDesktop();
        await Task.Delay(250).ConfigureAwait(true);
        Process.GetCurrentProcess().Kill();
    }

    private void AppBarButton_Click_2(object sender, RoutedEventArgs e)
    {
        DesktopPage.Refresh();
    }

    private void AppBarButton_Click_3(object sender, RoutedEventArgs e)
    {
        DesktopPage.ShowOptions();
    }
}
