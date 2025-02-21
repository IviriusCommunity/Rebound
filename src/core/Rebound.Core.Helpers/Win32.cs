using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;
using static Rebound.Helpers.User32;

namespace Rebound.Helpers;

public static partial class User32
{
    // Custom types for better abstraction
    public struct WndHandle(IntPtr handle)
    {
        public IntPtr Handle = handle;
    }

    public struct Style(int value)
    {
        public int Value = value;
    }

    public struct Msg(uint value)
    {
        public uint Value = value;
    }

    // Constants for Win32 messages and styles
    public static readonly Msg WM_MOUSEMOVE = new(0x0200);
    public static readonly Msg WM_LBUTTONUP = new(0x0202);

    public static readonly Style GWL_EXSTYLE = new(-20);
    public static readonly Style WS_EX_LAYERED = new(0x00080000);
    public static readonly Style WS_EX_TRANSPARENT = new(0x00000020);

    // API calls using abstracted types
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int GetWindowLong(WndHandle hWnd, Style nIndex);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int SetWindowLong(WndHandle hWnd, Style nIndex, int dwNewLong);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SendMessage(WndHandle hWnd, Msg Msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetParent(WndHandle hWndChild, WndHandle hWndNewParent);
}

public static partial class Shell32
{
    // Custom types for better abstraction
    public struct IconHandle(IntPtr handle)
    {
        public IntPtr Handle = handle;
    }

    public struct NotificationFlags(uint flags)
    {
        public uint Flags = flags;
    }

    public struct CallbackMessage(uint message)
    {
        public uint Message = message;
    }

    public struct NotificationId(uint id)
    {
        public uint ID = id;
    }

    // Constants using abstracted types
    public static readonly NotificationFlags NIF_ICON = new(0x00000002);
    public static readonly NotificationFlags NIF_MESSAGE = new(0x00000001);
    public static readonly NotificationFlags NIF_TIP = new(0x00000004);

    public static readonly NotificationId NIM_ADD = new(0x00000000);
    public static readonly NotificationId NIM_DELETE = new(0x00000002);

    // Define NOTIFYICONDATA using custom types
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct NOTIFYICONDATA
    {
        public uint cbSize;
        public WndHandle hWnd;
        public NotificationId uID;
        public NotificationFlags uFlags;
        public CallbackMessage uCallbackMessage;
        public IconHandle hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    // API calls using abstracted types
    [LibraryImport("shell32.dll")]
    public static partial int Shell_NotifyIcon(NotificationId dwMessage, ref IntPtr pnid);

    public static int ShellNotifyIcon(NotificationId dwMessage, ref NOTIFYICONDATA pnid)
    {
        // Marshal the struct into unmanaged memory
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NOTIFYICONDATA>());
        try
        {
            Marshal.StructureToPtr(pnid, ptr, false);
            return Shell_NotifyIcon(dwMessage, ref ptr);
        }
        finally
        {
            // Free the allocated memory after the operation
            Marshal.FreeHGlobal(ptr);
        }
    }
}

public static class Win32
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr RegOpenKeyEx(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int RegCloseKey(IntPtr hKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, uint dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    public const int HORZRES = 8; // Horizontal width of the display
    public const int VERTRES = 10; // Vertical height of the display
    public const int LOGPIXELSX = 88; // Logical pixels/inch in X

    [DllImport("dwmapi.dll", SetLastError = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public static void SetDarkMode(WindowEx window, Application app)
    {
        var i = 1;
        if (app.RequestedTheme == ApplicationTheme.Light)
        {
            i = 0;
        }
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
        CheckTheme();
        async void CheckTheme()
        {
            await Task.Delay(100);
            try
            {
                if (app != null)
                {
                    var i = 1;
                    if (app.RequestedTheme == ApplicationTheme.Light)
                    {
                        i = 0;
                    }
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    _ = DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
                    CheckTheme();
                }
            }
            catch
            {

            }
        }
    }

    public static int GET_X_LPARAM(IntPtr lParam) => unchecked((short)(long)lParam);

    public static int GET_Y_LPARAM(IntPtr lParam) => unchecked((short)((long)lParam >> 16));
}
