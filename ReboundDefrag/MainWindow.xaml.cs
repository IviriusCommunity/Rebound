using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32.TaskScheduler;
using Microsoft.WindowsAPICodePack.Taskbar;
using ReboundDefrag.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using WinUIEx;
using WinUIEx.Messaging;
using Task = System.Threading.Tasks.Task;

#nullable enable
#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method

namespace ReboundDefrag
{
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this?.InitializeComponent();
            // Set window backdrop to Mica for a modern translucent effect
            SystemBackdrop = new MicaBackdrop();

            // Set the window title
            Title = "Optimize Drives - Beta v0.1.0";

            // Window customization
            IsMaximizable = false;
            this.SetWindowSize(800, 670);
            IsResizable = false;
            AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
            this.CenterOnScreen();
            this.SetIcon(@$"{AppContext.BaseDirectory}/Assets/ReboundDefrag.ico");
        }

        public async Task LoadAppAsync()
        {
            AdvancedView.IsOn = GetBoolFromLocalSettings("Advanced");

            // Load data based on the current state of 'AdvancedView'
            await LoadData(AdvancedView.IsOn);

            // Begin monitoring window messages (such as device changes)
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowMessageMonitor mon = new(hWnd);

            // Subscribe to the WindowMessageReceived event
            mon.WindowMessageReceived += MessageReceived;

            // Set up a timer to periodically refresh the message listener every 5 seconds
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Timer interval set to 5 seconds
            };

            // Timer event handler
            timer.Tick += (sender, e) =>
            {
                mon.WindowMessageReceived += MessageReceived; // Re-subscribe to the event to keep monitoring
            };

            // Start the timer
            timer.Start();

            // Check if the application is running with administrator privileges
            IsAdministrator();
            CheckTask();
        }

        public static void SetProgressState(TaskbarProgressBarState state)
        {
            TaskbarManager.Instance.SetProgressState(state);
        }

        public static void SetProgressValue(int completed, int total)
        {
            TaskbarManager.Instance.SetProgressValue(completed, total);
        }

        private async void MessageReceived(object? sender, WindowMessageEventArgs e)
        {
            // Handle incoming messages
            switch (e.Message.MessageId)
            {
                default:
                    {
                        // No relevant message, break
                        break;
                    }
                case Win32Helper.WM_DEVICECHANGE:
                    {
                        // Handle specific device changes
                        switch ((int)e.Message.WParam)
                        {
                            case Win32Helper.DBT_DEVICEARRIVAL:
                                {
                                    // Device or partition inserted
                                    MyListView.ItemsSource = null; // Clear existing list
                                    await LoadData(AdvancedView.IsOn); // Reload data based on AdvancedView state
                                    break;
                                }
                            case Win32Helper.DBT_DEVICEREMOVECOMPLETE:
                                {
                                    // Device or partition removed
                                    MyListView.ItemsSource = null; // Clear existing list
                                    await LoadData(AdvancedView.IsOn); // Reload data based on AdvancedView state
                                    break;
                                }
                            default:
                                {
                                    // Handle any other device action
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        public class DiskItem : Item
        {
            public string? DriveLetter { get; set; }
            public string? MediaType { get; set; }
            public int ProgressValue { get; set; }
            public bool IsChecked { get; set; }
        }

        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Admin1.Visibility = Visibility.Collapsed;
                Admin2.Visibility = Visibility.Collapsed;
                Admin3.Visibility = Visibility.Collapsed;
                Admin4.Visibility = Visibility.Collapsed;
            }
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ListviewSelectionChange(object sender, SelectionChangedEventArgs args)
        {
            LoadSelectedItemInfo(GetStatus());
        }

        public string GetLastOptimizeDate()
        {
            if (MyListView.SelectedItem != null)
            {
                DiskItem? selectedItem = MyListView.SelectedItem as DiskItem;
                // Handle the selection change event

                try
                {
                    List<string> i = DefragInfo.GetEventLogEntriesForID(258);
                    return i.Last(s => s.Contains($"({selectedItem?.DriveLetter?.ToString().Remove(2, 1)})"));
                }
                catch
                {
                    return "Never....";
                }
            }
            else return "Unknown....";
        }

        public void LoadSelectedItemInfo(string status, string info = "....")
        {
            if (info == "....")
            {
                info = GetLastOptimizeDate();
            }
            else
            {
                info = "Unknown....";
            }
            if (MyListView.SelectedItem != null)
            {
                DiskItem? selectedItem = MyListView.SelectedItem as DiskItem;
                DetailsBar.Title = selectedItem?.Name;
                DetailsBar.Message = $"Media type: {selectedItem?.MediaType}\nLast analyzed or optimized: {info[..^4]}\nCurrent status: {status}";
                DetailsBar.Severity = InfoBarSeverity.Informational;
                OptimizeButton.IsEnabled = AdvancedView.IsEnabled;
                if (status.Contains("Needs optimization"))
                {
                    DetailsBar.Severity = InfoBarSeverity.Warning;
                }
                if (selectedItem?.MediaType == "CD-ROM")
                {
                    DetailsBar.Message = "Media type: CD-ROM\nLast analyzed or optimized: Never\nCurrent status: cannot be optimized";
                    DetailsBar.Severity = InfoBarSeverity.Error;
                    VisualStateManager.GoToState(OptimizeButton, "Disabled", true);
                    OptimizeButton.IsEnabled = false;
                }
                if (selectedItem?.Name == "EFI System Partition")
                {
                    DetailsBar.Message = $"Media type: {selectedItem.MediaType}\nLast analyzed or optimized: Never\nCurrent status: cannot be optimized (EFI System Partition)";
                    DetailsBar.Severity = InfoBarSeverity.Informational;
                }
                if (selectedItem?.Name == "Recovery Partition")
                {
                    DetailsBar.Message = $"Media type: {selectedItem.MediaType}\nLast analyzed or optimized: Never\nCurrent status: cannot be optimized (Recovery Partition)";
                    DetailsBar.Severity = InfoBarSeverity.Error;
                    VisualStateManager.GoToState(OptimizeButton, "Disabled", true);
                    OptimizeButton.IsEnabled = false;
                }
            }
        }

        public string GetStatus()
        {
            string status = string.Empty;

            try
            {
                if (MyListView.SelectedItem != null)
                {
                    List<string> i = DefragInfo.GetEventLogEntriesForID(258);
                    var selectedItem = MyListView.SelectedItem as DiskItem;

                    var selI = i.Last(s => s.Contains($"({selectedItem?.DriveLetter?.ToString().Remove(2, 1)})"));

                    var localDate = DateTime.Parse(selI[..^4]);

                    // Get the current local date and time
                    DateTime currentDate = DateTime.Now;

                    // Calculate the days passed
                    TimeSpan timeSpan = currentDate - localDate;
                    int daysPassed = timeSpan.Days;

                    if (daysPassed == 0)
                    {
                        //return $"OK (Last optimized: today)";
                        return $"OK";
                    }

                    if (daysPassed == 1)
                    {
                        //return $"OK (Last optimized: yesterday)";
                        return $"OK";
                    }

                    if (daysPassed < 50)
                    {
                        //return $"OK (Last optimized: {daysPassed} days ago)";
                        return $"OK";
                    }

                    if (daysPassed >= 50)
                    {
                        //return $"Needs optimization (Last optimized: {daysPassed} days ago)";
                        return $"Needs optimization";
                    }

                    else return "Unknown";
                }
                else
                {
                    return "Please select an item to proceed.";
                }
            }
            catch
            {
                return "Needs optimization";
            }
        }

        public void Lock(bool areItemsEnabled, string message = "", bool indeterminate = true)
        {
            AnalyzeButton.IsEnabled = areItemsEnabled;
            OptimizeButton.IsEnabled = areItemsEnabled;
            MyListView.IsEnabled = areItemsEnabled;
            AdvancedView.IsEnabled = areItemsEnabled;
            CurrentDisk.Visibility = areItemsEnabled == true ? Visibility.Collapsed : Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(message)) CurrentDisk.Text = message;
            if (indeterminate == true) CurrentProgress.IsIndeterminate = !areItemsEnabled;
            else CurrentProgress.IsIndeterminate = false;
            VisualStateManager.GoToState(OptimizeButton, areItemsEnabled == true ? "Normal" : "Disabled", true);
        }

        public async Task LoadData(bool loadSystemPartitions)
        {
            Lock(false, "Loading drive information...");

            // Initial delay
            // Essential for ensuring the UI loads before starting tasks
            await Task.Delay(100);

            List<DiskItem> items = [];

            // Get the logical drives bitmask
            uint drivesBitMask = Win32Helper.GetLogicalDrives();
            if (drivesBitMask == 0)
            {
                return;
            }

            for (char driveLetter = 'A'; driveLetter <= 'Z'; driveLetter++)
            {
                uint mask = 1u << (driveLetter - 'A');
                if ((drivesBitMask & mask) != 0)
                {
                    string drive = $"{driveLetter}:\\";

                    StringBuilder volumeName = new(261);
                    StringBuilder fileSystemName = new(261);
                    if (Win32Helper.GetVolumeInformation(drive, volumeName, volumeName.Capacity, out _, out _, out _, fileSystemName, fileSystemName.Capacity))
                    {
                        var newDriveLetter = drive.ToString().Remove(2, 1);
                        string mediaType = GetDriveTypeDescriptionAsync(drive);

                        if (volumeName.ToString() != string.Empty)
                        {
                            var item = new DiskItem
                            {
                                Name = $"{volumeName} ({newDriveLetter})",
                                ImagePath = "ms-appx:///Assets/Drive.png",
                                MediaType = mediaType,
                                DriveLetter = drive,
                            };
                            if (item.MediaType == "Removable")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveRemovable.png";
                            }
                            if (item.MediaType == "Unknown")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveUnknown.png";
                            }
                            if (item.MediaType == "CD-ROM")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveOptical.png";
                            }
                            if (item.DriveLetter.Contains('C'))
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveWindows.png";
                            }
                            item.IsChecked = GetBoolFromLocalSettings(ConvertStringToNumericRepresentation(drive));
                            items.Add(item);
                        }
                        else
                        {
                            var item = new DiskItem
                            {
                                Name = $"({newDriveLetter})",
                                ImagePath = "ms-appx:///Assets/Drive.png",
                                MediaType = mediaType,
                                DriveLetter = drive,
                            };
                            if (item.MediaType == "Removable")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveRemovable.png";
                            }
                            if (item.MediaType == "Unknown")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveUnknown.png";
                            }
                            if (item.MediaType == "CD-ROM")
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveOptical.png";
                            }
                            if (item.DriveLetter.Contains('C'))
                            {
                                item.ImagePath = "ms-appx:///Assets/DriveWindows.png";
                            }
                            item.IsChecked = GetBoolFromLocalSettings(ConvertStringToNumericRepresentation(drive));
                            items.Add(item);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"  Failed to get volume information for {drive}");
                    }
                }
            }

            if (loadSystemPartitions)
            {
                var syspart = SystemVolumes.GetSystemVolumes();

                // Add system partitions to the items list
                foreach (var result in syspart)
                {
                    string? driveType = string.Empty;
                    foreach (var diskitem in items)
                    {
                        if (diskitem.DriveLetter != null && diskitem.DriveLetter.Contains('C'))
                        {
                            driveType = diskitem.MediaType;
                        }
                    }
                    var item = new DiskItem
                    {
                        Name = result.FriendlyName,
                        ImagePath = "ms-appx:///Assets/DriveSystem.png",
                        MediaType = driveType,
                        DriveLetter = result.GUID,
                    };
                    if (result.GUID != null) item.IsChecked = GetBoolFromLocalSettings(ConvertStringToNumericRepresentation(result.GUID));
                    items.Add(item);
                }
            }

            int selIndex = MyListView.SelectedIndex is not -1 ? MyListView.SelectedIndex : 0;

            // Set the list view's item source
            MyListView.ItemsSource = items;

            MyListView.SelectedIndex = selIndex >= items.Count ? items.Count - 1 : selIndex;

            Lock(true);
        }

        public static string ConvertStringToNumericRepresentation(string input)
        {
            // Create a StringBuilder to store the numeric representation
            StringBuilder numericRepresentation = new();

            // Iterate over each character in the string
            foreach (char c in input)
            {
                // Convert the character to its ASCII value and append it
                numericRepresentation.Append((int)c);
            }

            // Return the numeric representation as a string
            return numericRepresentation.ToString();
        }

        // Method to read a bool value from LocalSettings
        public static bool GetBoolFromLocalSettings(string name)
        {
            // Access the local settings
            var localSettings = ApplicationData.Current.LocalSettings;

            // Check if the key exists and return the bool value if it does
            if (localSettings.Values.ContainsKey(name))
            {
                // Try to get the value and cast it to bool
                if (localSettings.Values[name] is bool value)
                {
                    return value;
                }
            }

            // Return false if the key does not exist or is not a bool
            return false;
        }

        // Method to write a bool value to LocalSettings
        public static bool WriteBoolToLocalSettings(string name, bool value)
        {
            try
            {
                // Access the local settings
                var localSettings = ApplicationData.Current.LocalSettings;

                // Write the bool value to the settings with the given key
                localSettings.Values[name] = value;

                return true; // Return true if write is successful
            }
            catch
            {
                return false; // Return false if an error occurs
            }
        }

        public static string GetDiskDriveFromLetter(string driveLetter)
        {
            try
            {
                var FULLSEARCHER = new ManagementObjectSearcher("root\\Microsoft\\Windows\\Storage", "SELECT * FROM MSFT_PhysicalDisk");
                foreach (ManagementObject queryObj in FULLSEARCHER.Get().Cast<ManagementObject>())
                {
                    string? MEDIATYPE = queryObj["MediaType"].ToString();
                    string? DEVICEID = queryObj["DeviceID"].ToString();

                    // Create a ManagementObjectSearcher to query the Win32_DiskDrive class
                    ManagementObjectSearcher searcher2 = new("SELECT * FROM Win32_DiskDrive");

                    foreach (ManagementObject disk in searcher2.Get().Cast<ManagementObject>())
                    {
                        // Get the device ID of the disk
                        string? deviceID2 = disk["DeviceID"].ToString();

                        // Create a ManagementObjectSearcher to query the Win32_LogicalDiskToPartition class
                        ManagementObjectSearcher partitionSearcher = new(
                            $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceID2}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                        foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>())
                        {
                            // Create a ManagementObjectSearcher to query the Win32_LogicalDisk class
                            ManagementObjectSearcher logicalDiskSearcher = new(
                                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

                            foreach (ManagementObject logicalDisk in logicalDiskSearcher.Get().Cast<ManagementObject>())
                            {
                                // Check if the logical disk is drive C
                                if (logicalDisk["DeviceID"].ToString() == driveLetter.Replace(@"\", ""))
                                {
                                    if (deviceID2 != null && DEVICEID != null && deviceID2.Contains(DEVICEID))
                                    {
                                        string driveType = MEDIATYPE switch
                                        {
                                            "3" => "HDD (Hard Disk Drive)",
                                            "4" => "SSD (Solid State Drive)",
                                            _ => "Unknown"
                                        };

                                        return driveType;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }
            return "Error";
        }

        private static async Task<string?> GetDeviceIdFromDriveAsync(string driveRoot)
        {
            string driveLetter = driveRoot.TrimEnd('\\'); // Clean the input

            return await Task.Run(async () =>
            {
                string query = $"SELECT * FROM Win32_DiskDrive";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject logicalDisk in searcher.Get().Cast<ManagementObject>())
                    {
                        if ((string)logicalDisk["DeviceID"] == await GetDeviceIdFromDriveAsync(driveRoot))
                            return logicalDisk["DeviceID"]?.ToString(); // Return the DeviceID of the disk drive
                    }
                }
                return "Unknown"; // Fallback if not found
            });
        }

        public static string GetDriveTypeDescriptionAsync(string driveRoot)
        {
            Win32Helper.DriveType driveType = Win32Helper.GetDriveType(driveRoot);

            return driveType switch
            {
                Win32Helper.DriveType.DRIVE_REMOVABLE => "Removable",
                Win32Helper.DriveType.DRIVE_FIXED => GetDiskDriveFromLetter(driveRoot),
                Win32Helper.DriveType.DRIVE_REMOTE => "Network",
                Win32Helper.DriveType.DRIVE_CDROM => "CD-ROM",
                Win32Helper.DriveType.DRIVE_RAMDISK => "RAM Disk",
                Win32Helper.DriveType.DRIVE_NO_ROOT_DIR => "No Root Directory",
                _ => "Unknown",
            };
        }

        private void Button_Click(object sender, SplitButtonClickEventArgs e)
        {
            OptimizeSelected(AdvancedView.IsOn);
        }

        public static void RestartAsAdmin(string args)
        {
            var packageName = Package.Current.Id.FamilyName;
            var appId = CoreApplication.Id;

            // Request elevation
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Start-Process 'shell:AppsFolder\\{packageName}!{appId}' -ArgumentList @('{args}') -Verb RunAs\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
                App.Current.Exit();
            }
            catch
            {

            }
        }

        public async void OptimizeSelected(bool systemPartitions)
        {
            AdvancedView.IsOn = systemPartitions;

            if (!IsAdministrator())
            {
                RestartAsAdmin($"SELECTED{(systemPartitions ? "-SYSTEM" : "")} {MyListView.SelectedIndex}");
                return;
            }

            foreach (var item in (List<DiskItem?>)MyListView.ItemsSource)
            {
                if (item?.IsChecked == true)
                {
                    Lock(false, "Loading...", true);

                    MyListView.SelectedIndex = ((List<DiskItem?>)MyListView.ItemsSource).IndexOf(item);

                    string scriptPath = "C:\\Rebound11\\rdfrgui.ps1";
                    string? volume = item?.DriveLetter?.ToString().Remove(1, 2);
                    string arguments = $@"
$job = Start-Job -ScriptBlock {{
    $global:OutputLines = @()
    
    # Capture all output including verbose messages
    Optimize-Volume -DriveLetter {volume} -Defrag -Verbose | ForEach-Object {{
        # Check if it's a progress update
        if ($_ -like ""*Progress*"") {{
            Write-Output ""Progress: $_""  # Modify to capture progress as needed
        }} else {{
            $global:OutputLines += $_
            Write-Output $_  # Output normal messages
        }}
    }}
}}

while ($job.State -eq 'Running') {{
    Clear-Host
    Start-Sleep -Seconds 0.01
    $output = Receive-Job -Id $job.Id -Keep
    if ($output) {{
        Write-Output $output[-1]  # Output the last line
    }}
}}

# Final output after the job completes
Receive-Job -Id $job.Id | ForEach-Object {{ Write-Output $_ }}

";

                    // Ensure the script is written before proceeding
                    await File.WriteAllTextAsync(scriptPath, arguments);

                    try
                    {
                        if (DetailsBar.Severity == InfoBarSeverity.Error)
                        {
                            LoadSelectedItemInfo("Cannot be optimized. Skipping...");
                            await Task.Delay(1000);
                            continue;
                        }
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",  // Use -File to execute the script
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Verb = "runas"  // Run as administrator
                        };

                        using var process = new Process { StartInfo = processInfo };

                        string outputData = "0";
                        bool updateData = true;

                        var alreadyUsed = new List<string>();

                        process.OutputDataReceived += UpdateOutput;

                        void UpdateOutput(object sender, DataReceivedEventArgs args)
                        {
                            // Only process if there's data
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                // Use the dispatcher to update the UI
                                DispatcherQueue.TryEnqueue(() => { UpdateIO(args.Data); });

                                // Store the output data
                                outputData = "\n" + args.Data;
                            }
                        }

                        process.Start();
                        process.BeginOutputReadLine();

                        Lock(false, "", true);
                        LoadSelectedItemInfo("Optimizing...");

                        void UpdateIO(string data)
                        {
                            if (!updateData) return;

                            if (data.Contains("VERBOSE: ") && data.Contains(" complete."))
                            {
                                string a = data[data.LastIndexOf("VERBOSE: ")..].Replace("VERBOSE: ", string.Empty);

                                string dataToReplace = " complete.";

                                DispatcherQueue.TryEnqueue(() => { RunUpdate(a, dataToReplace); });
                            }
                        }

                        void RunUpdate(string a, string dataToReplace)
                        {
                            if (alreadyUsed.Contains(a) != true)
                            {
                                alreadyUsed.Add(a);
                                CurrentProgress.Value = GetMaxPercentage(a);
                                CurrentProgress.IsIndeterminate = false;
                                SetProgressState(TaskbarProgressBarState.Normal);
                                SetProgressValue((int)CurrentProgress.Value, (int)CurrentProgress.Maximum);
                                if (a.Contains(" complete..."))
                                {
                                    dataToReplace = " complete...";
                                    if (item?.DriveLetter?.ToString().Contains('}') != true) CurrentDisk.Text = $"Drive {volume}: - {a.Remove(a.IndexOf(" complete..."))}";
                                    else CurrentDisk.Text = $"{((DiskItem)MyListView.SelectedItem).Name} - {a.Remove(a.IndexOf(" complete..."))}";
                                }
                                else if (a.Contains(" complete."))
                                {
                                    dataToReplace = " complete.";
                                    if (item?.DriveLetter?.ToString().Contains('}') != true) CurrentDisk.Text = $"Drive {volume}: - {a.Remove(a.IndexOf(" complete."))}";
                                    else CurrentDisk.Text = $"{((DiskItem)MyListView.SelectedItem).Name} - {a.Remove(a.IndexOf(" complete."))}";

                                }
                            }
                        }

                        await process.WaitForExitAsync();

                        updateData = false;
                        Lock(true);
                        LoadSelectedItemInfo(GetStatus());
                        File.Delete(scriptPath);
                        alreadyUsed.Clear();
                        CurrentProgress.Value = 0;
                        SetProgressState(TaskbarProgressBarState.NoProgress);
                    }
                    catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
                    {
                        ShowMessage("Defragmentation was canceled by the user.");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage($"Error: {ex.Message}");
                    }
                }
            }
        }

        private static int GetMaxPercentage(string data)
        {
            static string KeepOnlyNumbers(string input)
            {
                StringBuilder sb = new();

                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }
            var match = KeepOnlyNumbers(data);
            return int.Parse(match);
        }

        public async void OptimizeAll(bool close, bool systemPartitions)
        {
            AdvancedView.IsOn = systemPartitions;

            await LoadData(systemPartitions);

            MyListView.IsEnabled = false;

            if (!IsAdministrator())
            {
                if (close == true) RestartAsAdmin($"OPTIMIZEALLANDCLOSE{(systemPartitions ? "-SYSTEM" : "")}");
                else RestartAsAdmin($"OPTIMIZEALL{(systemPartitions ? "-SYSTEM" : "")}");
                return;
            }

            int i = 0;
            int j = ((List<DiskItem>)MyListView.ItemsSource).Count;

            MyListView.SelectedIndex = 0;

            foreach (var item in (List<DiskItem>)MyListView.ItemsSource)
            {
                Lock(false, "Optimizing...", true);

                string scriptPath = "C:\\Rebound11\\rdfrgui.ps1";
                string? volume = item.DriveLetter?.ToString().Remove(1, 2);
                string arguments = $@"
$job = Start-Job -ScriptBlock {{
    $global:OutputLines = @()
    
    # Capture all output including verbose messages
    Optimize-Volume -DriveLetter {volume} -Defrag -Verbose | ForEach-Object {{
        # Check if it's a progress update
        if ($_ -like ""*Progress*"") {{
            Write-Output ""Progress: $_""  # Modify to capture progress as needed
        }} else {{
            $global:OutputLines += $_
            Write-Output $_  # Output normal messages
        }}
    }}
}}

while ($job.State -eq 'Running') {{
    Clear-Host
    Start-Sleep -Seconds 0.01
    $output = Receive-Job -Id $job.Id -Keep
    if ($output) {{
        Write-Output $output[-1]  # Output the last line
    }}
}}

# Final output after the job completes
Receive-Job -Id $job.Id | ForEach-Object {{ Write-Output $_ }}

";

                // Ensure the script is written before proceeding
                await File.WriteAllTextAsync(scriptPath, arguments);

                try
                {
                    i++;

                    if (DetailsBar.Severity == InfoBarSeverity.Error)
                    {
                        LoadSelectedItemInfo("Cannot be optimized. Skipping...");
                        await Task.Delay(1000);
                        if (MyListView.SelectedIndex + 1 != j) MyListView.SelectedIndex++;
                        continue;
                    }
                        var processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",  // Use -File to execute the script
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas"  // Run as administrator
                    };

                    using var process = new Process { StartInfo = processInfo };

                    string outputData = "0";
                    bool updateData = true;

                    var alreadyUsed = new List<string>();

                    process.OutputDataReceived += UpdateOutput;

                    void UpdateOutput(object sender, DataReceivedEventArgs args)
                    {
                        // Only process if there's data
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            // Use the dispatcher to update the UI
                            DispatcherQueue.TryEnqueue(() => { UpdateIO(args.Data); });

                            // Store the output data
                            outputData = "\n" + args.Data;
                        }
                    }

                    process.Start();
                    process.BeginOutputReadLine();

                    Lock(false, "", true);
                    LoadSelectedItemInfo("Optimizing...");

                    void UpdateIO(string data)
                    {
                        if (!updateData) return;

                        if (data.Contains("VERBOSE: ") && data.Contains(" complete."))
                        {
                            string a = data[data.LastIndexOf("VERBOSE: ")..].Replace("VERBOSE: ", string.Empty);

                            string dataToReplace = " complete.";

                            DispatcherQueue.TryEnqueue(() => { RunUpdate(a, dataToReplace); });
                        }
                    }

                    void RunUpdate(string a, string dataToReplace)
                    {
                        if (alreadyUsed.Contains(a) != true)
                        {
                            alreadyUsed.Add(a);
                            CurrentProgress.IsIndeterminate = false;
                            CurrentProgress.Value = GetMaxPercentage(a);
                            SetProgressState(TaskbarProgressBarState.Normal);
                            SetProgressValue((int)CurrentProgress.Value, (int)CurrentProgress.Maximum);
                            if (a.Contains(" complete..."))
                            {
                                dataToReplace = " complete...";
                                if (item.DriveLetter?.ToString().Contains('}') != true) CurrentDisk.Text = $"Drive {i}/{j} ({volume}:) - {a.Remove(a.IndexOf(" complete..."))}";
                                else CurrentDisk.Text = $"{((DiskItem)MyListView.SelectedItem).Name} ({i}/{j}) - {a.Remove(a.IndexOf(" complete..."))}";
                            }
                            else if (a.Contains(" complete."))
                            {
                                dataToReplace = " complete.";
                                if (item.DriveLetter?.ToString().Contains('}') != true) CurrentDisk.Text = $"Drive {i}/{j} ({volume}:) - {a.Remove(a.IndexOf(" complete."))}";
                                else CurrentDisk.Text = $"{((DiskItem)MyListView.SelectedItem).Name} ({i}/{j}) - {a.Remove(a.IndexOf(" complete."))}";

                            }
                        }
                    }

                    await process.WaitForExitAsync();

                    updateData = false;
                    Lock(true, "", false);
                    LoadSelectedItemInfo(GetStatus());
                    File.Delete(scriptPath);
                    alreadyUsed.Clear();
                    CurrentProgress.Value = 0;
                    SetProgressState(TaskbarProgressBarState.NoProgress);
                    if (MyListView.SelectedIndex + 1 != j) MyListView.SelectedIndex++;
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    ShowMessage("Defragmentation was canceled by the user.");
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error: {ex.Message}");
                }
            }

            i = 0;

            MyListView.IsEnabled = true;

            if (close == true) Close();
        }

        private async void ShowMessage(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Defragmentation",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                _ = await dialog.ShowAsync();
            }
            catch
            {

            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            OptimizeSelected(AdvancedView.IsOn);
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            OptimizeAll(false, AdvancedView.IsOn);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (IsAdministrator() != true)
            {
                RestartAsAdmin($"TASK");
                return;
            }

            OpenTaskWindow();
        }

        public async void CheckTask()
        {
            await Task.Delay(100);
            try
            {
                ScheduledOptimizationDetails.Text = GetTaskFrequency();
                if (GetTaskFrequency() is not "Off")
                {
                    ScheduledTaskText.Text = "Configure";
                }
                else
                {
                    ScheduledTaskText.Text = "Turn on";
                }

                CheckTask();
            }
            catch
            {

            }
        }

        public void OpenTaskWindow()
        {
            var win = new ScheduledOptimization(this.AppWindow.Position.X, this.AppWindow.Position.Y);
            Win32Helper.CreateModalWindow(this, win, true, true);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LoadSelectedItemInfo(GetStatus());
        }

        private async void AdvancedView_Toggled(object sender, RoutedEventArgs e)
        {
            WriteBoolToLocalSettings("Advanced", AdvancedView.IsOn);
            await LoadData(AdvancedView.IsOn);
        }

        private async void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://ivirius.vercel.app/docs/rebound11/defragment-and-optimize-drives/"));
        }

        private void CheckBox_Click(object? sender, RoutedEventArgs e)
        {
            var name = ((DiskItem?)((CheckBox?)sender)?.DataContext)?.DriveLetter;
            var isChecked = ((CheckBox?)sender)?.IsChecked;
            if (name != null && isChecked != null) WriteBoolToLocalSettings(ConvertStringToNumericRepresentation(name), (bool)isChecked);
        }

        public static string GetTaskFrequency()
        {
            using (TaskService ts = new())
            {
                // Specify the path to the task in Task Scheduler
                TaskFolder defragFolder = ts.GetFolder(@"Microsoft\Windows\Defrag");

                // Retrieve the scheduled task
                Microsoft.Win32.TaskScheduler.Task task = defragFolder.GetTasks()["ScheduledDefrag"];

                if (task.Enabled != true)
                {
                    return $"Off";
                }

                if (task != null)
                {
                    // Check the triggers for their type
                    foreach (var trigger in task.Definition.Triggers)
                    {
                        switch (trigger)
                        {
                            case DailyTrigger _:
                                return "On (Frequency: daily)";
                            case WeeklyTrigger _:
                                return "On (Frequency: weekly)";
                            case MonthlyTrigger _:
                                return "On (Frequency: monthly)";
                        }
                    }

                    return "On (Frequency: unknown)";
                }
                else
                {
                    return $"Off";
                }
            }
        }

        public static string GetTaskCommand()
        {
            using (TaskService ts = new())
            {
                // Specify the path to the task in Task Scheduler
                TaskFolder defragFolder = ts.GetFolder(@"Microsoft\Windows\Defrag");

                // Retrieve the scheduled task
                Microsoft.Win32.TaskScheduler.Task task = defragFolder.GetTasks()["ScheduledDefrag"];

                if (task != null)
                {
                    // Check the triggers for their type
                    foreach (var action in task.Definition.Actions)
                    {
                        if (action is ExecAction ex)
                        {
                            return ex.Arguments;
                        }
                    }

                    return "None";
                }
                else
                {
                    return $"None";
                }
            }
        }

        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {
            Process.Start("dfrgui.exe");
            Close();
        }
    }

    public class DefragInfo
    {
        public static List<string> GetEventLogEntriesForID(int eventID)
        {
            List<string> eventMessages = [];

            // Define the query
            string logName = "Application"; // Windows Logs > Application
            string queryStr = "*[System/EventID=" + eventID + "]";

            EventLogQuery query = new(logName, PathType.LogName, queryStr);

            // Create the reader
            using (EventLogReader reader = new(query))
            {
                for (EventRecord eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
                {
                    // Extract the message from the event
                    string sb = string.Concat(eventInstance.TimeCreated.ToString(), eventInstance.FormatDescription().ToString().AsSpan(eventInstance.FormatDescription().ToString().Length - 4));

                    eventMessages.Add(sb.ToString());
                }
            }

            return eventMessages;
        }
    }
}
