using System;
using System.Linq;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.ControlPanel.ViewModels;

internal partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = GetWMIValue("Caption")?.Replace("Microsoft ", "") ?? "Windows 11";

    public const string WMI_WIN32OPERATINGSYSTEM = "SELECT * FROM Win32_OperatingSystem";

    private static string? GetWMIValue(string value)
    {
        // Query WMI
        using var searcher = new ManagementObjectSearcher(WMI_WIN32OPERATINGSYSTEM);

        // Obtain collection
        var collection = searcher.Get();

        // Cast to ManagementObject
        var managementObject = collection.Cast<ManagementObject?>().FirstOrDefault();

        // Obtain the required value
        return managementObject?[value]?.ToString();
    }

    public static string GetCpuName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
        foreach (var item in searcher.Get())
        {
            return item["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
        }
        return "Unknown CPU";
    }

    public static string GetTotalRamWmi()
    {
        using var searcher = new ManagementObjectSearcher("Select Capacity from Win32_PhysicalMemory");
        ulong totalBytes = 0;
        foreach (var item in searcher.Get())
        {
            totalBytes += (ulong)item["Capacity"];
        }
        var totalGB = Math.Round(totalBytes / 1024.0 / 1024 / 1024, 2);
        return $"{totalGB} GB";
    }
    public static string GetCurrentUser()
    {
        return Environment.UserName;
    }
}