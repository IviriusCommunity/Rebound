using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace Rebound.Control;

public partial class App : Application
{
    public App()
    {
        this?.InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log or handle the exception
        Debug.WriteLine($"Unhandled exception: {e.Exception.Message}");
        e.Handled = true; // Prevent the application from terminating
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var win = new MainWindow();
        ControlPanelWindow = win;
        win.Activate();
    }

    public static MainWindow? ControlPanelWindow;
}
