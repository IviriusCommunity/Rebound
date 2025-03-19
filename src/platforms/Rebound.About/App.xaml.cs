using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Core.SourceGeneratorAttributes;
using WinUIEx;

#nullable enable

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver")]
public partial class App : Application
{
    public static WindowEx? MainWindow { get; set; }

    //private SingleInstanceAppService SingleInstanceAppService { get; set; } = new SingleInstanceAppService("ReboundAbout");

    //private ReboundAppService ReboundAppService { get; set; } = new ReboundAppService("Legacy winver");

    private void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            LaunchWork();
        }
    }

    private void LaunchWork()
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    public App()
    {
        InitializeComponent();
        //SingleInstanceAppService.Launched += SingleInstanceApp_Launched;
    }

    // Override default app launch
    //protected override void OnLaunched(LaunchActivatedEventArgs args) => SingleInstanceAppService?.Launch(args.Arguments);

    /*private void SingleInstanceApp_Launched(object? sender, SingleInstanceLaunchEventArgs e)
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
    }*/
}