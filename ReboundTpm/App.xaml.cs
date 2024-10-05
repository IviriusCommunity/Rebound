using WinUIEx;

namespace ReboundTpm;

public partial class App : Application
{
    public static WindowEx m_window;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.SetWindowSize(1100, 750);
        m_window.Show();
    }
}

