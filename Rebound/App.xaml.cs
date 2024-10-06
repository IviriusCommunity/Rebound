using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.System.UserProfile;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound;
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
        this.UnhandledException += App_UnhandledException;
    }

    public string GetCurrentRegion()
    {
        // Get the current user's geographical region
        var geoRegion = GlobalizationPreferences.Languages.FirstOrDefault();

        // Optionally, you can use a more specific approach if needed
        // For example, checking the Region from the locale
        var region = geoRegion?.Split('-').LastOrDefault();

        return region ?? "Unknown";
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log or handle the exception
        Debug.WriteLine($"Unhandled exception: {e.Exception.Message}");
        e.Handled = true; // Prevent the application from terminating
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        /*var currentRegion = GetCurrentRegion();

        // List of restricted regions
        var restrictedRegions = new[] { "RU", "CN" };

        if (restrictedRegions.Contains(currentRegion))
        {
            // Show a message or close the app
            m_window = new RegionBlock();
            m_window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            m_window.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
            m_window.SystemBackdrop = new TransparentTintBackdrop();
            m_window.SetIsMinimizable(false);
            m_window.SetIsMaximizable(false);
            m_window.SetIsAlwaysOnTop(true);
            m_window.Activate();
            m_window.Maximize();
        }
        else
        {*/
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("CONTROL"))
        {
            var win = new ControlPanelWindow();
            cpanelWin = win;
            win.Show();
            win.CenterOnScreen();
            await Task.Delay(10);
            win.BringToFront();
            m_window = null;
            return;
        }
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("UAC"))
        {
            var win = new UACWindow();
            win.Show();
            await Task.Delay(10);
            win.BringToFront();
            m_window = null;
            return;
        }
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("UNINSTALL"))
        {
            var win = new UninstallationWindow();
            win.Show();
            await Task.Delay(10);
            win.BringToFront();
            m_window = null;
            return;
        }
        else
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
        //}
    }

    public static Window m_window;

    public static ControlPanelWindow cpanelWin;
}
