using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Defrag.Controls;
using Rebound.Defrag.Helpers;
using Rebound.Defrag.ViewModels;
using Rebound.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

#nullable enable

namespace Rebound.Defrag.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        InitializeComponent();
        ViewModel.ShowAdvanced = SettingsHelper.GetValue<bool?>("ViewAdvanced", "dfrgui") != null && SettingsHelper.GetValue<bool>("ViewAdvanced", "dfrgui");
        ViewModel.DriveItems ??= DriveHelper.GetDriveItems(ViewModel.ShowAdvanced);

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
        DispatcherQueue.TryEnqueue(ViewModel.ReloadListItems);
    }

    private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // Device was removed
        DispatcherQueue.TryEnqueue(ViewModel.ReloadListItems);
    }

    [RelayCommand]
    public async Task Optimize()
    {
        if (ViewModel.DriveItems is null)
        {
            return;
        }

        // Disable optimize button and enable stop button if there are items to optimize
        var hasOptimizableItems = ViewModel.DriveItems.Any(item => item.CanBeOptimized && item.IsChecked);
        ViewModel.IsOptimizeEnabled = !hasOptimizableItems;
        ViewModel.IsStopEnabled = hasOptimizableItems;

        // Collect optimization tasks
        var optimizationTasks = ViewModel.DriveItems
            .Where(item => item.CanBeOptimized && item.IsChecked)
            .Select(item => item.Optimize())
            .ToList();

        // Await all optimization tasks to complete
        await Task.WhenAll(optimizationTasks);
        ViewModel.CheckA();
    }

    [RelayCommand]
    public void Stop()
    {
        if (ViewModel.DriveItems is null)
        {
            return;
        }

        foreach (var item in ViewModel.DriveItems.Where(item => item.IsChecked && item.PowerShellProcess != null))
        {
            item.Cancel();
        }
    }

    private void ItemCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        ViewModel.CheckA();
    }
}
