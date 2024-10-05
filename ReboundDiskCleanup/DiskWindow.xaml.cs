using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Shell;
using Windows.UI.WindowManagement;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundDiskCleanup
{
    public class CleanItem
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string ItemPath { get; set; }
        public string Description { get; set; }
        public string DisplaySize { get; set; }
        public long Size { get; set; }
        public bool IsChecked { get; set; }
    }

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DiskWindow : WindowEx
    {
        public long GetFolderLongSize(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (IOException ex)
            {
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }

            // Format the size into appropriate units
            return totalSize;
        }

        private void CleanupDriverStore()
        {
            try
            {
                string driverStorePath = @"C:\Windows\System32\DriverStore\FileRepository";

                // Fetch all directories in the DriverStore
                var directories = Directory.GetDirectories(driverStorePath);

                foreach (var dir in directories)
                {
                    // Check if the directory is unused or old
                    // In this example, we assume that if a directory was last accessed over 30 days ago, it's unnecessary.
                    var lastAccessTime = Directory.GetLastAccessTime(dir);
                    if (lastAccessTime < DateTime.Now.AddDays(-30))
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            Debug.WriteLine($"Deleted: {dir}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error deleting {dir}: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine("Driver Store Cleanup Completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during cleanup: {ex.Message}");
            }
        }

        public long GetFolderLongSizeDrivers(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.LastAccessTime < DateTime.Now.AddDays(-30)) totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (IOException ex)
            {
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }

            // Format the size into appropriate units
            return totalSize;
        }

        public string GetFolderSizeDrivers(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return "0 B";
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.LastAccessTime < DateTime.Now.AddDays(-30)) totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return "0 B";
            }
            catch (IOException ex)
            {
                return "0 B";
            }
            catch (Exception ex)
            {
                return "0 B";
            }

            // Format the size into appropriate units
            return FormatSize(totalSize);
        }

        public long GetFolderLongSizeDB(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return 0;
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Extension.Contains("db") && fileInfo.Name.Contains("thumbcache") == true) totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (IOException ex)
            {
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }

            // Format the size into appropriate units
            return totalSize;
        }

        public string GetFolderSize(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return "0 B";
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return "Access Denied";
            }
            catch (IOException ex)
            {
                return "IO Error";
            }
            catch (Exception ex)
            {
                return "Unknown Error";
            }

            // Format the size into appropriate units
            return FormatSize(totalSize);
        }

        public void DeleteFiles(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (IOException ex)
            {
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public void DeleteFilesDB(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Extension.Contains("db") && fileInfo.Name.Contains("thumbcache") == true) File.Delete(file);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (IOException ex)
            {
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public string GetFolderSizeDB(string folderPath)
        {
            // Check if the directory exists
            if (!Directory.Exists(folderPath))
            {
                return "0 B";
            }

            long totalSize = 0;

            try
            {
                // Enumerate files and directories
                foreach (var file in EnumerateFiles(folderPath))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        Debug.WriteLine($"NAME: {fileInfo.Name} || EXTENSION: {fileInfo.Extension}");
                        if (fileInfo.Extension.Contains("db") && fileInfo.Name.Contains("thumbcache") == true) totalSize += fileInfo.Length;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Debug.WriteLine("ERR");
                        // Skip files we cannot access
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine("ERR");
                        // Handle other IO exceptions
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return "Access Denied";
            }
            catch (IOException ex)
            {
                return "IO Error";
            }
            catch (Exception ex)
            {
                return "Unknown Error";
            }

            // Format the size into appropriate units
            return FormatSize(totalSize);
        }

        private IEnumerable<string> EnumerateFiles(string folderPath)
        {
            var directoriesToProcess = new Stack<string>(new[] { folderPath });

            while (directoriesToProcess.Count > 0)
            {
                string currentDir = directoriesToProcess.Pop();

                IEnumerable<string> files = GetFilesSafe(currentDir);
                foreach (string file in files)
                {
                    yield return file;
                }

                IEnumerable<string> subDirs = GetDirectoriesSafe(currentDir);
                foreach (string subDir in subDirs)
                {
                    directoriesToProcess.Push(subDir);
                }
            }
        }

        private IEnumerable<string> GetFilesSafe(string directory)
        {
            try
            {
                return Directory.EnumerateFiles(directory);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we cannot access
                return Array.Empty<string>();
            }
            catch (IOException ex)
            {
                // Handle IO exceptions for directory operations
                return Array.Empty<string>();
            }
        }

        private IEnumerable<string> GetDirectoriesSafe(string directory)
        {
            try
            {
                return Directory.EnumerateDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we cannot access
                return Array.Empty<string>();
            }
            catch (IOException ex)
            {
                // Handle IO exceptions for directory operations
                return Array.Empty<string>();
            }
        }

        private string FormatSize(long sizeInBytes)
        {
            if (sizeInBytes < 1024)
                return $"{sizeInBytes} B";
            else if (sizeInBytes < 1024 * 1024)
                return $"{sizeInBytes / 1024.0:F2} KB";
            else if (sizeInBytes < 1024 * 1024 * 1024)
                return $"{sizeInBytes / (1024.0 * 1024):F2} MB";
            else if (sizeInBytes < 1024L * 1024 * 1024 * 1024)
                return $"{sizeInBytes / (1024.0 * 1024 * 1024):F2} GB";
            else if (sizeInBytes < 1024L * 1024 * 1024 * 1024 * 1024)
                return $"{sizeInBytes / (1024.0 * 1024 * 1024 * 1024):F2} TB";
            else
                return $"{sizeInBytes / (1024.0 * 1024 * 1024 * 1024 * 1024):F2} PB";
        }

        public List<CleanItem> items = new List<CleanItem>();

        public string Disk = "";

        public DiskWindow(string disk)
        {
            Disk = disk;
            this.InitializeComponent();
            if (IsAdministrator() == true) SysFilesButton.Visibility = Visibility.Collapsed;
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Temporary Internet Files",
                    ItemPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\INetCache",
                    ImagePath = "ms-appx:///Assets/imageres_59.ico",
                    Size = GetFolderLongSize($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\INetCache"),
                    DisplaySize = GetFolderSize($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\INetCache"),
                    Description = "These files are cached copies of web pages, images, and other online media from websites you've visited. They help speed up loading times when you revisit those sites. Deleting these files will free up space but might slow down page loading temporarily until the cache is rebuilt.",
                    IsChecked = false,
                });
            }
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Downloaded Program Files",
                    ItemPath = @"C:\Windows\Downloaded Program Files",
                    ImagePath = "ms-appx:///Assets/imageres_3.ico",
                    Size = GetFolderLongSize(@"C:\Windows\Downloaded Program Files"),
                    DisplaySize = GetFolderSize(@"C:\Windows\Downloaded Program Files"),
                    Description = "This category includes ActiveX controls and Java applets that were automatically downloaded from the Internet when you view certain web pages. These files are temporarily stored on your computer to speed up the loading of the pages when you revisit them. They can be safely deleted if not needed.",
                    IsChecked = true,
                });
            }
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Rebound 11 temporary files",
                    ItemPath = @"C:\Rebound11\Temp",
                    ImagePath = "ms-appx:///Assets/r11imageres_101.ico",
                    Size = GetFolderLongSize(@"C:\Rebound11\Temp"),
                    DisplaySize = GetFolderSize(@"C:\Rebound11\Temp"),
                    Description = "Rebound 11 might sometimes copy packages and other files to a special temp folder in order for PowerShell to read the paths easier when installing.",
                    IsChecked = true,
                });
            }
            items.Add(new CleanItem
            {
                Name = $"Recycle Bin",
                ItemPath = $@"{disk}\$Recycle.Bin",
                ImagePath = "ms-appx:///Assets/imageres_54.ico",
                Size = GetFolderLongSize($@"{disk}\$Recycle.Bin"),
                DisplaySize = GetFolderSize($@"{disk}\$Recycle.Bin"),
                Description = "The Recycle Bin stores files and folders that you’ve deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.",
                IsChecked = true,
            });
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Temporary Files",
                    ItemPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Temp",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSize($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Temp"),
                    DisplaySize = GetFolderSize($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Temp"),
                    Description = "These are files created by the operating system and applications inside the AppData folder for temporary use. They are often created during the installation of software or while programs are running. These files can usually be safely deleted once the system or application is done with them.",
                    IsChecked = false,
                });
            }
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Thumbnails",
                    ItemPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\Explorer",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSizeDB($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\Explorer"),
                    DisplaySize = GetFolderSizeDB($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Microsoft\Windows\Explorer"),
                    Description = "Thumbnails are small images used to preview the content of files, such as pictures and videos, within folders. The system caches these images to display them quickly. Deleting thumbnail caches will free up space but will cause the system to regenerate them when needed.",
                    IsChecked = false,
                });
            }
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"System Created Windows Error Reporting",
                    ItemPath = $@"C:\ProgramData\Microsoft\Windows\WER",
                    ImagePath = "ms-appx:///Assets/EventViewer.png",
                    Size = GetFolderLongSize($@"C:\ProgramData\Microsoft\Windows\WER"),
                    DisplaySize = GetFolderSize($@"C:\ProgramData\Microsoft\Windows\WER"),
                    Description = "These are files generated when your system encounters an error. They contain data that can be used to troubleshoot and diagnose issues with your system. If the reports have been sent to Microsoft or are no longer needed, they can be deleted to free up space.",
                    IsChecked = false,
                });
            }
            if (disk == "C:")
            {
                items.Add(new CleanItem
                {
                    Name = $"Downloads Folder (Current User)",
                    ItemPath = $@"{KnownFolders.GetPath(KnownFolder.Downloads)}",
                    ImagePath = "ms-appx:///Assets/imageres_184.ico",
                    Size = GetFolderLongSize($@"{KnownFolders.GetPath(KnownFolder.Downloads)}"),
                    DisplaySize = GetFolderSize($@"{KnownFolders.GetPath(KnownFolder.Downloads)}"),
                    Description = "The current user's downloads folder. When you download a file from the web, it will usually be placed here.",
                    IsChecked = true,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"System Cache Files",
                    ItemPath = $@"C:\Windows\Prefetch\",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSize($@"C:\Windows\Prefetch\"),
                    DisplaySize = GetFolderSize($@"C:\Windows\Prefetch\"),
                    Description = "These include various files used by the system to speed up operations. Examples include prefetch files that help applications start faster and font cache files that speed up font rendering. Deleting these files can reclaim space but might temporarily slow down some operations.",
                    IsChecked = false,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"Windows Update Cache Files",
                    ItemPath = $@"C:\Windows\SoftwareDistribution",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSize($@"C:\Windows\SoftwareDistribution"),
                    DisplaySize = GetFolderSize($@"C:\Windows\SoftwareDistribution"),
                    Description = "Disk Cleanup can remove files that are no longer needed after installing Windows updates. These include old versions of files that have been updated, which can sometimes take up significant disk space. Deleting these files will make it harder to uninstall updates.",
                    IsChecked = true,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"Previous Windows Installations",
                    ItemPath = $@"C:\Windows.old",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSize($@"C:\Windows.old"),
                    DisplaySize = GetFolderSize($@"C:\Windows.old"),
                    Description = "If you’ve recently upgraded to a newer version of Windows, files from the previous installation are kept in the Windows.old folder in case you need to revert to the earlier version. Deleting these files will permanently remove the ability to roll back to the previous version.",
                    IsChecked = false,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"System Error Memory Dump Files",
                    ItemPath = $@"C:\Windows\MEMORY.DMP",
                    ImagePath = "ms-appx:///Assets/EventViewer.png",
                    Size = GetFolderLongSize($@"C:\Windows\MEMORY.DMP"),
                    DisplaySize = GetFolderSize($@"C:\Windows\MEMORY.DMP"),
                    Description = "These files are created when Windows crashes and contain a copy of the memory at the time of the crash. They can be used to diagnose the cause of the crash. Large memory dumps can take up significant space and can be safely deleted if no longer needed.",
                    IsChecked = false,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"System Error Minidump Files",
                    ItemPath = $@"C:\Windows\Minidump",
                    ImagePath = "ms-appx:///Assets/EventViewer.png",
                    Size = GetFolderLongSize($@"C:\Windows\Minidump"),
                    DisplaySize = GetFolderSize($@"C:\Windows\Minidump"),
                    Description = "Minidumps are smaller versions of memory dump files created when the system crashes. They contain essential information to diagnose the cause of the crash but are smaller in size than full memory dumps. These can be deleted if you no longer need to troubleshoot a crash.",
                    IsChecked = false,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"Temporary Windows Installation Files",
                    ItemPath = $@"C:\Windows\Temp",
                    ImagePath = "ms-appx:///Assets/imageres_2.ico",
                    Size = GetFolderLongSize($@"C:\Windows\Temp"),
                    DisplaySize = GetFolderSize($@"C:\Windows\Temp"),
                    Description = "These files are created during the installation or updating of Windows. They help ensure the installation process runs smoothly and are typically deleted once the process is complete. Deleting them frees up space without affecting system stability.\r\n",
                    IsChecked = false,
                });
            }
            if (disk == "C:" && IsAdministrator() == true)
            {
                items.Add(new CleanItem
                {
                    Name = $"Device Driver Packages",
                    ItemPath = @"C:\Windows\System32\DriverStore\FileRepository",
                    ImagePath = "ms-appx:///Assets/DDORes_2001.ico",
                    Size = GetFolderLongSizeDrivers(@"C:\Windows\System32\DriverStore\FileRepository"),
                    DisplaySize = GetFolderSizeDrivers(@"C:\Windows\System32\DriverStore\FileRepository"),
                    Description = "These are files related to hardware drivers installed on your system. They are used by Windows to manage hardware devices and ensure proper functionality. Over time, old or unused driver packages may accumulate and take up disk space. Cleaning up these packages can help free up storage and keep your system organized. Only outdated or redundant packages will be removed, while active drivers will remain unaffected.",
                    IsChecked = false,
                });
            }

            long size = 0;

            foreach (var item in items)
            {
                size += item.Size;
            }

            // Sort the list alphabetically by the Name property
            items.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            CleanItems.ItemsSource = items;

            Title.Title = $"You can use Disk Cleanup to free up to {FormatSize(size)} of disk space on ({disk}).";

            CleanItems.SelectedIndex = 0;

            CheckItems();

            string commandArgs = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

            if (commandArgs.Contains("CLEANALL"))
            {
                RunFullOptimization();
            }
        }

        public async void RunFullOptimization()
        {
            SelectAllBox.IsChecked = true;

            await Task.Delay(500);

            CleanItems.IsEnabled = false;
            CleanButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SelectAllBox.IsEnabled = false;
            MoreOptions.IsEnabled = false;
            ViewFiles.IsEnabled = false;
            Working.IsIndeterminate = true;
            (this as WindowEx).Title = $"Disk Cleanup : Cleaning drive ({Disk})... (This may take a while)";

            await Task.Delay(100);

            foreach (var item in items)
            {
                if (item.Name == "Thumbnails")
                {
                    DeleteFilesDB(item.ItemPath);
                }
                else if (item.Name == "Device Driver Packages")
                {
                    CleanupDriverStore();
                }
                else
                {
                    DeleteFiles(item.ItemPath);
                }
            }

            Close();
        }

        public enum KnownFolder
        {
            Contacts,
            Downloads,
            Favorites,
            Links,
            SavedGames,
            SavedSearches
        }

        public static class KnownFolders
        {
            private static readonly Dictionary<KnownFolder, Guid> _guids = new()
            {
                [KnownFolder.Contacts] = new("56784854-C6CB-462B-8169-88E350ACB882"),
                [KnownFolder.Downloads] = new("374DE290-123F-4565-9164-39C4925E467B"),
                [KnownFolder.Favorites] = new("1777F761-68AD-4D8A-87BD-30B759FA33DD"),
                [KnownFolder.Links] = new("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968"),
                [KnownFolder.SavedGames] = new("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"),
                [KnownFolder.SavedSearches] = new("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA")
            };

            public static string GetPath(KnownFolder knownFolder)
            {
                return SHGetKnownFolderPath(_guids[knownFolder], 0);
            }

            [DllImport("shell32",
                CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
            private static extern string SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
                nint hToken = 0);
        }

        public async void CheckItems()
        {
            try
            {
                await Task.Delay(10);

                int totalItems = 0;
                int selectedItems = 0;

                foreach (var item in items)
                {
                    totalItems++;
                    if (item.IsChecked == true) selectedItems++;
                }

                if (CleanItems.SelectedIndex >= 0) ItemDetails.Text = items[CleanItems.SelectedIndex].Description;

                if (selectedItems == 0)
                {
                    SelectAllBox.IsChecked = false;
                }
                else if (selectedItems == totalItems)
                {
                    SelectAllBox.IsChecked = true;
                }
                else
                {
                    SelectAllBox.IsChecked = null;
                }

                CheckItems();
            }
            catch
            {

            }
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            string customDefragPath = @"C:\Rebound11\rdfrgui.exe";
            string systemDefragPath = @"C:\Windows\System32\dfrgui.exe";

            try
            {
                if (File.Exists(customDefragPath))
                {
                    // Launch the custom defrag tool
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = customDefragPath,
                        UseShellExecute = true,
                        Verb = "runas" // Ensure it runs with admin rights
                    });
                }
                else
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    startInfo.Arguments = $"Start-Process -FilePath \"dfrgui\"";

                    try
                    {
                        var res = Process.Start(startInfo);
                        await res.WaitForExitAsync();
                        if (res.ExitCode == 0) return;
                        else throw new Exception();
                    }
                    catch (Exception ex)
                    {
                        await this.ShowMessageDialogAsync($"The system cannot find the file specified or the command line arguments are invalid.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
                ContentDialog noWifiDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Could not launch Disk Defragmenter: {ex.Message}",
                    CloseButtonText = "Ok"
                };

                await noWifiDialog.ShowAsync(); // Showing the error dialog
            }
        }

        private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Arguments = $"Start-Process -FilePath \"cleanmgr\"";

            try
            {
                var res = Process.Start(startInfo);
                await res.WaitForExitAsync();
                if (res.ExitCode == 0) Close();
                else throw new Exception();
            }
            catch (Exception ex)
            {
                await this.ShowMessageDialogAsync($"The system cannot find the file specified or the command line arguments are invalid.", "Error");
            }
        }

        private async void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures"));
        }

        private void CleanItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllBox.IsChecked == true)
            {
                CleanItems.ItemsSource = null;
                foreach (var item in items)
                {
                    item.IsChecked = true;
                }
                CleanItems.ItemsSource = items;
                CleanItems.SelectedIndex = 0;
            }
            if (SelectAllBox.IsChecked == false)
            {
                CleanItems.ItemsSource = null;
                foreach (var item in items)
                {
                    item.IsChecked = false;
                }
                CleanItems.ItemsSource = items;
                CleanItems.SelectedIndex = 0;
            }
        }

        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas",
                Arguments = @$"Start-Process ""shell:AppsFolder\e8dfd11c-954d-46a2-b700-9cbc6201f056_pthpn8nb9xcaa!App"" -ArgumentList ""{Disk}"" -Verb RunAs"
            });
            Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CleanItems.IsEnabled = false;
            CleanButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SelectAllBox.IsEnabled = false;
            MoreOptions.IsEnabled = false;
            ViewFiles.IsEnabled = false;
            Working.IsIndeterminate = true;
            (this as WindowEx).Title = $"Disk Cleanup : Cleaning drive ({Disk})... (This may take a while)";

            await Task.Delay(100);

            foreach (var item in items)
            {
                if (item.IsChecked == true)
                {
                    if (item.Name == "Thumbnails")
                    {
                        DeleteFilesDB(item.ItemPath);
                    }
                    else if (item.Name == "Device Driver Packages")
                    {
                        CleanupDriverStore();
                    }
                    else
                    {
                        DeleteFiles(item.ItemPath);
                    }
                }
            }

            Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ViewFiles_Click(object sender, RoutedEventArgs e)
        {
            if (items[CleanItems.SelectedIndex].ItemPath.Contains("Recycle.Bin"))
            {
                await Launcher.LaunchFolderPathAsync($"shell:RecycleBinFolder");
                return;
            }
            await Launcher.LaunchFolderPathAsync($"{items[CleanItems.SelectedIndex].ItemPath}");
        }
    }
}
