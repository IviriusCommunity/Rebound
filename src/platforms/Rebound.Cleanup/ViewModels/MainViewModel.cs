using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Cleanup.Helpers;
using Rebound.Cleanup.Items;
using Rebound.Helpers;
using Rebound.Helpers.AppEnvironment;
using Windows.System;
using static Rebound.Cleanup.DiskWindow;

namespace Rebound.Cleanup.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    public ObservableCollection<DriveComboBoxItem> ComboBoxItems { get; } = DriveHelper.GetDriveItems();

    public ObservableCollection<Items.CleanItem> CleanItems { get; set; } = [];

    public bool IsRunningAsAdmin { get; } = AppHelper.IsRunningAsAdmin();

    [ObservableProperty]
    public partial int FilesCount { get; set; }

    [ObservableProperty]
    public partial string FilesSize { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool CanItemsBeClicked { get; set; } = true;

    [RelayCommand]
    public void Refresh(DriveComboBoxItem selectedItem)
    {
        IsLoading = true;
        CleanItems.Clear();

        var isSystemDrive = selectedItem.DrivePath == "C:\\";

        void AddItem(string name, string path, string image, string description, bool isChecked = false, ItemType itemType = ItemType.Normal)
        {
            CleanItems.Add(new Items.CleanItem(name, image, description, path, isChecked, itemType));
        }

        if (IsRunningAsAdmin)
        {
            AddItem("Recycle Bin", $@"{selectedItem.DrivePath}$Recycle.Bin", "ms-appx:///Assets/RecycleBin.ico",
                "The Recycle Bin stores files and folders that you've deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.", true, ItemType.RecycleBin);
        }

        if (isSystemDrive)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();

            AddItem("Temporary Files (Current User)", $@"{localAppData}\Temp", "ms-appx:///Assets/User.ico",
                "Stores temporary data created by applications and the system during normal operation.");

            AddItem("Recent Files (Current User)", $@"{appData}\Microsoft\Windows\Recent", "ms-appx:///Assets/User.ico",
                "Tracks recently opened files and documents for quick access in File Explorer and applications.");

            AddItem("Temporary Internet Files", $@"{localAppData}\Microsoft\Windows\INetCache", "ms-appx:///Assets/Internet.ico",
                "Contains cached web content used by Internet Explorer and other legacy components.");

            AddItem("Downloaded Program Files", $@"{systemRoot}Windows\Downloaded Program Files", "ms-appx:///Assets/Program.ico",
                "Stores legacy ActiveX controls and Java applets downloaded from the internet.", true);

            AddItem("Thumbnails", $@"{localAppData}\Microsoft\Windows\Explorer", "ms-appx:///Assets/Thumbnail.ico",
                "Contains cached thumbnail previews for images, videos, and documents.", false, ItemType.ThumbnailCache);

            AddItem("System Created Windows Error Reporting", $@"{systemRoot}ProgramData\Microsoft\Windows\WER", "ms-appx:///Assets/EventViewer.ico",
                "Stores diagnostic reports generated when applications or the system encounter errors.");

            AddItem("Downloads Folder (Current User)", KnownFolders.GetPath(KnownFolder.Downloads), "ms-appx:///Assets/Downloads.ico",
                "Default location for files downloaded from the internet or transferred from other devices.", true);

            AddItem("DirectX Shader Cache", $@"{localAppData}\D3DSCache", "ms-appx:///Assets/Video.ico",
                "Holds precompiled shader files used to enhance graphics rendering performance.", false);

            if (IsRunningAsAdmin)
            {
                AddItem("Prefetch", $@"{systemRoot}Windows\Prefetch", "ms-appx:///Assets/File.ico",
                    "Stores execution history to speed up application and system boot times.");

                AddItem("Delivery Optimization Cache", $@"{systemRoot}Windows\SoftwareDistribution", "ms-appx:///Assets/Script.ico",
                    "Holds downloaded update files used for peer-to-peer delivery across devices.", true);

                AddItem("Windows Update Logs", $@"{systemRoot}Windows\Logs\WindowsUpdate", "ms-appx:///Assets/EventViewer.ico",
                    "Contains log files generated during the Windows Update process.", true);

                AddItem("Previous Windows Installations", $@"{systemRoot}Windows.old", "ms-appx:///Assets/History.ico",
                    "Stores files from a previous version of Windows after an upgrade.");

                AddItem("System Error Memory Dump Files", $@"{systemRoot}Windows\MEMORY.DMP", "ms-appx:///Assets/DLL.ico",
                    "Captures the full contents of system memory during a crash for diagnostic analysis.");

                AddItem("System Error Minidump Files", $@"{systemRoot}Windows\Minidump", "ms-appx:///Assets/DLL.ico",
                    "Contains small memory dump files created during system crashes.");

                AddItem("Temporary Windows Installation Files", $@"{systemRoot}Windows\Temp", "ms-appx:///Assets/Desktop.ico",
                    "Used for temporary storage during Windows installation, setup, and updates.");
            }
        }
        IsLoading = false;

        long filesSize = 0;

        foreach (var item in CleanItems)
        {
            filesSize += item.Size;
            FilesCount += item.FilePaths.Count; // Directly use the count of FilePaths instead of iterating over it
        }

        FilesSize = Items.CleanItem.FormatSize(filesSize);
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        CanItemsBeClicked = false;

        await Task.Delay(250).ConfigureAwait(true);

        var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();

        foreach (var item in CleanItems)
        {
            // Check if the path matches and if the item is checked
            if (item.ItemPath.Equals($@"{systemRoot}Windows\SoftwareDistribution", StringComparison.OrdinalIgnoreCase) && item.IsChecked)
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

        long filesSize = 0;

        foreach (var item in CleanItems)
        {
            filesSize += item.Size;
            FilesCount += item.FilePaths.Count; // Directly use the count of FilePaths instead of iterating over it
        }

        FilesSize = Items.CleanItem.FormatSize(filesSize);

        CanItemsBeClicked = true;
    }

    [RelayCommand]
    public async Task ViewFilesAsync(int index)
    {
        var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();
        if (CleanItems[index].ItemPath.Equals($@"{systemRoot}$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
        {
            await Launcher.LaunchFolderPathAsync("shell:RecycleBinFolder");
        }
        else
        {
            await Launcher.LaunchFolderPathAsync(CleanItems[index].ItemPath);
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