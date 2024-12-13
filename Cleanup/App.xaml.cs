using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Cleanup;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var commandArgs = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

        m_window = new MainWindow(commandArgs);
        m_window.Activate();

        if (string.IsNullOrEmpty(commandArgs) != true)
        {
            await Task.Delay(100);
            await (m_window as MainWindow).ArgumentsLaunch(commandArgs[..2]);
        }
    }

    private Window m_window;
}
