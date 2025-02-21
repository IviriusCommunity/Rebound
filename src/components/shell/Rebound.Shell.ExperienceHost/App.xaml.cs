using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers.Services;
using Rebound.Shell.Desktop;
using WinUIEx;

namespace Rebound.Shell.ExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "Legacy Shell")]
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // Tray icon
        /*using System.Drawing.Icon icon = new($"{AppContext.BaseDirectory}\\Assets\\ReboundIcon.ico");
        var trayIcon = new TrayIcon
        {
            Icon = icon.Handle,
            ToolTip = "Rebound Shell"
        };
        trayIcon.Create();
        trayIcon.Show();*/

        // Desktop
        Run();
    }

    private async void Run()
    {
        DesktopWindow = new DesktopWindow();
        DesktopWindow.SetWindowOpacity(0);
        DesktopWindow.Activate();
        await Task.Delay(1000);
        DesktopWindow.AttachToProgMan();
    }

    private void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {

        }
    }

    public static WindowEx? RunWindow { get; set; }
    public static WindowEx? DesktopWindow { get; set; }
    public static WindowEx? ShutdownDialog { get; set; }
}
