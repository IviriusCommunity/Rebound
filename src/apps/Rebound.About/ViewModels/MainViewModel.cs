using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Rebound.Core.Helpers;
using System;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.About.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = GetProductName();

    [ObservableProperty]
    public partial string WindowsVersionName { get; set; } = GetProductName().Contains("10") ? "Windows 10" : "Windows 11";

    [ObservableProperty]
    public partial string DetailedWindowsVersion { get; set; } = GetDetailedWindowsVersion();

    [ObservableProperty]
    public partial string LicenseOwners { get; set; } = GetCurrentUserName();

    [ObservableProperty]
    public partial bool IsSidebarOn { get; set; }

    [ObservableProperty]
    public partial bool IsReboundOn { get; set; }

    [ObservableProperty]
    public partial string LegalInfo { get; set; } = GetInformation();

    [ObservableProperty]
    public partial string CPUName { get; set; } = NormalizeTrademarkSymbols((Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString()) ?? "Unknown";

    [ObservableProperty]
    public partial string GPUName { get; set; } = GetGPUName();

    [ObservableProperty]
    public partial string CurrentUser { get; set; } = GetDisplayName();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USER_INFO_2
    {
        public string usri2_name;
        public string usri2_password;
        public uint usri2_password_age;
        public uint usri2_priv;
        public string usri2_home_dir;
        public string usri2_comment;
        public uint usri2_flags;
        public string usri2_script_path;
        public uint usri2_auth_flags;
        public string usri2_full_name;
        public string usri2_usr_comment;
        public string usri2_parms;
        public string usri2_workstations;
        public uint usri2_last_logon;
        public uint usri2_last_logoff;
        public uint usri2_acct_expires;
        public uint usri2_max_storage;
        public uint usri2_units_per_week;
        public IntPtr usri2_logon_hours;
        public uint usri2_bad_pw_count;
        public uint usri2_num_logons;
        public string usri2_logon_server;
        public uint usri2_country_code;
        public uint usri2_code_page;
    }

    private static unsafe string GetDisplayName()
    {

        try
        {
            var userName = Environment.UserName;

            byte* bufPtr;

            // Use the Win32 NetUserGetInfo API to get the full name
            USER_INFO_2 info;
            var result = Windows.Win32.PInvoke.NetUserGetInfo(null, userName.ToPCWSTR(), 2, &bufPtr);
            if (result == 0)
            {
                info = Marshal.PtrToStructure<USER_INFO_2>((nint)bufPtr);
                string fullName = info.usri2_full_name;
                Windows.Win32.PInvoke.NetApiBufferFree(bufPtr);
                if (!string.IsNullOrEmpty(fullName))
                    return string.Format(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("HelloUser"), fullName);
            }
        }
        catch
        {
            // fallback
        }

        return Environment.UserName;
    }

    [ObservableProperty]
    public partial string RAM { get; set; } = GetTotalRam();

    [ObservableProperty]
    public partial string UsableRAM { get; set; } = GetUsableRAM();

    [ObservableProperty]
    public partial bool ShowBlurAndGlow { get; set; }

    public MainViewModel()
    {
        IsSidebarOn = SettingsHelper.GetValue("IsSidebarOn", "winver", false);
        IsReboundOn = SettingsHelper.GetValue("IsReboundOn", "winver", true);
        ShowBlurAndGlow = SettingsHelper.GetValue("ShowBlurAndGlow", "rebound", true);
    }

    partial void OnIsSidebarOnChanged(bool value)
    {
        SettingsHelper.SetValue("IsSidebarOn", "winver", value);
    }

    partial void OnIsReboundOnChanged(bool value)
    {
        SettingsHelper.SetValue("IsReboundOn", "winver", value);
    }

    public static unsafe string GetTotalRam()
    {
        try
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
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
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
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
                using var subKey = videoKey.OpenSubKey($@"{subKeyName}\0000");
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

    private static string NormalizeTrademarkSymbols(string input) => input
            .Replace("(R)", "®")
            .Replace("(r)", "®")
            .Replace("(TM)", "™")
            .Replace("(tm)", "™")
            .Replace("(C)", "©")
            .Replace("(c)", "©");

    private static string GetCurrentUserName()
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

    private static string GetDetailedWindowsVersion()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve build number and revision
            var versionName = key.GetValue("DisplayVersion", "Unknown") as string;
            var buildNumber = key.GetValue("CurrentBuildNumber", "Unknown") as string;
            var buildLab = key.GetValue("UBR", "Unknown");
            return string.Format(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("VersionOSBuild"), versionName, buildNumber, buildLab);
        }
        return "Unknown";
    }

    private static string GetProductName()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve build number and revision
            var productName = key.GetValue("ProductName", "Unknown") as string;
            var buildNumber = key.GetValue("CurrentBuildNumber", "Unknown") as string;
            if (int.Parse(buildNumber ?? "") >= 22000)
            {
                return productName.Replace("10", "11");
            }
            return productName;
        }
        return "Unknown";
    }

    public static string GetInformation()
        => String.Format(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString("LegalInfo"), GetProductName());
}