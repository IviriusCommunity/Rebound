using WinUIEx;
using MicaSystemBackdrop = WinUIEx.MicaSystemBackdrop;

namespace ReboundSysInfo;

public partial class App : Application
{
    public IThemeService ThemeService { get; set; }
    public IJsonNavigationViewService JsonNavigationViewService { get; set; }
    public new static App Current => (App)Application.Current;
    public string AppVersion { get; set; } = AssemblyInfoHelper.GetAssemblyVersion();
    public string AppName { get; set; } = "ReboundSysInfo";
    public App()
    {
        this.InitializeComponent();
        JsonNavigationViewService = new JsonNavigationViewService();
        JsonNavigationViewService.ConfigDefaultPage(typeof(HomeLandingPage));
        JsonNavigationViewService.ConfigSettingsPage(typeof(SettingsPage));
    }

    public static WindowEx m_window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new WindowEx();

        m_window.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        m_window.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;

        //CurrentWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        //CurrentWindow.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        if (m_window.Content is not Frame rootFrame)
        {
            m_window.Content = rootFrame = new Frame();
        }

        rootFrame.Navigate(typeof(MainPage));

        m_window.Title = $"{AppName} v{AppVersion}";
        m_window.SetIcon("Assets/icon.ico");

        m_window.Activate();
        //await DynamicLocalizerHelper.InitializeLocalizer("en-US");

        //UnhandledException += (s, e) => Logger?.Error(e.Exception, "UnhandledException");
    }
}

