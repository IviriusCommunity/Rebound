using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Cleanup.Helpers;
using Rebound.Cleanup.Items;
using Rebound.Helpers;
using Rebound.Helpers.AppEnvironment;
using static Rebound.Cleanup.DiskWindow;

namespace Rebound.Cleanup.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    public ObservableCollection<DriveComboBoxItem> ComboBoxItems { get; } = DriveHelper.GetDriveItems();

    public ObservableCollection<Items.CleanItem> CleanItems { get; set; } = [];

    public bool IsRunningAsAdmin { get; } = Application.Current.IsRunningAsAdmin();

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

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

        AddItem("Recycle Bin", $@"{selectedItem.DrivePath}$Recycle.Bin", "ms-appx:///Assets/imageres_54.ico",
            "The Recycle Bin stores files and folders that you've deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.", true);

        if (isSystemDrive)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var systemRoot = EnvironmentHelper.GetWindowsInstallationDrivePath();

            AddItem("Temporary Internet Files", $@"{localAppData}\Microsoft\Windows\INetCache", "ms-appx:///Assets/imageres_59.ico",
                "Cached copies of web pages, images, and other online media. Deleting these can free space, but may slow down initial page loads.");

            AddItem("Downloaded Program Files", $@"{systemRoot}\Windows\Downloaded Program Files", "ms-appx:///Assets/imageres_3.ico",
                "ActiveX controls and Java applets from websites. Can be safely deleted.", true);

            AddItem("Temporary Files", $@"{localAppData}\Temp", "ms-appx:///Assets/imageres_2.ico",
                "Temporary files created by OS and apps inside AppData.");

            AddItem("Thumbnails", $@"{localAppData}\Microsoft\Windows\Explorer", "ms-appx:///Assets/imageres_2.ico",
                "Thumbnail cache for faster file previewing.", false, ItemType.ThumbnailCache);

            AddItem("System Created Windows Error Reporting", $@"{systemRoot}\ProgramData\Microsoft\Windows\WER", "ms-appx:///Assets/EventViewer.png",
                "Crash reports for diagnosing system errors.");

            AddItem("Downloads Folder (Current User)", KnownFolders.GetPath(KnownFolder.Downloads), "ms-appx:///Assets/imageres_184.ico",
                "Default download folder for the current user.", true);

            if (IsRunningAsAdmin)
            {
                AddItem("System Cache Files", $@"{systemRoot}\Windows\Prefetch", "ms-appx:///Assets/imageres_2.ico",
                    "Prefetch and font cache files used for system speed-up.");

                AddItem("Windows Update Cache Files", $@"{systemRoot}\Windows\SoftwareDistribution", "ms-appx:///Assets/imageres_2.ico",
                    "Old files from previous Windows Updates.", true);

                AddItem("Previous Windows Installations", $@"{systemRoot}\Windows.old", "ms-appx:///Assets/imageres_2.ico",
                    "Files from a previous Windows version. Useful for rollback.");

                AddItem("System Error Memory Dump Files", $@"{systemRoot}\Windows\MEMORY.DMP", "ms-appx:///Assets/EventViewer.png",
                    "Full memory dumps generated on crash. Can be large.");

                AddItem("System Error Minidump Files", $@"{systemRoot}\Windows\Minidump", "ms-appx:///Assets/EventViewer.png",
                    "Smaller crash dumps for diagnosis.");

                AddItem("Temporary Windows Installation Files", $@"{systemRoot}\Windows\Temp", "ms-appx:///Assets/imageres_2.ico",
                    "Temp files from installing/updating Windows.");
            }
        }
        IsLoading = false;
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