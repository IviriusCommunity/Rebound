using System.Linq;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Helpers.Environment;
using Windows.ApplicationModel.DataTransfer;

namespace Rebound.About;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = GetWMIValue("Caption").Replace("Microsoft ", "");

    [ObservableProperty]
    public partial string WindowsVersionName { get; set; } = GetWMIValue("Caption").Contains("10") ? "Windows 10" : "Windows 11";

    [ObservableProperty]
    public partial string DetailedWindowsVersion { get; set; } = GetDetailedWindowsVersion();

    [ObservableProperty]
    public partial string CurrentUserName { get; set; } = GetCurrentUserName();

    public const string WMI_WIN32OPERATINGSYSTEM = "SELECT * FROM Win32_OperatingSystem";

    [RelayCommand]
    public void CopyDetails()
    {
        var content = 

$@"{WindowsVersionTitle}
{DetailedWindowsVersion}
Licensed to: {CurrentUserName.Replace("\n", ", ")}

Rebound 11
{ReboundVersion.REBOUND_VERSION}";

        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }

    private static string GetCurrentUserName()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve current username
            var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
            var owner2 = key.GetValue("RegisteredOrganization", "Unknown") as string;

            return owner + (string.IsNullOrEmpty(owner2) ? string.Empty : ("\n" + owner2));
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

    public string GetInformation()
        => $"The {
            // Simplified name for Windows without the Microsoft branding
            GetWMIValue("Caption").Replace("Microsoft ", "")
            } operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions.";

    private static string GetWMIValue(string value) => 
        // Query WMI
        new ManagementObjectSearcher(WMI_WIN32OPERATINGSYSTEM)
        
        // Obtain collection
        .Get()
        
        // Cast to ManagementObject
        .Cast<ManagementObject>()
        
        // Get the first object available
        .First()
        
        // Obtain the required value
        [value].ToString();
}