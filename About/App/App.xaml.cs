using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Helpers.Services;
using WinUIEx;

#nullable enable

namespace Rebound.About;

public partial class App : Application
{
    public static WindowEx? MainWindow { get; set; }

    private SingleInstanceAppService SingleInstanceAppService { get; set; } = new SingleInstanceAppService("ReboundAbout");

    private ReboundAppService ReboundAppService { get; set; } = new ReboundAppService("Legacy winver");

    public App()
    {
        InitializeComponent();
        SingleInstanceAppService.Launched += SingleInstanceApp_Launched;
        Current.UnhandledException += App_UnhandledException;
    }

    // Override default app launch
    protected override void OnLaunched(LaunchActivatedEventArgs args) => SingleInstanceAppService?.Launch(args.Arguments);

    // Handle any unhandled exceptions
    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e) => e.Handled = true;

    private void SingleInstanceApp_Launched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.Arguments == ReboundAppService.LEGACY_LAUNCH)
        {
            _ = Process.Start("winver");
            Process.GetCurrentProcess().Kill();
            return;
        }

        if (e.IsFirstLaunch)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
        else
        {
            MainWindow?.BringToFront();
        }

        return;
    }
}