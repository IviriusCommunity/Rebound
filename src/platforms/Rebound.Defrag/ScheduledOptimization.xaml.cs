using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32.TaskScheduler;
using Rebound.Defrag.Controls;
using Rebound.Defrag.Helpers;
using Rebound.Helpers;
using WinUIEx;
using static Rebound.Defrag.MainWindow;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace Rebound.Defrag;

public sealed partial class ScheduledOptimization : WindowEx
{
    public static string GetTaskFrequency()
    {
        using TaskService ts = new();
        // Specify the path to the task in Task Scheduler
        var defragFolder = ts.GetFolder(@"Microsoft\Windows\Defrag");

        // Retrieve the scheduled task
        var task = defragFolder.GetTasks()["ScheduledDefrag"];

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

    public static string GetTaskCommand()
    {
        using TaskService ts = new();
        // Specify the path to the task in Task Scheduler
        var defragFolder = ts.GetFolder(@"Microsoft\Windows\Defrag");

        // Retrieve the scheduled task
        var task = defragFolder.GetTasks()["ScheduledDefrag"];

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

    public ScheduledOptimization(int parentX, int parentY)
    {
        this.InitializeComponent();
        Win32Helper.RemoveIcon(this);
        IsMaximizable = false;
        IsMinimizable = false;
        this.MoveAndResize(parentX + 50, parentY + 50, 550, 600);
        IsResizable = false;
        Title = "Scheduled optimization";
        SystemBackdrop = new MicaBackdrop();
        //LoadData();
        if (GetTaskFrequency() is not "Off")
        {
            EnableTaskSwitch.IsOn = true;
            if (GetTaskFrequency().Contains("daily", StringComparison.CurrentCultureIgnoreCase))
            {
                Frequency.SelectedIndex = 0;
            }
            if (GetTaskFrequency().Contains("weekly", StringComparison.CurrentCultureIgnoreCase))
            {
                Frequency.SelectedIndex = 1;
            }
            if (GetTaskFrequency().Contains("monthly", StringComparison.CurrentCultureIgnoreCase))
            {
                Frequency.SelectedIndex = 2;
            }
        }
        CheckIsOn();
        CheckData();
    }

    public async void CheckData()
    {
        await Task.Delay(500);

        if (GetTaskCommand().Contains("/E"))
        {
            OptimizeNew.IsChecked = true;
            foreach (var disk in (List<CommonDriveListViewItem>)MyListView.ItemsSource)
            {
                var letter = disk.DrivePath != null && disk.DrivePath.EndsWith('\\') ? disk.DrivePath[..^1] : disk.DrivePath;
                disk.IsChecked = letter == null || !GetTaskCommand().Contains(letter);
            }
        }
        else
        {
            OptimizeNew.IsChecked = false;
            foreach (var disk in (List<CommonDriveListViewItem>)MyListView.ItemsSource)
            {
                var letter = disk.DrivePath != null && disk.DrivePath.EndsWith('\\') ? disk.DrivePath[..^1] : disk.DrivePath;
                disk.IsChecked = letter != null && GetTaskCommand().Contains(letter);
            }
        }
        CheckSelectAll();
    }

    public static void ScheduleDefragTask(List<CommonDriveListViewItem> items, bool optimizeNewDrives, string scheduleFrequency)
    {
        using TaskService ts = new();
        // Create or open the Defrag folder in Task Scheduler
        var defragFolder = ts.GetFolder(@"\Microsoft\Windows\Defrag");

        // Retrieve the ScheduledDefrag task if it exists
        var scheduledDefrag = defragFolder.GetTasks()["ScheduledDefrag"];

        // If the task exists, we'll modify it
        TaskDefinition td;
        if (scheduledDefrag != null)
        {
            td = scheduledDefrag.Definition;
        }
        else
        {
            td = ts.NewTask();
            td.RegistrationInfo.Description = "Scheduled Defrag Task";
            td.Settings.Priority = ProcessPriorityClass.High;
            td.Settings.Volatile = false;
            td.Settings.RunOnlyIfLoggedOn = false;
        }

        // Set triggers based on the scheduleFrequency input
        td.Triggers.Clear();
        switch (scheduleFrequency.ToLower())
        {
            case "daily":
                { td.Triggers.Add(new DailyTrigger { DaysInterval = 1 });
                    break;
                }
            case "weekly":
                {
                    td.Triggers.Add(new WeeklyTrigger { DaysOfWeek = DaysOfTheWeek.Sunday });
                        break;
                }
            case "monthly":
                {
                    td.Triggers.Add(new MonthlyTrigger { DaysOfMonth = [1] });
                        break;
                }
        };

        // Build the defrag command with selected drives
        if (optimizeNewDrives == true)
        {
            List<string> drives = [];
            foreach (var disk in items)
            {
                if (disk.IsChecked == false)
                {
                    var letter = disk.DrivePath != null && disk.DrivePath.EndsWith('\\') ? disk.DrivePath[..^1] : disk.DrivePath;
                    if (letter != null)
                    {
                        drives.Add(letter);
                    }
                }
            }
            var defragCommand = string.Join(" ", drives);
            td.Actions.Clear();
            _ = td.Actions.Add(new ExecAction("%SystemRoot%\\System32\\defrag.exe", $"/E {defragCommand}", null));  // Optimizing the drives
        }
        else
        {
            List<string> drives = [];
            foreach (var disk in items)
            {
                if (disk.IsChecked == true)
                {
                    var letter = disk.DrivePath != null && disk.DrivePath.EndsWith('\\') ? disk.DrivePath[..^1] : disk.DrivePath;
                    if (letter != null)
                    {
                        drives.Add(letter);
                    }
                }
            }
            var defragCommand = string.Join(" ", drives);
            td.Actions.Clear();
            _ = td.Actions.Add(new ExecAction("%SystemRoot%\\System32\\defrag.exe", $"{defragCommand} /O", null));  // Optimizing the drives
        }

        // Register or update the task
        _ = defragFolder.RegisterTaskDefinition("ScheduledDefrag", td);

        // Retrieve the ScheduledDefrag task if it exists
        var newScheduledDefrag = defragFolder.GetTasks()["ScheduledDefrag"];

        newScheduledDefrag.Enabled = true;
    }

    public static void TurnOffDefragTask()
    {
        using TaskService ts = new();
        // Create or open the Defrag folder in Task Scheduler
        var defragFolder = ts.GetFolder(@"\Microsoft\Windows\Defrag");

        // Retrieve the ScheduledDefrag task if it exists
        var scheduledDefrag = defragFolder.GetTasks()["ScheduledDefrag"];

        scheduledDefrag.Enabled = false;
    }

    public static string GetDriveTypeDescription(string driveRoot)
    {
        var driveType = Win32Helper.GetDriveType(driveRoot);

        return driveType switch
        {
            Win32Helper.DriveType.DRIVE_REMOVABLE => "Removable",
            Win32Helper.DriveType.DRIVE_FIXED => "Fixed",
            Win32Helper.DriveType.DRIVE_REMOTE => "Network",
            Win32Helper.DriveType.DRIVE_CDROM => "CD-ROM",
            Win32Helper.DriveType.DRIVE_RAMDISK => "RAM Disk",
            Win32Helper.DriveType.DRIVE_NO_ROOT_DIR => "No Root Directory",
            _ => "Unknown",
        };
    }

    public static List<CommonDriveListViewItem> GetDriveItems()
    {
        // The drive items
        List<CommonDriveListViewItem> items = [];

        // Get the logical drives bitmask
        var drivesBitMask = Win32Helper.GetLogicalDrives();

        // If there are no drives return
        if (drivesBitMask is 0)
        {
            return [];
        }

        // Obtain each drive
        for (var singularDriveLetter = 'A'; singularDriveLetter <= 'Z'; singularDriveLetter++)
        {
            // Get mask
            var mask = 1u << (singularDriveLetter - 'A');

            if ((drivesBitMask & mask) != 0)
            {
                // Convert single drive letter into drive path of format C:\
                var drive = $"{singularDriveLetter}:\\";

                // Create string builders
                StringBuilder volumeName = new(261);
                StringBuilder fileSystemName = new(261);

                // Obtain volume information using P/Invoke
                if (Win32Helper.GetVolumeInformation(drive, volumeName, volumeName.Capacity, out _, out _, out _, fileSystemName, fileSystemName.Capacity))
                {
                    // Convert singular drive letter of format C into drive letter of format C:
                    var driveLetter = $"{singularDriveLetter}:";

                    // Obtain the drive media type for drive path
                    var mediaType = GetDriveTypeDescription(drive);

                    // Create the drive item
                    var item = new CommonDriveListViewItem
                    {
                        // Set the friendly name of format Local Disk (C:)
                        DriveName = volumeName.ToString() == string.Empty ? $"({driveLetter})" : $"{volumeName} ({driveLetter})",
                        ImagePath = "ms-appx:///Assets/Drive.png",
                        MediaType = mediaType,
                        DrivePath = drive,
                    };

                    // Set the icon for the drive
                    item.ImagePath = item.MediaType switch
                    {
                        "Removable" => "ms-appx:///Assets/DriveRemovable.png",
                        "Unknown" => "ms-appx:///Assets/DriveUnknown.png",
                        "CD-ROM" => "ms-appx:///Assets/DriveOptical.png",
                        _ => "ms-appx:///Assets/Drive.png"
                    };

                    // If the drive is the Windows installation drive use the Windows drive icon
                    item.ImagePath = driveLetter == EnvironmentHelper.GetWindowsInstallationDrivePath().DrivePathToLetter() ? "ms-appx:///Assets/DriveWindows.png" : item.ImagePath;

                    // Add to collection
                    items.Add(item);
                }
            }
        }

        // Return the drives list
        return items;
    }

    private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OptimizeNew.IsChecked ??= true;

        var frequencyItem = Frequency.SelectedItem as ComboBoxItem;
        var frequencyContent = frequencyItem?.Content?.ToString() ?? string.Empty;

        if (EnableTaskSwitch.IsOn == true)
        {
            ScheduleDefragTask((List<CommonDriveListViewItem>?)MyListView.ItemsSource ?? [], OptimizeNew.IsChecked ?? false, frequencyContent);
            Close();
            return;
        }
        else
        {
            TurnOffDefragTask();
            Close();
            return;
        }
    }

    private void Button_Click_1(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Close();

    private void EnableTaskSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => CheckIsOn();

    public void CheckIsOn() => MyListView.IsEnabled = Frequency.IsEnabled = OptimizeNew.IsEnabled = SelectAllBox.IsEnabled = EnableTaskSwitch.IsOn;

    private async void CheckBox_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(50);
        CheckSelectAll();
    }

    public void CheckSelectAll()
    {
        var checkedItems = 0;
        foreach (var item in (List<CommonDriveListViewItem>)MyListView.ItemsSource)
        {
            if (item.IsChecked == true)
            {
                checkedItems++;
            }
        }
        if (checkedItems == ((List<CommonDriveListViewItem>)MyListView.ItemsSource).Count)
        {
            SelectAllBox.IsChecked = true;
            return;
        }
        else if (checkedItems == 0)
        {
            SelectAllBox.IsChecked = false;
            return;
        }
        else
        {
            SelectAllBox.IsChecked = null;
            return;
        }
    }

    private async void CheckBox_Checked_1(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(50);
        List<CommonDriveListViewItem> list = [];
        if (SelectAllBox.IsChecked == true)
        {
            foreach (var item in (List<CommonDriveListViewItem>)MyListView.ItemsSource)
            {
                list.Add(new()
                {
                    DrivePath = item.DrivePath,
                    ImagePath = item.ImagePath,
                    IsChecked = true,
                    MediaType = item.MediaType,
                    DriveName = item.DriveName,
                });
            }
        }
        else if (SelectAllBox.IsChecked == false)
        {
            foreach (var item in (List<CommonDriveListViewItem>)MyListView.ItemsSource)
            {
                list.Add(new()
                {
                    DrivePath = item.DrivePath,
                    ImagePath = item.ImagePath,
                    IsChecked = true,
                    MediaType = item.MediaType,
                    DriveName = item.DriveName,
                });
            }
        }
        MyListView.ItemsSource = list;
        CheckSelectAll();
    }
}
