﻿using Microsoft.UI.Xaml;
using Rebound.Generators;

namespace ReboundHubInstaller;

[ReboundApp("Rebound.Installer", "")]
public partial class App : Application
{
    private void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            CreateMainWindow();
        }
        else
        {
            if (MainAppWindow != null)
            {
                _ = ((MainWindow)MainAppWindow).BringToFront();
            }
            else
            {
                CreateMainWindow();
            }
            return;
        }
    }

    public static void CreateMainWindow()
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }
}