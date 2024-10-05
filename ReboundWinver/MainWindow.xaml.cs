using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;

namespace ReboundWinver
{
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
            this.IsMaximizable = false;
            this.IsMinimizable = false;
            this.MinWidth = 650;
            this.MoveAndResize(25, 25, 650, 690);
            this.Title = "About Windows";
            this.IsResizable = false;
            this.SystemBackdrop = new MicaBackdrop();
            this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\ReboundHub.ico");
            User.Text = GetCurrentUserName();
            Version.Text = GetDetailedWindowsVersion();
            LegalStuff.Text = GetLegalInfo();
            Load();
        }

        public async void Load()
        {
            await Task.Delay(100);

            this.SetWindowSize(WinverPanel.ActualWidth + 60, 690);
        }

        public static string GetDetailedWindowsVersion()
        {
            try
            {
                // Open the registry key
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        // Retrieve build number and revision
                        var versionName = key.GetValue("DisplayVersion", "Unknown") as string;
                        var buildNumber = key.GetValue("CurrentBuildNumber", "Unknown") as string;
                        var buildLab = key.GetValue("UBR", "Unknown");

                        return $"Version {versionName} (OS Build {buildNumber}.{buildLab})";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving OS version details: {ex.Message}";
            }

            return "Registry key not found";
        }

        public string GetLegalInfo()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");

                foreach (ManagementObject os in searcher.Get().Cast<ManagementObject>())
                {
                    var caption = os["Caption"];
                    var version = os["Version"];
                    var buildNumber = os["BuildNumber"];

                    if (caption.ToString().Contains("10")) windowsVer = "Windows 10";
                    else windowsVer = "Windows 11";

                    WindowsVer.Text = caption.ToString().Replace("Microsoft ", "");

                    return $"The {caption.ToString().Replace("Microsoft ", "")} operating system and its user interface are protected by trademark and other pending or existing intellectual property rights in the United States and other countries/regions.";
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving OS edition details: {ex.Message}";
            }

            return "WMI query returned no results";
        }

        public static string GetCurrentUserName()
        {
            try
            {
                // Open the registry key
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        // Retrieve build number and revision
                        var owner = key.GetValue("RegisteredOwner", "Unknown") as string;

                        return owner;
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving OS version details: {ex.Message}";
            }

            return "Registry key not found";
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var info = new ProcessStartInfo()
            {
                FileName = "powershell",
                Arguments = "winver",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(info);

            await proc.WaitForExitAsync();

            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        string windowsVer = "Windows";

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string content = $@"==========================
---Microsoft {windowsVer}---
==========================

{GetDetailedWindowsVersion()}
© Microsoft Corporation. All rights reserved.

{GetLegalInfo()}

This product is licensed under the [Microsoft Software License Terms] (https://support.microsoft.com/en-us/windows/microsoft-software-license-terms-e26eedad-97a2-5250-2670-aad156b654bd) to: {GetCurrentUserName()}

==========================
--------Rebound 11--------
==========================

{ReboundVer.Text}

Rebound 11 is a Windows mod that does not interfere with the system. The current Windows installation contains additional apps to run Rebound 11.";
            var package = new DataPackage();
            package.SetText(content);
            Clipboard.SetContent(package);
        }
    }
}
