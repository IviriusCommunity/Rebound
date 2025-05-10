using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Cleanup.Helpers;
using Rebound.Cleanup.Items;
using Rebound.Helpers;
using Rebound.Helpers.AppEnvironment;
using Windows.System;

namespace Rebound.Cleanup.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    public ObservableCollection<DriveComboBoxItem> ComboBoxItems { get; } = DriveHelper.GetDriveItems();

    public ObservableCollection<CleanItem> CleanItems { get; set; } = [];

    public bool IsRunningAsAdmin { get; } = AppHelper.IsRunningAsAdmin();

    [ObservableProperty]
    public partial int FilesCount { get; set; }

    [ObservableProperty]
    public partial string FilesSize { get; set; } = "0 B";

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool? IsEverythingSelected { get; set; }

    [ObservableProperty]
    public partial bool CanItemsBeClicked { get; set; } = true;

    [ObservableProperty]
    public partial int SelectedDriveIndex { get; set; }

    partial void OnIsEverythingSelectedChanged(bool? oldValue, bool? newValue)
    {
        switch (newValue)
        {
            case true:
                foreach (var item in CleanItems)
                {
                    item.IsChecked = true;
                }
                break;
            case false:
                foreach (var item in CleanItems)
                {
                    item.IsChecked = false;
                }
                break;
        }
    }

    partial void OnSelectedDriveIndexChanged(int oldValue, int newValue)
    {
        SettingsHelper.SetValue("SelectedDriveIndex", "cleanmgr", newValue);
    }

    public MainViewModel()
    {
        SelectedDriveIndex = SettingsHelper.GetValue("SelectedDriveIndex", "cleanmgr", 0);
    }

    [RelayCommand]
    public static void DeleteOldRestorePointsAndShadowCopies()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "vssadmin",
                Arguments = $"delete shadows /for={EnvironmentHelper.GetWindowsInstallationDrivePath()[..1]}: /oldest",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "shadowcopy delete",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {

        }
    }

    [RelayCommand]
    public static void LaunchDefrag()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "dfrgui.exe",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public async Task RefreshAsync(DriveComboBoxItem selectedItem)
    {
        IsLoading = true;

        await Task.Delay(25).ConfigureAwait(true);

        CleanItems.Clear();

        var isSystemDrive = selectedItem.DrivePath == "C:\\";

        void AddItem(string name, string path, string image, string description, string id, bool isChecked = false, ItemType itemType = ItemType.Normal)
        {
            CleanItems.Add(new CleanItem(name, image, description, path, id, isChecked, itemType));
        }

        if (IsRunningAsAdmin)
        {
            AddItem("Recycle Bin", $@"{selectedItem.DrivePath}$Recycle.Bin", "ms-appx:///Assets/RecycleBin.ico",
                "The Recycle Bin stores files and folders that you've deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.", "TRASH", true, ItemType.RecycleBin);
        }

        if (isSystemDrive)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();

            AddItem("Temporary Files (Current User)", $@"{localAppData}\Temp", "ms-appx:///Assets/User.ico",
                "Stores temporary data created by applications and the system during normal operation.", "APPDATA_TEMP");

            AddItem("Recent Files (Current User)", $@"{appData}\Microsoft\Windows\Recent", "ms-appx:///Assets/User.ico",
                "Tracks recently opened files and documents for quick access in File Explorer and applications.", "APPDATA_MS_WIN_RECENT");

            AddItem("Temporary Internet Files", $@"{localAppData}\Microsoft\Windows\INetCache", "ms-appx:///Assets/Internet.ico",
                "Contains cached web content used by Internet Explorer and other legacy components.", "LOCALAPPDATA_MS_WIN_INETCACHE");

            AddItem("Downloaded Program Files", $@"{systemRoot}Windows\Downloaded Program Files", "ms-appx:///Assets/Program.ico",
                "Stores legacy ActiveX controls and Java applets downloaded from the internet.", "DOWNLOADED_PROGRAM_FILES", true);

            AddItem("Thumbnails", $@"{localAppData}\Microsoft\Windows\Explorer", "ms-appx:///Assets/Thumbnail.ico",
                "Contains cached thumbnail previews for images, videos, and documents.", "MS_WIN_EXPLORER_THUMBNAILS", false, ItemType.ThumbnailCache);

            AddItem("System Created Windows Error Reporting", $@"{systemRoot}ProgramData\Microsoft\Windows\WER", "ms-appx:///Assets/EventViewer.ico",
                "Stores diagnostic reports generated when applications or the system encounter errors.", "WER");

            AddItem("Downloads Folder (Current User)", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), "ms-appx:///Assets/Downloads.ico",
                "Default location for files downloaded from the internet or transferred from other devices.", "DOWNLOADS", true);

            AddItem("DirectX Shader Cache", $@"{localAppData}\D3DSCache", "ms-appx:///Assets/Video.ico",
                "Holds precompiled shader files used to enhance graphics rendering performance.", "D3DS", false);

            if (IsRunningAsAdmin)
            {
                AddItem("Prefetch", $@"{systemRoot}Windows\Prefetch", "ms-appx:///Assets/File.ico",
                    "Stores execution history to speed up application and system boot times.", "PREFETCH");

                AddItem("Delivery Optimization Cache", $@"{systemRoot}Windows\SoftwareDistribution", "ms-appx:///Assets/Script.ico",
                    "Holds downloaded update files used for peer-to-peer delivery across devices.", "SOFTWARE_DISTRIBUTION", true);

                AddItem("Windows Update Logs", $@"{systemRoot}Windows\Logs\WindowsUpdate", "ms-appx:///Assets/EventViewer.ico",
                    "Contains log files generated during the Windows Update process.", "WIN_UPDATE_LOGS", true);

                AddItem("Previous Windows Installations", $@"{systemRoot}Windows.old", "ms-appx:///Assets/History.ico",
                    "Stores files from a previous version of Windows after an upgrade.", "WINDOWS_OLD");

                AddItem("System Error Memory Dump Files", $@"{systemRoot}Windows\MEMORY.DMP", "ms-appx:///Assets/DLL.ico",
                    "Captures the full contents of system memory during a crash for diagnostic analysis.", "MEMORY_DUMP");

                AddItem("System Error Minidump Files", $@"{systemRoot}Windows\Minidump", "ms-appx:///Assets/DLL.ico",
                    "Contains small memory dump files created during system crashes.", "MINIDUMP");

                AddItem("Temporary Windows Installation Files", $@"{systemRoot}Windows\Temp", "ms-appx:///Assets/Desktop.ico",
                    "Used for temporary storage during Windows installation, setup, and updates.", "WINDOWS_TEMP");

                // Chrome
                AddChromiumBrowserProfiles("Chrome", Path.Combine(localAppData, @"Google\Chrome"), "ms-appx:///Assets/Chrome.png", "CHROME");

                // Edge
                AddChromiumBrowserProfiles("Edge", Path.Combine(localAppData, @"Microsoft\Edge"), "ms-appx:///Assets/Edge.png", "EDGE");

                var firefoxProfilesPath = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(firefoxProfilesPath))
                {
                    foreach (var profileDir in Directory.GetDirectories(firefoxProfilesPath))
                    {
                        var profileName = new DirectoryInfo(profileDir).Name;

                        AddItem($"Firefox Cache ({profileName})", Path.Combine(profileDir, "cache2"), "ms-appx:///Assets/Firefox.png",
                            "Cache files used by Mozilla Firefox to store web data for faster browsing.", $"FIREFOX_CACHE_{profileName}", false);

                        AddItem($"Firefox Cookies ({profileName})", profileDir, "ms-appx:///Assets/Firefox.png",
                            "Cookies stored by Mozilla Firefox for websites you visit.", $"FIREFOX_COOKIES_{profileName}", false, ItemType.FirefoxCookies);

                        AddItem($"Firefox History ({profileName})", profileDir, "ms-appx:///Assets/Firefox.png",
                            "Browser history stored by Mozilla Firefox.", $"FIREFOX_HISTORY_{profileName}", false, ItemType.FirefoxHistory);
                    }
                }
            }
        }
        IsLoading = false;

        LoadProperties();
        void AddChromiumBrowserProfiles(string browserName, string basePath, string iconPath, string prefix)
        {
            var browserProfilesPath = Path.Combine(basePath, "User Data");

            if (!Directory.Exists(browserProfilesPath))
                return;

            foreach (var profileDir in Directory.GetDirectories(browserProfilesPath))
            {
                var profileName = new DirectoryInfo(profileDir).Name;

                // Only include "Default" or folders starting with "Profile "
                if (!profileName.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                    !profileName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    continue;

                AddItem($"{browserName} Cache ({profileName})", Path.Combine(profileDir, "Cache"), iconPath,
                    $"Cache files used by {browserName} to store web data for faster browsing.",
                    $"{prefix}_CACHE_{profileName}", false);

                AddItem($"{browserName} Cookies ({profileName})", profileDir, iconPath,
                    $"Cookies stored by {browserName} for websites you visit.",
                    $"{prefix}_COOKIES_{profileName}", false, ItemType.ChromiumCookies);

                AddItem($"{browserName} History ({profileName})", profileDir, iconPath,
                    $"Browsing history stored by {browserName}.",
                    $"{prefix}_HISTORY_{profileName}", false, ItemType.ChromiumHistory);
            }
        }
    }

    private void LoadProperties()
    {
        long filesSize = 0;
        FilesCount = 0;

        foreach (var item in CleanItems)
        {
            filesSize += item.Size;
            FilesCount += item.FilePaths.Count; // Directly use the count of FilePaths instead of iterating over it
        }

        FilesSize = CleanItem.FormatSize(filesSize);

        var selectedItems = 0;
        foreach (var item in CleanItems)
        {
            if (item.IsChecked)
            {
                selectedItems++;
            }
        }
        var totalItems = CleanItems.Count; // Store the count in a variable
        IsEverythingSelected = selectedItems switch
        {
            0 => false,
            // Use a pattern matching case
            var count when count == totalItems => true,
            _ => null,
        };
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        CanItemsBeClicked = false;

        await Task.Delay(25).ConfigureAwait(true);

        var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();

        foreach (var item in CleanItems)
        {
            try
            {
                // Check if the path matches and if the item is checked
                if (item.IsChecked)
                {
                    if (item.ItemPath.Equals($@"{systemRoot}Windows\SoftwareDistribution", StringComparison.OrdinalIgnoreCase))
                    {
                        StopWindowsUpdateService();
                        item.Delete();
                        StartWindowsUpdateService();
                    }
                    else
                    {
                        item.Delete();
                    }
                }
            }
            catch
            {

            }
        }

        LoadProperties();

        CanItemsBeClicked = true;
    }

    [RelayCommand]
    public async Task ViewFilesAsync(int index)
    {
        if (index != -1)
        {
            if (CleanItems[index].ItemPath.Contains("$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
            {
                await Launcher.LaunchFolderPathAsync("shell:RecycleBinFolder");
            }
            else
            {
                await Launcher.LaunchFolderPathAsync(CleanItems[index].ItemPath);
            }
        }
    }

    private static void StopWindowsUpdateService()
    {
        try
        {
            using var service = new ServiceController("wuauserv");
            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }
        catch
        {

        }
    }

    private static void StartWindowsUpdateService()
    {
        try
        {
            using var service = new ServiceController("wuauserv");
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                service.Start();
            }
        }
        catch
        {

        }
    }

    [RelayCommand]
    public static void RelaunchAsAdmin()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            UseShellExecute = true,
            Verb = "runas"
        });
        Process.GetCurrentProcess().Kill();
    }
}