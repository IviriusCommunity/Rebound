using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

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
    public partial string LegalInfo { get; set; } = GetInformation();

    [ObservableProperty]
    public partial string CPUName { get; set; } = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString() ?? "Unknown";

    [ObservableProperty]
    public partial string CPUSpeed { get; set; } = (Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "~MHz", 0) ?? "").ToString() + " MHz" ?? "Unknown";

    [ObservableProperty]
    public partial string GPUName { get; set; } = GetGPUName();
    
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
        return gpuName ?? "Unknown";
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
        return "Unknown license holders";
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

            return $"Version {versionName} (OS Build {buildNumber}.{buildLab})";
        }
        return "Unknown version";
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
        return "Unknown version";
    }

    public static string GetInformation()
        => $"The {GetProductName()} operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions.";
}