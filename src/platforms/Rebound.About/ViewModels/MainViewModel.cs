using System.Linq;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace Rebound.About.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = GetWMIValue("Caption")?.Replace("Microsoft ", "") ?? "Windows 11";

    [ObservableProperty]
    public partial string WindowsVersionName { get; set; } = GetWMIValue("Caption")?.Contains("10") ?? false ? "Windows 10" : "Windows 11";

    [ObservableProperty]
    public partial string DetailedWindowsVersion { get; set; } = GetDetailedWindowsVersion();

    [ObservableProperty]
    public partial string LicenseOwners { get; set; } = GetCurrentUserName();

    [ObservableProperty]
    public partial string LegalInfo { get; set; } = GetInformation();

    public const string WMI_WIN32OPERATINGSYSTEM = "SELECT * FROM Win32_OperatingSystem";

    private static string GetCurrentUserName()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve current username
            var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
            var owner2 = key.GetValue("RegisteredOrganization", "Unknown") as string;

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

    public static string GetInformation()
        => $"The {
            // Simplified name for Windows without the Microsoft branding
            GetWMIValue("Caption")?.Replace("Microsoft ", "", System.StringComparison.CurrentCultureIgnoreCase)
            } operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions.";

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
}