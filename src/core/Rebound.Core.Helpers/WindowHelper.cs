using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Rebound.Helpers;

public static class WindowHelper
{
    public static void RemoveIcon(this WindowEx window)
    {
        PInvoke.SetWindowLongPtr(new HWND(window.GetWindowHandle()), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)0x00000001L);
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