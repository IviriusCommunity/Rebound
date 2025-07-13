using System;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Rebound.Helpers;
using WinUI3Localizer;

namespace Rebound.Shell.ShutdownDialog
{
    public partial class ShutdownViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string WindowsVersionTitle { get; set; } = GetProductName();

        [ObservableProperty]
        public partial string WindowsVersionName { get; set; } = GetProductName().Contains("10") ? "Windows 10" : "Windows 11";

        [ObservableProperty]
        public partial bool ShowBlurAndGlow { get; set; }

        public ShutdownViewModel()
        {
            ShowBlurAndGlow = SettingsHelper.GetValue("ShowBlurAndGlow", "rebound", true);
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
    }
}
