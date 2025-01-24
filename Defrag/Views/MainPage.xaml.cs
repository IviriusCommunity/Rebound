using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Defrag.Controls;
using Rebound.Defrag.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

#nullable enable

namespace Rebound.Defrag.Views;

[ObservableObject]
[DependencyProperty<bool>("ShowAdvanced", OnChanged = "OnShowAdvancedChanged")]
[DependencyProperty<List<DriveListViewItem>>("DriveItems")]

public sealed partial class MainPage : Page
{
    [ObservableProperty]
    public partial bool AreItemsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    public partial bool IsOptimizeEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsStopEnabled { get; set; } = true;

    public void OnShowAdvancedChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue("ViewAdvanced", newValue);
        ReloadListItems();
    }

    public async void ReloadListItems()
    {
        IsOptimizeEnabled = false;
        IsStopEnabled = false;
        AreItemsEnabled = false;
        IsLoading = true;
        await Task.Delay(75);
        DriveItems = DriveHelper.GetDriveItems(ShowAdvanced);
        AreItemsEnabled = true;
        IsLoading = false;
        CheckA();
    }

    public MainPage()
    {
        InitializeComponent();
        ShowAdvanced = SettingsHelper.GetValue<bool?>("ViewAdvanced") != null && SettingsHelper.GetValue<bool>("ViewAdvanced");
        DriveItems ??= DriveHelper.GetDriveItems(ShowAdvanced);

        // Begin monitoring window messages (such as device changes)
        var deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);

        // Handle device added
        deviceWatcher.Added += DeviceWatcher_Added;

        // Handle device removed
        deviceWatcher.Removed += DeviceWatcher_Removed;

        // Start watching
        deviceWatcher.Start();
    }

    private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        // Device was plugged in
        DispatcherQueue.TryEnqueue(ReloadListItems);
    }

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // Device was removed
        DispatcherQueue.TryEnqueue(ReloadListItems);
    }

    [RelayCommand]
    public async Task Optimize()
    {
        if (DriveItems is null)
        {
            return;
        }

        // Disable optimize button and enable stop button if there are items to optimize
        var hasOptimizableItems = DriveItems.Any(item => item.CanBeOptimized && item.IsChecked);
        IsOptimizeEnabled = !hasOptimizableItems;
        IsStopEnabled = hasOptimizableItems;

        // Collect optimization tasks
        var optimizationTasks = DriveItems
            .Where(item => item.CanBeOptimized && item.IsChecked)
            .Select(item => item.Optimize())
            .ToList();

        // Await all optimization tasks to complete
        await Task.WhenAll(optimizationTasks);
        CheckA();
    }

    [RelayCommand]
    public void Stop()
    {
        if (DriveItems is null)
        {
            return;
        }

        foreach (var item in DriveItems.Where(item => item.IsChecked && item.PowerShellProcess != null))
        {
            item.Cancel();
        }
    }

    private void ItemCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        CheckA();
    }

    private async void CheckA()
    {
        await Task.Delay(10); // Small delay to allow UI updates

        if (DriveItems is null)
        {
            IsOptimizeEnabled = false;
            IsStopEnabled = false;
            return;
        }

        var canOptimize = true;
        var canStop = true;
        var selectedItems = 0;

        foreach (var item in DriveItems)
        {
            if (item.IsChecked)
            {
                selectedItems++;
                if (!item.CanBeOptimized || item.PowerShellProcess != null)
                {
                    canOptimize = false;
                }

                if (!item.CanBeOptimized || item.PowerShellProcess == null)
                {
                    canStop = false;
                }
            }
        }

        if (selectedItems == 0)
        {
            canOptimize = false;
            canStop = false;
        }

        IsOptimizeEnabled = canOptimize;
        IsStopEnabled = canStop;
    }
}
