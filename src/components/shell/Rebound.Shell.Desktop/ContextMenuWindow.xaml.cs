using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class ContextMenuWindow : WindowEx
{
    DesktopWindow desktopWindow;

    public ContextMenuWindow(DesktopWindow win)
    {
        desktopWindow = win;
        this.InitializeComponent();
        this.SystemBackdrop = new TransparentTintBackdrop();
        this.ExtendsContentIntoTitleBar = true;
        this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
        this.MoveAndResize(0, 0, 0, 0);
        this.ToggleWindowStyle(false,
            WindowStyle.SizeBox | WindowStyle.Caption |
            WindowStyle.MaximizeBox | WindowStyle.MinimizeBox |
            WindowStyle.Border | WindowStyle.Iconic |
            WindowStyle.SysMenu);
        this.ToggleExtendedWindowStyle(false, ExtendedWindowStyle.AppWindow);
    }

    public void ShowContextMenu(Point pos)
    {
        this.MoveAndResize(pos.X, pos.Y - 36, 0, 0);
        this.BringToFront();
        Menu.ShowAt(StartPoint, new FlyoutShowOptions()
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
        });
    }

    bool canClose = false;

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        canClose = true;
        this.Close();
        desktopWindow.RestoreExplorerDesktop();
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        args.Handled = !canClose;
    }

    private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
    {
        canClose = true;
        this.Close();
        desktopWindow.RestoreExplorerDesktop();
        await Task.Delay(250);
        Process.GetCurrentProcess().Kill();
    }
}
