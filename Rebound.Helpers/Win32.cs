using System;
using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA1401 // P/Invokes should not be visible

namespace Rebound.Helpers;

public static class Win32
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr RegOpenKeyEx(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int RegCloseKey(IntPtr hKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, uint dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    public static int GET_X_LPARAM(IntPtr lParam)
    {
        return unchecked((short)(long)lParam);
    }

    public static int GET_Y_LPARAM(IntPtr lParam)
    {
        return unchecked((short)((long)lParam >> 16));
    }
}
