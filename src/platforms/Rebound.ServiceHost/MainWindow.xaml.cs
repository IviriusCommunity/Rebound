using Microsoft.UI.Xaml;
using WinUIEx;

namespace Rebound.ServiceHost;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        IsMaximizable = false;
        IsMinimizable = false;
        IsResizable = false;
        this.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
        this.SetWindowStyle(WindowStyle.Visible);
        this.MoveAndResize(0, 0, 0, 0);
        this.Minimize();
        this.SetWindowOpacity(0);
        SystemBackdrop = new TransparentTintBackdrop();
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args) => args.Handled = true;
}
