using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32.TaskScheduler;
using Rebound.Defrag.Helpers;
using WinUIEx;
using static Rebound.Defrag.MainWindow;
using Task = System.Threading.Tasks.Task;

#nullable enable

namespace Rebound.Defrag;

public sealed partial class ScheduledOptimization : WindowEx
{
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
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        LoadData();
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
            foreach (var disk in (List<DiskItem>)MyListView.ItemsSource)
            {
                var letter = disk.DriveLetter != null && disk.DriveLetter.EndsWith('\\') ? disk.DriveLetter[..^1] : disk.DriveLetter;
                disk.IsChecked = letter == null || !GetTaskCommand().Contains(letter);
            }
        }
        else
        {
            OptimizeNew.IsChecked = false;
            foreach (var disk in (List<DiskItem>)MyListView.ItemsSource)
            {
                var letter = disk.DriveLetter != null && disk.DriveLetter.EndsWith('\\') ? disk.DriveLetter[..^1] : disk.DriveLetter;
                disk.IsChecked = letter != null && GetTaskCommand().Contains(letter);
            }
        }
        CheckSelectAll();
    }

    public static void ScheduleDefragTask(List<DiskItem> items, bool optimizeNewDrives, string scheduleFrequency)
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
                    var letter = disk.DriveLetter != null && disk.DriveLetter.EndsWith('\\') ? disk.DriveLetter[..^1] : disk.DriveLetter;
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
                    var letter = disk.DriveLetter != null && disk.DriveLetter.EndsWith('\\') ? disk.DriveLetter[..^1] : disk.DriveLetter;
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

    public async void LoadData()
    {
        // Initial delay
        // Essential for ensuring the UI loads before starting tasks
        await Task.Delay(100);

        List<DiskItem> items = [];

        // Get the logical drives bitmask
        var drivesBitMask = Win32Helper.GetLogicalDrives();
        if (drivesBitMask == 0)
        {
            return;
        }

        for (var driveLetter = 'A'; driveLetter <= 'Z'; driveLetter++)
        {
            var mask = 1u << (driveLetter - 'A');
            if ((drivesBitMask & mask) != 0)
            {
                var drive = $"{driveLetter}:\\";

                StringBuilder volumeName = new(261);
                StringBuilder fileSystemName = new(261);
                if (Win32Helper.GetVolumeInformation(drive, volumeName, volumeName.Capacity, out _, out _, out _, fileSystemName, fileSystemName.Capacity))
                {
                    var newDriveLetter = drive.ToString().Remove(2, 1);
                    var mediaType = GetDriveTypeDescriptionAsync(drive);

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
                        items.Add(item);
                    }
                }
                else
                {
                    Debug.WriteLine($"  Failed to get volume information for {drive}");
                }
            }
        }

        int selIndex = MyListView.SelectedIndex is not -1 ? MyListView.SelectedIndex : 0;

        // Set the list view's item source
        MyListView.ItemsSource = items;

        MyListView.SelectedIndex = selIndex >= items.Count ? items.Count - 1 : selIndex;
    }

    private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OptimizeNew.IsChecked ??= true;

        var frequencyItem = Frequency.SelectedItem as ComboBoxItem;
        var frequencyContent = frequencyItem?.Content?.ToString() ?? string.Empty;

        if (EnableTaskSwitch.IsOn == true)
        {
            ScheduleDefragTask((List<DiskItem>?)MyListView.ItemsSource ?? [], OptimizeNew.IsChecked ?? false, frequencyContent);
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
        foreach (var item in (List<DiskItem>)MyListView.ItemsSource)
        {
            if (item.IsChecked == true)
            {
                checkedItems++;
            }
        }
        if (checkedItems == ((List<DiskItem>)MyListView.ItemsSource).Count)
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
        List<DiskItem> list = [];
        if (SelectAllBox.IsChecked == true)
        {
            foreach (var item in (List<DiskItem>)MyListView.ItemsSource)
            {
                list.Add(new()
                {
                    DriveLetter = item.DriveLetter,
                    ImagePath = item.ImagePath,
                    IsChecked = true,
                    MediaType = item.MediaType,
                    Name = item.Name,
                    ProgressValue = item.ProgressValue,
                });
            }
        }
        else if (SelectAllBox.IsChecked == false)
        {
            foreach (var item in (List<DiskItem>)MyListView.ItemsSource)
            {
                list.Add(new()
                {
                    DriveLetter = item.DriveLetter,
                    ImagePath = item.ImagePath,
                    IsChecked = false,
                    MediaType = item.MediaType,
                    Name = item.Name,
                    ProgressValue = item.ProgressValue,
                });
            }
        }
        MyListView.ItemsSource = list;
        CheckSelectAll();
    }
}
