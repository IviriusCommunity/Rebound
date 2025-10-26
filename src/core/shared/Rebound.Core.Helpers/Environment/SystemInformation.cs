// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32.Foundation;

namespace Rebound.Core.Helpers.Environment;

internal class SystemInformation
{
    public static string NormalizeTrademarkSymbols(string input) => input
            .Replace("(R)", "®", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(r)", "®", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(TM)", "™", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(tm)", "™", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(C)", "©", StringComparison.InvariantCultureIgnoreCase)
            .Replace("(c)", "©", StringComparison.InvariantCultureIgnoreCase);

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

    public static string GetOSName()
    {
        string regPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        using var key = Registry.LocalMachine.OpenSubKey(regPath);
        if (key != null)
        {
            var windowsVersionTitle = key.GetValue("ProductName")?.ToString();
            var isWindows11 = int.Parse(key.GetValue("CurrentBuildNumber")?.ToString()!, null) >= 22000;

            return isWindows11
                ? windowsVersionTitle?.Replace("10", "11", StringComparison.InvariantCultureIgnoreCase)!
                : windowsVersionTitle ?? "Unknown";
        }

        return "Unknown";
    }

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

                if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
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

    public static unsafe string GetTotalRam()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{roundedGb} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static unsafe string GetUsableRAM()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{totalGb:F2} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static string GetGPUName()
    {
        string? gpuName = null;

        using (var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video"))
        {
            foreach (var subKeyName in videoKey?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var subKey = videoKey?.OpenSubKey($@"{subKeyName}\0000");
                var name = subKey?.GetValue("DriverDesc")?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    gpuName = NormalizeTrademarkSymbols(name);
                    break;
                }
            }
        }

        return gpuName ?? "Unknown";
    }

    public static string GetLicenseOwners()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve current username
            var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
            var owner2 = key.GetValue("RegisteredOrganization", "") as string;

            return owner + (string.IsNullOrEmpty(owner2) ? string.Empty : (", " + owner2));
        }
        return "UnknownLicenseHolders";
    }

    public static string GetDisplayVersion()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("DisplayVersion", "Unknown").ToString()!;
        }
        return "Unknown";
    }

    public static string GetCurrentBuildNumber()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("CurrentBuildNumber", "Unknown").ToString()!;
        }
        return "Unknown";
    }

    public static string GetUBR()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("UBR", "Unknown").ToString()!;
        }
        return "Unknown";
    }

    public static string GetCPUName()
    {
        return NormalizeTrademarkSymbols((Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString()!) ?? "Unknown";
    }

    public static unsafe string GetDisplayName()
    {
        try
        {
            var userName = System.Environment.UserName;
            byte* bufPtr;

            // Query full user info (level 2) via NetUserGetInfo
            var result = Windows.Win32.PInvoke.NetUserGetInfo(
                servername: null,
                username: userName.ToPCWSTR(),
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
            // fallback
        }

        // fallback: username if NetUserGetInfo fails
        return System.Environment.UserName;
    }
}