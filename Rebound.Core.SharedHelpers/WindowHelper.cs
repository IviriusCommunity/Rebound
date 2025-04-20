using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2000 // Dispose objects before losing scope

namespace Rebound.Helpers;

public static class WindowHelper
{
    public static void RemoveTitleBarIcon(this WindowEx window)
    {
        var exStyle = PInvoke.GetWindowLongPtr(new HWND(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        PInvoke.SetWindowLongPtr(new HWND(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle | (nint)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME);

        PInvoke.SetWindowPos(new HWND(window.GetWindowHandle()), HWND.Null, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }

    public static void ConfigureTitleBar(this WindowEx window)
    {
        window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        window.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        var listener = new ThemeListener();
        UpdateWinUITheme(window, listener);
        listener.ThemeChanged += Listener_ThemeChanged;
        void Listener_ThemeChanged(ThemeListener sender) => UpdateTheme(window, listener);
    }

    private static void UpdateWinUITheme(WindowEx window, ThemeListener listener)
    {
        var i = listener.CurrentTheme == ApplicationTheme.Light;
        window.AppWindow.TitleBar.ButtonForegroundColor = i ? Colors.Black : Colors.White;
        window.AppWindow.TitleBar.ButtonHoverForegroundColor = i ? Colors.Black : Colors.White;
        window.AppWindow.TitleBar.ButtonInactiveForegroundColor = i ? Colors.Black : Colors.White;
        window.AppWindow.TitleBar.ButtonPressedForegroundColor = i ? Colors.Black : Colors.White;
    }

    public static void SetDarkMode(this WindowEx window)
    {
        var listener = new ThemeListener();
        UpdateTheme(window, listener);
        listener.ThemeChanged += Listener_ThemeChanged;
        void Listener_ThemeChanged(ThemeListener sender) => UpdateTheme(window, listener);
    }

    private static void UpdateTheme(WindowEx window, ThemeListener listener)
    {
        unsafe
        {
            var i = listener.CurrentTheme == ApplicationTheme.Light ? 0 : 1;
            _ = PInvoke.DwmSetWindowAttribute(new(window.GetWindowHandle()), DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &i, sizeof(int));
        }
    }
}