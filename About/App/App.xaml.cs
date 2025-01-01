using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Helpers.Services;
using WinUIEx;

#nullable enable

namespace Rebound.About;

public partial class App : Application
{
    public static WindowEx? MainWindow { get; set; }

    private SingleInstanceAppService SingleInstanceAppService { get; set; }

    public App()
    {
        InitializeComponent();
        SingleInstanceAppService = new SingleInstanceAppService("ReboundAbout");
        SingleInstanceAppService.Launched += SingleInstanceApp_Launched;
        Current.UnhandledException += App_UnhandledException;
        AddTasksToTaskbar();
    }

    public static async void AddTasksToTaskbar()
    {
        // Get the app's jump list.
        var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();

        // Disable the system-managed jump list group.
        jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;

        // Remove any previously added custom jump list items.
        jumpList.Items.Clear();

        var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments("legacy", "Legacy winver");
        item.Logo = new Uri("ms-appx:///Assets/imageres_61.ico");

        jumpList.Items.Add(item);

        // Save the changes to the app's jump list.
        await jumpList.SaveAsync();
    }

    // Override default app launch
    protected override void OnLaunched(LaunchActivatedEventArgs args) => SingleInstanceAppService?.Launch(args.Arguments);

    // Handle any unhandled exceptions
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => e.Handled = true;

    private void SingleInstanceApp_Launched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.Arguments == "legacy")
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