using System;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Rebound.Generators;

namespace Rebound;

[ReboundApp("Rebound.Hub", "")]
public partial class App : Application
{
    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            App.Current.UnhandledException += Current_UnhandledException;
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

    private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
    }

    public static void CreateMainWindow()
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }
}