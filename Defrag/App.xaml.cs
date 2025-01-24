using Microsoft.UI.Xaml;
using Rebound.Helpers;
using WinUIEx;

#nullable enable

namespace Rebound.Defrag;

public partial class App : Application
{
    private readonly SingleInstanceDesktopApp _singleInstanceApp;

    public App()
    {
        this?.InitializeComponent();

        _singleInstanceApp = new SingleInstanceDesktopApp("Rebound.Defrag");
        _singleInstanceApp.Launched += OnSingleInstanceLaunched;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }

    private void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            LaunchWork();
        }
        else
        {
            if (MainAppWindow != null)
            {
                _ = ((MainWindow)MainAppWindow).BringToFront();
            }
            else
            {
                LaunchWork();
            }
            return;
        }
    }

    private static void LaunchWork()
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }

    public static WindowEx? MainAppWindow { get; set; }
}
