using WinUIEx;

namespace Rebound.Dialer;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        RootFrame.Navigate(typeof(Views.MainPage));
    }
}