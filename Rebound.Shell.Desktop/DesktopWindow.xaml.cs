using WinUIEx;

#nullable enable

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopWindow : WindowEx
{
    public DesktopWindow()
    {
        InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
        this.SetWindowPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
        RootFrame.Navigate(typeof(DesktopPage));
    }
}