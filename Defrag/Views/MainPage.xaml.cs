using DependencyPropertyGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Rebound.Defrag.Helpers;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WinUIEx.Messaging;
using WinUIEx;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Rebound.Defrag.Controls;
using CommunityToolkit.Mvvm.Input;

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

    public async void OnShowAdvancedChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue("ViewAdvanced", newValue);
        ReloadListItems();
    }

    public async void ReloadListItems()
    {
        AreItemsEnabled = false;
        IsLoading = true;
        await Task.Delay(75);
        DriveItems = DriveHelper.GetDriveItems(ShowAdvanced);
        AreItemsEnabled = true;
        IsLoading = false;
    }

    private DeviceWatcher _deviceWatcher;

    public MainPage()
    {
        InitializeComponent();
        ShowAdvanced = SettingsHelper.GetValue<bool?>("ViewAdvanced") != null && SettingsHelper.GetValue<bool>("ViewAdvanced");
        DriveItems ??= DriveHelper.GetDriveItems(ShowAdvanced);
        // Begin monitoring window messages (such as device changes)

        // Subscribe to the WindowMessageReceived event
        //App.mon.WindowMessageReceived += MessageReceived;
        // Create a watcher for USB devices (or any other device class you need)
        var deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);

        // Handle device added
        deviceWatcher.Added += DeviceWatcher_Added;

        // Handle device removed
        deviceWatcher.Removed += DeviceWatcher_Removed;

        // Start watching
        deviceWatcher.Start();
        _deviceWatcher = deviceWatcher;

    }

    private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        // Device was plugged in
        DispatcherQueue.TryEnqueue(() =>
        {
            ReloadListItems();
            });
    }

    private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        // Device was removed
        DispatcherQueue.TryEnqueue(() =>
        {
            ReloadListItems();
        });
    }

    [RelayCommand]
    public void Optimize()
    {
        foreach (var item in DriveItems)
        {
            if (item.CanBeOptimized && item.IsChecked)
            {
                item.Optimize();
            }
        }
    }
}
