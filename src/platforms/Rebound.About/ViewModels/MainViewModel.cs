using System;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Rebound.Helpers;
using WinUI3Localizer;

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
    public partial string CPUName { get; set; } = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString() ?? "Unknown".GetLocalizedString();

    [ObservableProperty]
    public partial string GPUName { get; set; } = GetGPUName();

    [ObservableProperty]
    public partial string CurrentUser { get; set; } = Environment.UserName;

    [ObservableProperty]
    public partial string RAM { get; set; } = GetTotalRam();

    public MainViewModel()
    {
        IsSidebarOn = SettingsHelper.GetValue("IsSidebarOn", "winver", false);
        IsReboundOn = SettingsHelper.GetValue("IsReboundOn", "winver", true);
    }

    partial void OnIsSidebarOnChanged(bool value)
    {
        SettingsHelper.SetValue("IsSidebarOn", "winver", value);
    }

    partial void OnIsReboundOnChanged(bool value)
    {
        SettingsHelper.SetValue("IsReboundOn", "winver", value);
    }

    public static string GetTotalRam()
    {
        var lpBuffer = new Windows.Win32.System.SystemInformation.MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<Windows.Win32.System.SystemInformation.MEMORYSTATUSEX>()
        };

        Windows.Win32.PInvoke.GlobalMemoryStatusEx(ref lpBuffer);

        // Mimic Windows' display logic
        int displayedSize;
        var totalGb = lpBuffer.ullTotalPhys / 1024.0 / 1024 / 1024;

        // Common marketed RAM sizes in ascending order
        int[] commonSizes = { 1, 2, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 256 };

        displayedSize = commonSizes.FirstOrDefault(size => totalGb < size);
        if (displayedSize == 0)
        {
            // If it's larger than all predefined sizes, round to nearest multiple of 8
            displayedSize = (int)Math.Round(totalGb / 8) * 8;
        }

        return $"{displayedSize} GB";
    }

    public static string GetGPUName()
    {
        string? gpuName = null;
        using (var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video"))
        {
            foreach (var subKeyName in videoKey?.GetSubKeyNames())
            {
                using var subKey = videoKey.OpenSubKey($@"{subKeyName}\0000");
                var name = subKey?.GetValue("DriverDesc")?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    gpuName = name;
                    break;
                }
            }
        }
        return gpuName ?? "Unknown".GetLocalizedString();
    }

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
        return "UnknownLicenseHolders".GetLocalizedString();
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

            return String.Format("VersionOSBuild".GetLocalizedString(), versionName, buildNumber, buildLab);
        }
        return "Unknown".GetLocalizedString();
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
        return "Unknown".GetLocalizedString();
    }

    public static string GetInformation()
        => String.Format("LegalInfo".GetLocalizedString(), GetProductName());
}