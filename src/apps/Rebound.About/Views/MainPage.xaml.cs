// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.About.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Rebound.About.Views;

public partial class StringToUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            return new Uri(path);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

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

    [ObservableProperty]
    private partial string WallpaperPath { get; set; }

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
        UIThreadQueue.QueueAction(() =>
        {
            WallpaperPath = UserInformation.GetWallpaperPath() ?? string.Empty;
        });
    }

    [RelayCommand]
    public async Task OpenActivationSettingsAsync()
    {
        await Launcher.LaunchUriAsync(new("ms-settings:activation"));
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
    private void CopyLicenseOwners() => CopyToClipboard(WindowsInformation.GetLicenseOwners());

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

    private void Page_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
    {
        UpdateSize(e.NewSize);
    }

    public void UpdateSize(Windows.Foundation.Size size)
    {
        if (size.Width < 600 || size.Height < 800)
        {
            ViewModel.ShowExpandedView = false;
        }
        else
        {
            ViewModel.ShowExpandedView = true;
        }
    }

    private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        UpdateSize(new(ActualWidth, ActualHeight));
    }
}