using System;
using WinUIEx;

namespace Rebound.Helpers.Windowing;

public static class WindowHelper
{
    public static void SetWindowIcon(this WindowEx window, string iconPath)
    {
        window.SetIcon(iconPath);
        window.SetTaskBarIcon(Icon.FromFile(iconPath));
    }

    public static void TurnOffDoubleClick(this WindowEx window)
    {
        var windowManager = WindowManager.Get(window);
        windowManager.WindowMessageReceived += WindowManager_WindowMessageReceived;

        void WindowManager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == 0x00A3) // WM_NCLBUTTONDBLCLK
            {
                // Prevent double-click from maximizing the window
                e.Result = IntPtr.Zero;
                e.Handled = true;
                return;
            }
        }
    }
}