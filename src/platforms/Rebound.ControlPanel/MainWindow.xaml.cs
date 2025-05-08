using WinUIEx;

namespace Rebound.ControlPanel;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        RootFrame.Navigate(typeof(Views.MainPage));
    }
}
