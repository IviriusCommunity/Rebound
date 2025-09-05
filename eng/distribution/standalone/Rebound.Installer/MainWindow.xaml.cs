using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Helpers.Windowing;
using Rebound.Installer;
using WinUIEx;

namespace ReboundHubInstaller;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "ReboundHubInstaller.ico"));
        this.CenterOnScreen();
        RootFrame.Navigate(typeof(MainPage));
        ExtendsContentIntoTitleBar = true;
    }

    /*public ObservableCollection<ReboundAppInstructions> Instructions { get; } =
    [
        new WinverInstructions(),
        new OnScreenKeyboardInstructions(),
        new DiskCleanupInstructions(),
        new UserAccountControlSettingsInstructions(),
        new ControlPanelInstructions(),
        new ShellInstructions()
    ];*/

    private void WindowEx_Closed(object sender, WindowEventArgs args) => Process.GetCurrentProcess().Kill();
}