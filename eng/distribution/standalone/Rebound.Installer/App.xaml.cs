using Microsoft.UI.Xaml;
using Rebound.Core.UI;
using Rebound.Generators;
using Rebound.Installer;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ReboundHubInstaller;

[ReboundApp("Rebound.Installer", "")]
public partial class App : Application
{
    private async void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            UIThreadQueue.QueueAction(CreateMainWindow);
        }
        else
        {
            return;
        }
    }

    public static unsafe void CreateMainWindow()
    {
        // Create the window
        MainWindow = new()
        {
            IsPersistenceEnabled = false,
            PersistenceKey = "Rebound.Installer.MainWindow",
            Width = 720,
            Height = 600,
        };

        // AppWindow init
        MainWindow.AppWindowInitialized += (s, e) =>
        {
            MainWindow.Title = "Rebound Installer";

            // Window properties
            MainWindow.IsMaximizable = false;
            MainWindow.IsResizable = false;
            MainWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            MainWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
            MainWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
            MainWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            MainWindow.AppWindow?.SetTaskbarIcon($"{AppContext.BaseDirectory}\\Assets\\AboutWindows.ico");

            MainWindow.CenterWindow();
        };

        // Load main page
        MainWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(MainPage));
            MainWindow.Content = frame;
        };

        // Spawn the window
        MainWindow.Create();
        MainWindow.CenterWindow();
    }

    public static IslandsWindow? MainWindow { get; set; }
}