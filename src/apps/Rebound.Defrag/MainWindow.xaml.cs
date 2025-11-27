using Rebound.Defrag.Views;
using WinUIEx;

namespace Rebound.Defrag;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        this.CenterOnScreen();
        RootFrame.Navigate(typeof(MainPage));
    }
}