// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.About.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Rebound.About.Views;

[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{
    private InfoBarSeverity WindowsActivationSeverity
    {
        get
        {
            return WindowsInformation.GetWindowsActivationType() switch
            {
                WindowsActivationType.Unlicensed => InfoBarSeverity.Error,
                WindowsActivationType.Activated => InfoBarSeverity.Success,
                WindowsActivationType.GracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.NonGenuine => InfoBarSeverity.Error,
                WindowsActivationType.ExtendedGracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.Unknown => InfoBarSeverity.Informational,
                _ => InfoBarSeverity.Informational
            };
        }
    }

    [ObservableProperty]
    public partial BitmapImage UserPicture { get; set; }

    private static async Task<BitmapImage?> GetUserPictureAsync()
    {
        var picturePath = UserInformation.GetUserPicturePath();
        if (!string.IsNullOrEmpty(picturePath)) return new BitmapImage(new Uri(picturePath));
        else return null;
    }

    private MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        Load();
    }

    public async void Load()
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
        {
            UserPicture = await GetUserPictureAsync();
        });
    }

    [RelayCommand]
    private void CopyWindowsVersion() => CopyToClipboard(MainViewModel.DetailedWindowsVersion);

    [RelayCommand]
    private void CopyLicenseOwners() => CopyToClipboard(MainViewModel.LicenseOwners);

    [RelayCommand]
    private static void CopyReboundVersion() => CopyToClipboard(Variables.ReboundVersion);

    [RelayCommand]
    private void CloseWindow() => App.MainWindow?.Close();

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }
}