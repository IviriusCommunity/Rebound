using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Diagnostics;
using System.IO;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;
using WinRT;

namespace Rebound.ControlPanel.Views;

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
internal sealed partial class HomePage : Page
{
    internal HomeViewModel ViewModel { get; } = new();

    [ObservableProperty]
    private partial string UserPicturePath { get; set; }

    [ObservableProperty]
    private partial string WallpaperPath { get; set; }

    public HomePage()
    {
        InitializeComponent();
        UIThreadQueue.QueueAction(() =>
        {
            UserPicturePath = UserInformation.GetUserPicturePath() ?? string.Empty;
            WallpaperPath = UserInformation.GetWallpaperPath() ?? string.Empty;
        });
    }

    [RelayCommand]
    public unsafe void LaunchReboundHub()
    {
        try
        {
            var manager = new ApplicationActivationManager();
            manager.As<IApplicationActivationManager>().ActivateApplication("Rebound.Hub_rcz2tbwv5qzb8!App".ToPointer(), null, ACTIVATEOPTIONS.AO_NONE, null);
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void LaunchPath(string path)
    {
        try
        {
            Process.Start(path);
        }
        catch
        {

        }
    }

    private void WinverHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        Process.Start("winver.exe");
    }
}