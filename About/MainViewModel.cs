using System.Linq;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.About;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle
    {
        get; set;
    }

    [ObservableProperty]
    public partial string WindowsVersionName
    {
        get; set;
    }

    public MainViewModel()
    {
        GetLegalInfo();
    }

    public string GetLegalInfo()
    {
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");

        foreach (var os in searcher.Get().Cast<ManagementObject>())
        {
            var caption = os["Caption"];
            var version = os["Version"];
            var buildNumber = os["BuildNumber"];

            WindowsVersionName = caption.ToString().Contains("10") ? "Windows 10" : "Windows 11";

            WindowsVersionTitle = caption.ToString().Replace("Microsoft ", "");

            return $"The {caption.ToString().Replace("Microsoft ", "")} operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions.";
        }

        return "WMI query returned no results";
    }
}