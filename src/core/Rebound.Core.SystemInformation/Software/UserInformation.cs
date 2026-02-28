// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using Rebound.Core.Native;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using TerraFX.Interop.Windows;
using static Rebound.Core.Native.NetApi;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Software;

public static class UserInformation
{
    /// <summary>
    /// Checks if the current user is an admin or not.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the user is an admin. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsAdmin()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();

        // If not elevated, the current check is already accurate
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
        {
            // Could still be admin but running unelevated — check via group membership
            using WindowsIdentity? linked = GetLinkedToken(identity);
            if (linked != null)
                return new WindowsPrincipal(linked).IsInRole(WindowsBuiltInRole.Administrator);
        }

        return true;
    }

    private static unsafe WindowsIdentity? GetLinkedToken(WindowsIdentity identity)
    {
        try
        {
            nint linkedToken;
            if (!GetTokenInformation(new((void*)identity.Token), TOKEN_INFORMATION_CLASS.TokenLinkedToken,
                    &linkedToken, (uint)sizeof(nint), null))
                return null;

            return new WindowsIdentity(linkedToken);
        }
        catch { return null; }
    }

    /// <summary>
    /// Gets the date and time the current user's password was last set.
    /// </summary>
    public static unsafe string GetPasswordLastSet()
    {
        using var userName = new ManagedPtr<char>(Environment.UserName);
        USER_INFO_2* info;
        HRESULT hr = NetUserGetInfo(null, userName, 2, (byte**)&info);

        if (hr != S.S_OK) return "Never";

        long ticks = info->usri2_password_age;
        hr = NetApiBufferFree(info);

        if (FAILED(hr)) return "Something went wrong.";

        return DateTimeOffset.UtcNow
            .AddSeconds(-ticks)
            .ToLocalTime()
            .ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Gets the date and time the current user's password will expire,
    /// or "Never" if it never expires.
    /// </summary>
    public static unsafe string GetPasswordExpiry()
    {
        using var userName = new ManagedPtr<char>(Environment.UserName);
        USER_INFO_2* info;
        HRESULT hr = NetUserGetInfo(null, userName, 2, (byte**)&info);

        if (hr != S.S_OK) return "Never";

        uint passwordAge = info->usri2_password_age;
        hr = NetApiBufferFree(info);

        if (FAILED(hr)) return "Something went wrong.";

        USER_MODALS_INFO_0* modals;
        NetUserModalsGet(null, 0, (byte**)&modals);
        uint maxPasswordAge = modals->usrmod0_max_passwd_age;
        hr = NetApiBufferFree(modals);

        if (FAILED(hr)) return "Something went wrong.";
        if (maxPasswordAge == uint.MaxValue) return "Never";

        return DateTimeOffset.UtcNow
            .AddSeconds(-passwordAge)
            .AddSeconds(maxPasswordAge)
            .ToLocalTime()
            .ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Checks if the current user is logged in with a Microsoft account for the entire user profile or not.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current user is tied to a Microsoft account. Otherwise <see langword="false"/>. 
    /// </returns>
    public static bool IsMicrosoftAccount()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\IdentityStore\LogonCache");
        if (key == null) return false;

        foreach (var guid in key.GetSubKeyNames())
        {
            using var sid2name = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\IdentityStore\LogonCache\{guid}\Sid2Name");
            if (sid2name == null) continue;

            foreach (var sid in sid2name.GetSubKeyNames())
            {
                using var sidKey = sid2name.OpenSubKey(sid);
                var authority = sidKey?.GetValue("AuthenticatingAuthority") as string;
                if (authority?.Equals("MicrosoftAccount", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieves the current user's folder.
    /// </summary>
    /// <returns>
    /// A <see langword="string"/> representing the full path to the current user's folder.
    /// </returns>
    public static string GetUserFolder()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
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
            using var clsid = new ManagedPtr<Guid>(CLSID.CLSID_DesktopWallpaper);
            using var iid = new ManagedPtr<Guid>(IID.IID_IDesktopWallpaper);
            using ComPtr<IDesktopWallpaper> desktopWallpaper = default;

            var hr = CoCreateInstance(
                clsid, 
                null, 
                0x17, 
                iid, 
                (void**)desktopWallpaper.GetAddressOf());

            if (FAILED(hr)) return null;

            ushort* wallpaperPtr = null;
            hr = desktopWallpaper.Get()->GetWallpaper(null, (char**)&wallpaperPtr);

            if (FAILED(hr)) return null;

            string wallpaperPath = new string((char*)wallpaperPtr);
            CoTaskMemFree(wallpaperPtr);

            if (!string.IsNullOrEmpty(wallpaperPath))
            {
                return wallpaperPath;
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
            using var identity = WindowsIdentity.GetCurrent();
            var sid = identity.User?.Value;
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
            HRESULT hr;
            using var userName = new ManagedPtr<char>(Environment.UserName);
            byte* bufPtr;

            // Query full user info (level 2) via NetUserGetInfo
            hr = NetUserGetInfo(
                null,
                userName,
                2,
                &bufPtr);

            if (SUCCEEDED(hr) && bufPtr != null)
            {
                // Cast unmanaged buffer to blittable struct
                var info = *(USER_INFO_2*)(nint)bufPtr;

                // Convert PCWSTR -> managed string safely
                string fullName = new string(info.usri2_full_name);

                // Free the unmanaged buffer
                hr = NetApiBufferFree(bufPtr);

                if (FAILED(hr))
                    return "Something went wrong.";

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
