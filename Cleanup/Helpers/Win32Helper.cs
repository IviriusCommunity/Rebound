using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using WinUIEx.Messaging;
using WinUIEx;

namespace Rebound.Cleanup.Helpers;
public static class Win32Helper
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
}