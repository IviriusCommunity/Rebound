using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
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
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Rebound.ico");
        this.SetTaskBarIcon(Icon.FromFile(iconPath));
        this.ConfigureTitleBar();
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