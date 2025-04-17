using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Generators;

namespace Rebound;

[ReboundApp("Rebound.Hub", "")]
public partial class App : Application
{
    private async void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            await LaunchWork();
        }
        else
        {
            if (MainAppWindow != null)
            {
                _ = ((MainWindow)MainAppWindow).BringToFront();
            }
            else
            {
                await LaunchWork();
            }
            return;
        }
    }

    public async Task LaunchWork()
    {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
    }
}