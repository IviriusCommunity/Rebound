using System;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Rebound.Helpers;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;

namespace Rebound.About;

internal sealed partial class MainWindow : WindowEx
{
    private readonly MainViewModel ViewModel;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        this.Move(25, 25);
        this.SetDarkMode();
        this.RemoveIcon();
    }

    [RelayCommand]
    private void CopyWindowsVersion() => CopyToClipboard(ViewModel.DetailedWindowsVersion);

    [RelayCommand]
    private void CopyLicenseOwners() => CopyToClipboard(ViewModel.LicenseOwners);

    [RelayCommand]
    private static void CopyReboundVersion() => CopyToClipboard(Helpers.Environment.ReboundVersion.REBOUND_VERSION);

    [RelayCommand]
    private void CloseWindow() => Close();

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }
}