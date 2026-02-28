// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Rebound.About.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#pragma warning disable CA1031

namespace Rebound.About.Views;

internal sealed partial class MainPage : Page
{
    [GeneratedDependencyProperty(DefaultValue = InfoBarSeverity.Informational)] public partial InfoBarSeverity WindowsActivationSeverity { get; set; }

    // The \\\\ is a workaround for this thing: https://github.com/CommunityToolkit/Labs-Windows/issues/788
    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string UserPicturePath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string WallpaperPath { get; set; }

    private MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        // Run upon loading
        Loaded += (s, e) =>
        {
            // Populate fields with data
            WindowsActivationSeverity = WindowsInformation.GetWindowsActivationType() switch
            {
                WindowsActivationType.Unlicensed => InfoBarSeverity.Error,
                WindowsActivationType.Activated => InfoBarSeverity.Success,
                WindowsActivationType.GracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.NonGenuine => InfoBarSeverity.Error,
                WindowsActivationType.ExtendedGracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.Unknown => InfoBarSeverity.Informational,
                _ => InfoBarSeverity.Informational
            };
            WallpaperPath = UserInformation.GetWallpaperPath() ?? string.Empty;
            UserPicturePath = UserInformation.GetUserPicturePath() ?? string.Empty;

            // Dynamic view code
            SizeChanged += (s, e) => { UpdateSize(e.NewSize); };
            UpdateSize(new(ActualWidth, ActualHeight));
        };

        InitializeComponent();
    }

    private void UpdateSize(Windows.Foundation.Size size) => ViewModel.ShowExpandedView = !(size.Width < 600 || size.Height < 800);

    [RelayCommand] private static void CopyWindowsVersion() 
        => CopyToClipboard(WindowsInformation.GetDetailedWindowsVersion());

    [RelayCommand] private static void CopyFirstFour()
        => CopyToClipboard($"{WindowsInformation.GetDetailedWindowsVersion()}\n{WindowsInformation.GetOSName()}\n{WindowsInformation.GetInstalledOnDateString()}\n{WindowsInformation.GetComputerName()}");

    [RelayCommand] private static void CopyLicenseOwners()
        => CopyToClipboard(WindowsInformation.GetLicenseOwners());

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
    private static async Task ViewMoreAsync()
    {
        try
        {
            Process.Start("msinfo32.exe");
        }
        catch (Exception ex)
        {
            await ReboundDialog.ShowAsync(
                "Rebound About",
                "Couldn't launch System Information.",
                $"Something went wrong and we couldn't launch System Information. ({ex.Message})",
                null,
                DialogIcon.Error
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

    [RelayCommand] private static void CloseWindow()
        => App.MainWindow?.Close();
}