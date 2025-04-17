using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Rebound.Generators;

namespace Rebound.About;

[ReboundApp("Rebound.About", new List<LegacyLaunchItem>() { new LegacyLaunchItem("Legacy winver", "legacy", "ms-appx:///Assets/Computer disk.png") })]
public partial class App : Application
{
    public App()
    {

    }

    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
    }
}