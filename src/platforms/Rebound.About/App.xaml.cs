using Microsoft.UI.Xaml;
using Rebound.Generators;

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver")]
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