// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.ControlPanel.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AboutWindowsPage : Page
{
    [GeneratedDependencyProperty(DefaultValue = InfoBarSeverity.Informational)] public partial InfoBarSeverity WindowsActivationSeverity { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "ms-appx:///")] public partial string? CustomLogoPath { get; set; }

    AboutWindowsViewModel ViewModel { get; } = new();

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public AboutWindowsPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        // Fast init — these are probably cheap, keep on UI thread
        ViewModel.InitializePrimarySoftware();
        ViewModel.InitializePrimaryHardware();

        // All heavy work off UI thread
        _ = LoadDeferredAsync();

        ViewModel._listener.SettingChanged += (s, e) =>
        {
            _dispatcherQueue.EnqueueAsync(() =>
            {
                UpdateCustomBranding();
            });
        };

        UpdateCustomBranding();
    }

    private void UpdateCustomBranding()
    {
        if (SettingsManager.GetValue<string?>("CustomBrandingPath", "rebound") is string path && !string.IsNullOrWhiteSpace(path))
        {
            CustomLogoPath = path;
            ViewModel.ShowCustomBranding = true;
        }
        else
        {
            CustomLogoPath = "ms-appx:///Assets/ControlPanel.ico";
            ViewModel.ShowCustomBranding = false;
        }
    }

    private async Task LoadDeferredAsync()
    {
        // Capture everything on background thread
        var results = await Task.Run(() =>
        {
            // Software
            var activationType = WindowsInformation.GetWindowsActivationType();
            var activationInfo = activationType switch
            {
                WindowsActivationType.Unlicensed => "Unlicensed",
                WindowsActivationType.Activated => "Activated",
                WindowsActivationType.GracePeriod => "Grace period",
                WindowsActivationType.NonGenuine => "Non-genuine",
                WindowsActivationType.ExtendedGracePeriod => "Extended grace period",
                _ => "Unknown"
            };

            // Hardware
            var cpuName = CPU.GetName();
            var cpuArch = CPU.GetArchitecture();
            var gpuName = GPU.GetName();
            var pagefileSize = RAM.GetPageFileSize();
            var winSpace = Storage.GetWindowsDriveOccupiedSpacePercentage();
            var totalSpace = Storage.GetTotalOccupiedSpacePercentage();
            var manufacturer = Device.GetDeviceManufacturer();
            var model = Device.GetDeviceModel();
            var motherboard = Device.GetMotherboardModel();

            // Activation severity
            var severity = activationType switch
            {
                WindowsActivationType.Unlicensed => InfoBarSeverity.Error,
                WindowsActivationType.Activated => InfoBarSeverity.Success,
                WindowsActivationType.GracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.NonGenuine => InfoBarSeverity.Error,
                WindowsActivationType.ExtendedGracePeriod => InfoBarSeverity.Warning,
                _ => InfoBarSeverity.Informational
            };

            return new
            {
                activationInfo,
                cpuName,
                cpuArch,
                gpuName,
                pagefileSize,
                winSpace,
                totalSpace,
                manufacturer,
                model,
                motherboard,
                severity,
            };
        }).ConfigureAwait(true);

        // Marshal all results back to UI thread in one shot
        ViewModel.WindowsActivationInfo = results.activationInfo;
        ViewModel.CpuName = results.cpuName;
        ViewModel.CpuArchitecture = results.cpuArch;
        ViewModel.GpuName = results.gpuName;
        ViewModel.PagefileSize = results.pagefileSize;
        ViewModel.WindowsOccupiedSpace = results.winSpace;
        ViewModel.WindowsOccupiedSpaceString = ((int)results.winSpace).ToString((IFormatProvider?)null);
        ViewModel.TotalOccupiedSpace = results.totalSpace;
        ViewModel.TotalOccupiedSpaceString = ((int)results.totalSpace).ToString((IFormatProvider?)null);
        ViewModel.DeviceManufacturer = results.manufacturer;
        ViewModel.DeviceModel = results.model;
        ViewModel.MotherboardModel = results.motherboard;
        WindowsActivationSeverity = results.severity;
    }
}
