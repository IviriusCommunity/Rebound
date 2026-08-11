// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Rebound.Core.SystemInformation.Hardware;
using TerraFX.Interop.Windows;
using Windows.System;
using WinUIEx;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.UI.Windowing;

public static unsafe class ReboundWindowMenu
{
    private static List<HWND> _handles = [];

    public static void Register(Window window)
    {
        var manager = WindowManager.Get(window);
        var handle = new HWND((void*)window.GetWindowHandle());
        manager.WindowMessageReceived += (s, e) =>
        {
            unsafe
            {
                if (e.Message.MessageId is WM.WM_NCRBUTTONDOWN or WM.WM_NCRBUTTONUP)
                {
                    int hitTest = (int)e.Message.WParam;
                    if (hitTest is HTCAPTION or HTSYSMENU)
                    {
                        e.Handled = true;
                        e.Result = 0;

                        if (e.Message.MessageId == WM.WM_NCRBUTTONUP)
                        {
                            // Nonclient messages carry SCREEN coords directly in lParam
                            int x = GET_X_LPARAM(e.Message.LParam);
                            int y = GET_Y_LPARAM(e.Message.LParam);
                            ShowFlyout();
                        }
                    }
                }
                else if (e.Message.MessageId == WM.WM_SYSCOMMAND)
                {
                    nuint command = e.Message.WParam & 0xFFF0;
                    if (command == SC.SC_KEYMENU)
                    {
                        e.Handled = true;
                        e.Result = 0;

                        ShowFlyoutPos(0, 32);
                    }
                }
            }
        };

        unsafe void ShowFlyout()
        {
            POINT point;
            GetCursorPos(&point);
            var scale = Display.GetScale(new((void*)window.GetWindowHandle()));

            var transform = window.Content.TransformToVisual(null);
            var localPoint = transform.Inverse.TransformPoint(
                new Windows.Foundation.Point((point.x - window.AppWindow!.Position.X) / scale, (point.y - window.AppWindow!.Position.Y) / scale)
            );

            ShowFlyoutPos(localPoint.X, localPoint.Y);
        }
        void ShowFlyoutPos(double x, double y)
        {
            var flyout = new MenuFlyout()
            {
                XamlRoot = window.Content.XamlRoot,
            };

            bool isOverlappedPresenter = false;
            bool isMaximized = false;
            bool isMinimized = false;
            bool isNormal = false;

            // Determine current window state to set initial enabled/disabled toggles
            if (window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                isOverlappedPresenter = true;
                isMaximized = overlappedPresenter.State == OverlappedPresenterState.Maximized;
                isMinimized = overlappedPresenter.State == OverlappedPresenterState.Minimized;
                isNormal = overlappedPresenter.State == OverlappedPresenterState.Restored;
            }

            // Minimize
            var minimizeItem = new MenuFlyoutItem()
            {
                Text = "Minimize",
                IsEnabled = !isMinimized,
                Visibility = isOverlappedPresenter ? Visibility.Visible : Visibility.Collapsed,
                Icon = new FontIcon() { Glyph = "\uE921", Margin = new(2) }
            };
            minimizeItem.Click += (s, e) => window.Minimize();
            flyout.Items.Add(minimizeItem);

            // Maximize
            var maximizeItem = new MenuFlyoutItem()
            {
                Text = "Maximize",
                IsEnabled = !isMaximized,
                Visibility = isOverlappedPresenter ? Visibility.Visible : Visibility.Collapsed,
                Icon = new FontIcon() { Glyph = "\uE922", Margin = new(2) }
            };
            maximizeItem.Click += (s, e) => window.Maximize();
            flyout.Items.Add(maximizeItem);

            // Restore
            var restoreItem = new MenuFlyoutItem()
            {
                Text = "Restore",
                IsEnabled = !isNormal,
                Visibility = isOverlappedPresenter ? Visibility.Visible : Visibility.Collapsed,
                Icon = new FontIcon() { Glyph = "\uE923", Margin = new(2) }
            };
            restoreItem.Click += (s, e) => window.Restore();
            flyout.Items.Add(restoreItem);

            flyout.Items.Add(new MenuFlyoutSeparator());

            // Move (Triggers OS SysCommand for window dragging via Win32)
            var moveItem = new MenuFlyoutItem()
            {
                Text = "Move",
                IsEnabled = isNormal,
                Icon = new FontIcon() { Glyph = "\uE7C2" }
            };
            moveItem.Click += (s, e) => TriggerSysCommand(0xF010 /* SC_MOVE */);
            flyout.Items.Add(moveItem);

            // Resize (Triggers OS SysCommand for window resizing via Win32)
            var resizeItem = new MenuFlyoutItem()
            {
                Text = "Resize",
                IsEnabled = isNormal,
                Icon = new FontIcon() { Glyph = "\uE741" }
            };
            resizeItem.Click += (s, e) => TriggerSysCommand(0xF000 /* SC_SIZE */);
            flyout.Items.Add(resizeItem);

            // More Options Sub-Menu
            var moreOptionsItem = new MenuFlyoutSubItem()
            {
                Text = "More options",
                Icon = new FontIcon() { Glyph = "\uE712" }
            };

            var keepOnTopItem = new ToggleMenuFlyoutItem()
            {
                Text = "Keep on top",
                IsChecked = window.GetIsAlwaysOnTop(),
                Icon = new FontIcon() { Glyph = "\uE74A" }
            };
            var keepBelowItem = new ToggleMenuFlyoutItem()
            {
                Text = "Keep below",
                IsChecked = _handles.Contains(handle),
                Icon = new FontIcon() { Glyph = "\uE74B" }
            };
            keepOnTopItem.Click += (s, e) =>
            {
                if (keepOnTopItem.IsChecked && _handles.Contains(handle))
                {
                    _handles.Remove(handle);
                    manager.ZOrderChanged -= ZOrder_Changed;
                }
                window.SetIsAlwaysOnTop(keepOnTopItem.IsChecked);
            };
            moreOptionsItem.Items.Add(keepOnTopItem);

            keepBelowItem.Click += (s, e) =>
            {
                if (keepBelowItem.IsChecked)
                {
                    window.SetIsAlwaysOnTop(false);
                    _handles.Add(handle);
                    manager.ZOrderChanged += ZOrder_Changed;
                    window.AppWindow.MoveInZOrderAtBottom();
                }
                else
                {
                    _handles.Remove(handle);
                    manager.ZOrderChanged -= ZOrder_Changed;
                }
            };
            moreOptionsItem.Items.Add(keepBelowItem);

            var windowBorderItem = new ToggleMenuFlyoutItem()
            {
                Text = "Window border",
                IsEnabled = window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter1 && !overlappedPresenter1.HasTitleBar,
                IsChecked = window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter2 && overlappedPresenter2.HasBorder,
                Icon = new FontIcon() { Glyph = "\uE739" }
            };
            windowBorderItem.Click += (s, e) =>
            {
                if (window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
                    overlappedPresenter.SetBorderAndTitleBar(windowBorderItem.IsChecked, overlappedPresenter.HasTitleBar);
            };
            moreOptionsItem.Items.Add(windowBorderItem);

            var titleBarItem = new ToggleMenuFlyoutItem()
            {
                Text = "Title bar",
                IsChecked = window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter3 && overlappedPresenter3.HasTitleBar,
                Icon = new FontIcon() { Glyph = "\uE737" }
            };
            titleBarItem.Click += (s, e) =>
            {
                if (window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
                    overlappedPresenter.SetBorderAndTitleBar(titleBarItem.IsChecked ? true : overlappedPresenter.HasBorder, titleBarItem.IsChecked);
            };
            moreOptionsItem.Items.Add(titleBarItem);

            flyout.Items.Add(moreOptionsItem);
            flyout.Items.Add(new MenuFlyoutSeparator());

            // Close
            var closeItem = new MenuFlyoutItem()
            {
                Text = "Close",
                Icon = new FontIcon() { Glyph = "\uE8BB", Margin = new(2) }
            };
            closeItem.KeyboardAccelerators.Add(new()
            {
                Modifiers = VirtualKeyModifiers.Menu,
                Key = VirtualKey.F4
            });
            closeItem.Click += (s, e) => window.Close();
            flyout.Items.Add(closeItem);

            flyout.ShowAt(window.Content, new FlyoutShowOptions
            {
                Position = new(x, y)
            });
        }
        ;

        // Helper to send Move/Resize system commands to the HWND
        void TriggerSysCommand(WPARAM command)
        {
            PostMessageW(new((void*)window.GetWindowHandle()), WM.WM_SYSCOMMAND, command, 0);
        }

        void ZOrder_Changed(object? sender, ZOrderInfo e)
        {
            if (!e.IsZOrderAtBottom)
                window.AppWindow.MoveInZOrderAtBottom();
        }
    }
}