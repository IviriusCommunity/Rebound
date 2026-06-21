// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.About.ViewModels;
using Rebound.Core;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.Threading;
using Rebound.Core.UI.Localizer;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

#pragma warning disable CA1031

namespace Rebound.About.Views;

internal sealed partial class MainPage : Page
{
    [GeneratedDependencyProperty(DefaultValue = InfoBarSeverity.Informational)] public partial InfoBarSeverity WindowsActivationSeverity { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "ms-appx:///")] public partial string? UserPicturePath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "ms-appx:///")] public partial string? WallpaperPath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "ms-appx:///")] public partial string? CustomLogoPath { get; set; }

    private MainViewModel ViewModel { get; } = new();

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public MainPage()
    {
        InitializeComponent();
        SizeChanged += (s, e) => { UpdateSize(e.NewSize); };
        UpdateSize(new(ActualWidth, ActualHeight));
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        // This needs a STA thread for some reason (Shell COM moment)
        var (wallpaper, userPicture) = await STAThread.RunOnSTAThread(() =>
        {
            string w = string.Empty, u = string.Empty;
            try { w = UserInformation.GetWallpaperPath()!; } catch { }
            try { u = UserInformation.GetUserPicturePath(); } catch { }
            return (w, u);
        }).ConfigureAwait(true);

        WallpaperPath = wallpaper;
        UserPicturePath = userPicture;

        // Fast init — these are probably cheap, keep on UI thread
        ViewModel.InitializePrimarySoftware();
        ViewModel.InitializePrimaryHardware();
        ViewModel.InitializePrimaryUser();

        ViewModel.Loaded = true;

        // All heavy work off UI thread
        _ = LoadDeferredAsync();

        ViewModel._liveHardwareFeed.OnUpdate += LiveHardwareFeed_OnUpdate;
        ViewModel._liveHardwareFeed.Start();

        ViewModel._listener.SettingChanged += (s, e) =>
        {
            _dispatcherQueue.EnqueueAsync(() =>
            {
                ViewModel.UpdateSettings();
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
            CustomLogoPath = "ms-appx:///Assets/AboutWindows.ico";
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
            var activationInfo = LocalizedResource.GetLocalizedString(activationType switch
            {
                WindowsActivationType.Unlicensed => "ActivationStatusUnlicensed",
                WindowsActivationType.Activated => "ActivationStatusActivated",
                WindowsActivationType.GracePeriod => "ActivationStatusGracePeriod",
                WindowsActivationType.NonGenuine => "ActivationStatusNonGenuine",
                WindowsActivationType.ExtendedGracePeriod => "ActivationStatusExtendedGracePeriod",
                _ => "ActivationStatusUnknown"
            });
            var installedOn = WindowsInformation.GetInstalledOnDate().ToString((IFormatProvider?)null);
            var locale = WindowsInformation.GetLocale();
            var localIP = WindowsInformation.GetLocalIP();

            // Hardware
            var cpuName = CPU.GetName();
            var cpuArch = CPU.GetArchitecture();
            var gpuName = GPU.GetName();
            var pagefileSize = RAM.GetPageFileSize();
            var res = Display.GetDisplayResolution();
            var refreshRate = Display.GetDisplayRefreshRate();
            var winSpace = Storage.GetWindowsDriveOccupiedSpacePercentage();
            var totalSpace = Storage.GetTotalOccupiedSpacePercentage();
            var manufacturer = Device.GetDeviceManufacturer();
            var model = Device.GetDeviceModel();
            var motherboard = Device.GetMotherboardModel();

            // User
            var isMsAccount = UserInformation.IsMicrosoftAccount();
            var licenseOwners = WindowsInformation.GetLicenseOwners();
            var passwordExpiry = UserInformation.GetPasswordExpiry();

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

            // Scale
            string? scale;
            unsafe
            {
                scale = (Display.GetScale(new((void*)Process.GetCurrentProcess().MainWindowHandle)) * 100).ToString((IFormatProvider?)null) + "%";
            }

            return new
            {
                activationInfo,
                installedOn,
                locale,
                localIP,
                cpuName,
                cpuArch,
                gpuName,
                pagefileSize,
                res,
                refreshRate,
                winSpace,
                totalSpace,
                manufacturer,
                model,
                motherboard,
                isMsAccount,
                licenseOwners,
                passwordExpiry,
                severity,
                scale
            };
        }).ConfigureAwait(true);

        // Marshal all results back to UI thread in one shot
        ViewModel.WindowsActivationInfo = results.activationInfo;
        ViewModel.InstalledOnDate = results.installedOn;
        ViewModel.Locale = results.locale;
        ViewModel.LocalIP = results.localIP;
        ViewModel.CpuName = results.cpuName;
        ViewModel.CpuArchitecture = results.cpuArch;
        ViewModel.GpuName = results.gpuName;
        ViewModel.PagefileSize = results.pagefileSize;
        ViewModel.DisplayResolution = $"{results.res.cx}x{results.res.cy}";
        ViewModel.DisplayRefreshRate = $"{results.refreshRate} Hz";
        ViewModel.WindowsOccupiedSpace = results.winSpace;
        ViewModel.WindowsOccupiedSpaceString = ((int)results.winSpace).ToString((IFormatProvider?)null);
        ViewModel.TotalOccupiedSpace = results.totalSpace;
        ViewModel.TotalOccupiedSpaceString = ((int)results.totalSpace).ToString((IFormatProvider?)null);
        ViewModel.DeviceManufacturer = results.manufacturer;
        ViewModel.DeviceModel = results.model;
        ViewModel.MotherboardModel = results.motherboard;
        ViewModel.IsMicrosoftAccount = results.isMsAccount;
        ViewModel.LicenseOwners = results.licenseOwners;
        ViewModel.PasswordExpiryDate = results.passwordExpiry;
        WindowsActivationSeverity = results.severity;
        ViewModel.Scale = results.scale;
    }

    private void LiveHardwareFeed_OnUpdate(object? sender, HardwareFeedUpdateEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.CpuUsage = e.CpuUsage;
            ViewModel.RamUsage = e.RamUsagePercent;
            ViewModel.GpuUsage = e.GpuUsage;
            ViewModel.Uptime = $"{(int)e.Uptime.TotalDays}d {e.Uptime.Hours}h {e.Uptime.Minutes}m";
        });
    }

    private void UpdateSize(Windows.Foundation.Size size) => ViewModel.ShowExpandedView = !(size.Width < 680 || size.Height < 680);

    [RelayCommand] private void CopyWindowsVersion() 
        => CopyToClipboard(ViewModel.DetailedWindowsVersion);

    [RelayCommand] private void CopyFirstFour()
        => CopyToClipboard($"{WindowsInformation.GetDetailedWindowsVersion()}\n{ViewModel.OSName}\n{ViewModel.InstalledOnDate}\n{ViewModel.ComputerName}");

    [RelayCommand] private void CopyLicenseOwners()
        => CopyToClipboard(ViewModel.LicenseOwners);

    [RelayCommand] private static void CopyReboundVersion()
        => CopyToClipboard(Variables.ReboundVersion);

    [RelayCommand] public static async Task OpenUserFolderAsync()
        => await Launcher.LaunchFolderPathAsync(UserInformation.GetUserFolder());

    [RelayCommand]
    private static void LegacyLaunch()
    { 
        if (Application.Current is App app)
            app.LaunchLegacy(string.Empty);
    }

    [RelayCommand]
    private static async Task OpenWinActivation()
    {
        var uri = new Uri("ms-settings:activation");
        if (!await Launcher.LaunchUriAsync(uri))
        {
            await App.MainWindow!.ShowMessageDialogAsync(
                "Couldn't open Activation settings.",
                "Something went wrong and we couldn't open the Activation settings page."
                ).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private static async Task ViewMoreAsync()
    {
        try
        {
            Process.Start("msinfo32.exe");
        }
        catch (Exception ex)
        {
            await App.MainWindow!.ShowMessageDialogAsync(
                "Couldn't launch System Information.",
                $"Something went wrong and we couldn't launch System Information. ({ex.Message})"
                ).ConfigureAwait(false);
        }
    }

    // Boilerplate code
    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    private static void CloseWindow()
        => App.MainWindow?.Close();
}