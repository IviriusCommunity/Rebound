using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using WinUIEx;

namespace Rebound.About;

public sealed partial class MainWindow : WindowEx
{
    private readonly MainViewModel ViewModel;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        this.MoveAndResize(25, 25, 650, 690);
        SystemBackdrop = new MicaBackdrop();
        this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Rebound.ico");
    }

    [RelayCommand]
    private void CopyWindowsVersion() => CopyToClipboard(ViewModel.DetailedWindowsVersion);

    [RelayCommand]
    private void CopyLicenseOwners() => CopyToClipboard(ViewModel.LicenseOwners);

    [RelayCommand]
    private void CopyReboundVersion() => CopyToClipboard(Helpers.Environment.ReboundVersion.REBOUND_VERSION);

    [RelayCommand]
    private void CloseWindow() => Close();

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();
        package.SetText(content);
        Clipboard.SetContent(package);
    }
}