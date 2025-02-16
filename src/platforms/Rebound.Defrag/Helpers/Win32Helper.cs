using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using WinUIEx;
using WinUIEx.Messaging;

#nullable enable

namespace Rebound.Defrag.Helpers;

public static partial class Win32Helper
{
    public const int WM_DEVICECHANGE = 0x0219;
    public const int DBT_DEVICEARRIVAL = 0x8000;
    public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetLogicalDrives();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool GetVolumeInformation(
        string lpRootPathName,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern DriveType GetDriveType(string lpRootPathName);

    public enum DriveType : uint
    {
        DRIVE_UNKNOWN = 0,
        DRIVE_NO_ROOT_DIR = 1,
        DRIVE_REMOVABLE = 2,
        DRIVE_FIXED = 3,
        DRIVE_REMOTE = 4,
        DRIVE_CDROM = 5,
        DRIVE_RAMDISK = 6
    }

    public static void RemoveIcon(WindowEx window)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        _ = SetWindowLongPtr(hWnd, -20, 0x00000001L);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong);

    [DllImport("dwmapi.dll", SetLastError = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public static void SetDarkMode(WindowEx window)
    {
        var i = 1;
        if (App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light)
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
                var i = 1;
                if (App.Current.RequestedTheme == Microsoft.UI.Xaml.ApplicationTheme.Light)
                {
                    i = 0;
                }
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                _ = DwmSetWindowAttribute(hWnd, 20, ref i, sizeof(int));
                CheckTheme();
            }
            catch
            {

            }
        }
    }

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    private const int GWL_HWNDPARENT = -8;

    private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => IntPtr.Size == 4 ? SetWindowLongPtr32(hWnd, nIndex, dwNewLong) : SetWindowLongPtr64(hWnd, nIndex, dwNewLong);

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static void CreateModalWindow(WindowEx parentWindow, WindowEx childWindow, bool summonWindowAutomatically = true, bool blockInput = false)
    {
        var hWndChildWindow = WinRT.Interop.WindowNative.GetWindowHandle(childWindow);
        var hWndParentWindow = WinRT.Interop.WindowNative.GetWindowHandle(parentWindow);
        _ = SetWindowLong(hWndChildWindow, GWL_HWNDPARENT, hWndParentWindow);
        ((OverlappedPresenter)childWindow.AppWindow.Presenter).IsModal = true;
        if (blockInput == true)
        {
            _ = EnableWindow(hWndParentWindow, false);
            childWindow.Closed += ChildWindow_Closed;
            void ChildWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
            {
                _ = EnableWindow(hWndParentWindow, true);
            }
        }
        if (summonWindowAutomatically == true)
        {
            _ = childWindow.Show();
        }

        WindowMessageMonitor _msgMonitor;

        _msgMonitor = new WindowMessageMonitor(childWindow);
        _msgMonitor.WindowMessageReceived += (_, e) =>
        {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            if (e.Message.MessageId == WM_NCLBUTTONDBLCLK)
            {
                // Disable double click on title bar to maximize window
                e.Result = 0;
                e.Handled = true;
            }
        };
    }
}
