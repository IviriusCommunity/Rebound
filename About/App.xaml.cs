using Microsoft.UI.Xaml;
using WinUIEx;

namespace Rebound.About;

public partial class App : Application
{
    public static WindowEx MainWindow { get; set; }

    public App()
    {
        InitializeComponent();
        Current.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}