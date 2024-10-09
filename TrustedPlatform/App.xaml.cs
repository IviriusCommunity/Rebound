using Rebound.Helpers;
using WinUIEx;

#nullable enable
#pragma warning disable CA2211 // Non-constant fields should not be visible

namespace Rebound.TrustedPlatform;

public partial class App : Application
{
    private readonly SingleInstanceDesktopApp _singleInstanceApp;

    public static WindowEx? MainAppWindow;

    public App()
    {
        this?.InitializeComponent();

        _singleInstanceApp = new SingleInstanceDesktopApp("Rebound.TPM");
        _singleInstanceApp.Launched += OnSingleInstanceLaunched;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _singleInstanceApp?.Launch(args.Arguments);
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
        MainAppWindow.Show();
    }
}