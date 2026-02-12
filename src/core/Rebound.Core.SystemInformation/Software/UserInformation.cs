// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using TerraFX.Interop.Windows;
using Windows.Win32.Foundation;

namespace Rebound.Core.SystemInformation.Software;

[StructLayout(LayoutKind.Sequential)]
internal struct USER_INFO_2
{
    public PCWSTR usri2_name;
    public PCWSTR usri2_password;
    public uint usri2_password_age;
    public uint usri2_priv;
    public PCWSTR usri2_home_dir;
    public PCWSTR usri2_comment;
    public uint usri2_flags;
    public PCWSTR usri2_script_path;
    public uint usri2_auth_flags;
    public PCWSTR usri2_full_name;
    public PCWSTR usri2_usr_comment;
    public PCWSTR usri2_parms;
    public PCWSTR usri2_workstations;
    public uint usri2_last_logon;
    public uint usri2_last_logoff;
    public uint usri2_acct_expires;
    public uint usri2_max_storage;
    public uint usri2_units_per_week;
    public HANDLE usri2_logon_hours;
    public uint usri2_bad_pw_count;
    public uint usri2_num_logons;
    public PCWSTR usri2_logon_server;
    public uint usri2_country_code;
    public uint usri2_code_page;
}

public static class UserInformation
{
    private static unsafe char* ToPointer(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return valueCharPtr;
        }
    }

    /// <summary>
    /// Retrieves the current user's desktop wallpaper path using the IDesktopWallpaper COM interface.
    /// </summary>
    /// <returns>
    /// A string corresponding to the wallpaper's full path on disk.
    /// </returns>
    public static string? GetWallpaperPath()
    {
        unsafe
        {
            fixed (Guid* clsid = &CLSID.CLSID_DesktopWallpaper)
            fixed (Guid* iid = &IID.IID_IDesktopWallpaper)
            {
                ComPtr<IDesktopWallpaper> desktopWallpaper = default;
                var hr = TerraFX.Interop.Windows.Windows.CoCreateInstance(
                    clsid, null, 0x17, iid, (void**)desktopWallpaper.GetAddressOf());

                if (TerraFX.Interop.Windows.Windows.FAILED(hr)) return null;

                ushort* wallpaperPtr = null;
                hr = desktopWallpaper.Get()->GetWallpaper(null, (char**)&wallpaperPtr);

                if (TerraFX.Interop.Windows.Windows.FAILED(hr)) return null;

                string wallpaperPath = new string((char*)wallpaperPtr);
                TerraFX.Interop.Windows.Windows.CoTaskMemFree(wallpaperPtr);

                if (!string.IsNullOrEmpty(wallpaperPath))
                {
                    return wallpaperPath;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Retrieves the current user's profile picture path from the registry. It looks up the user's SID to find the corresponding registry key under
    /// "SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\<SID>" and attempts to retrieve the path of the profile picture (preferring 
    /// higher resolution images if available).
    /// </summary>
    /// <returns>
    /// A string representing the file path to the user's profile picture. If the path cannot be found or an error occurs, it returns null.
    /// </returns>
    public static string? GetUserPicturePath()
    {
        try
        {
            var sid = WindowsIdentity.GetCurrent().User?.Value;
            if (sid == null) return null;

            string regPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\{sid}";

            using var key = Registry.LocalMachine.OpenSubKey(regPath);
            if (key != null)
            {
                // Prefer the largest image available (Image1080 > Image192 > etc.)
                var imagePath = key.GetValue("Image1080") as string
                                ?? key.GetValue("Image192") as string
                                ?? key.GetValue("Image64") as string;

                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    return imagePath;
                }
            }
        }
        catch
        {

        }

        return null;
    }

    /// <summary>
    /// Retrieves the current user's display name (full name) using the NetUserGetInfo API.
    /// </summary>
    /// <returns>
    /// A string representing the user's display name. If the API call fails or the full name is not set, it falls back to the username.
    /// </returns>
    public static unsafe string GetDisplayName()
    {
        try
        {
            var userName = Environment.UserName;
            byte* bufPtr;

            // Query full user info (level 2) via NetUserGetInfo
            var result = Windows.Win32.PInvoke.NetUserGetInfo(
                servername: null,
                username: new PCWSTR(userName.ToPointer()),
                level: 2,
                bufptr: &bufPtr);

            if (result == 0 && bufPtr != null)
            {
                // Cast unmanaged buffer to blittable struct
                var info = *(USER_INFO_2*)(nint)bufPtr;

                // Convert PCWSTR -> managed string safely
                string fullName = info.usri2_full_name.ToString() ?? string.Empty;

                // Free the unmanaged buffer
                _ = Windows.Win32.PInvoke.NetApiBufferFree(bufPtr);

                if (!string.IsNullOrEmpty(fullName))
                {
                    return fullName;
                }
            }
        }
        catch
        {

        }

        // fallback: username if NetUserGetInfo fails
        return Environment.UserName;
    }
}
