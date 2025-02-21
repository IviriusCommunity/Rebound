using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Rebound.Helpers.Shell32;

namespace Rebound.Helpers.SystemComponents;
public partial class TrayIcon
{
    public TrayIcon()
    {

    }

    private IconHandle _iconHandle;

    // Create the system tray icon
    private void CreateTrayIcon()
    {
        var notifyIconData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = new(User32.FindWindow(null, null)), // Get the window handle
            uID = new(1),
            uFlags = new(Shell32.NIF_ICON.Flags | Shell32.NIF_TIP.Flags | Shell32.NIF_MESSAGE.Flags),
            uCallbackMessage = new(User32.WM_LBUTTONUP.Value),
            szTip = "My System Tray Icon",
            hIcon = _iconHandle, // Set the icon here (use your own icon)
        };

        ShellNotifyIcon(NIM_ADD, ref notifyIconData);
    }

    private void CleanupTrayIcon()
    {
        var notifyIconData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = new(User32.FindWindow(null, null)),
            uID = new(1),
        };

        ShellNotifyIcon(NIM_DELETE, ref notifyIconData);
    }
}
