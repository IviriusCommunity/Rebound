using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace Rebound.ControlPanel.ViewModels;

internal partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = GetProductName();

    [ObservableProperty]
    public partial string ComputerName { get; set; } = Environment.MachineName;

    public static string GetCpuName()
    {
        return (Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") ?? "").ToString() ?? "Unknown";
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

    public static string GetTotalRamWmi()
    {
        var lpBuffer = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        PInvoke.GlobalMemoryStatusEx(ref lpBuffer);

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

    public static string GetCurrentUser()
    {
        return Environment.UserName;
    }
}