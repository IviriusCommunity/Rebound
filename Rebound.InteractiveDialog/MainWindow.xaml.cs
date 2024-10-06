using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.InteractiveDialog;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private AppWindow _apw;
    private OverlappedPresenter _presenter;

    public void GetAppWindowAndPresenter()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _apw = AppWindow.GetFromWindowId(myWndId);
        _presenter = _apw.Presenter as OverlappedPresenter;
    }

    public MainWindow()
    {
        this.InitializeComponent();

        GetAppWindowAndPresenter();
        _presenter.IsMaximizable = false;
        _presenter.IsMinimizable = false;
        this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(548, 184, 548, 184));
        this.SetIsMaximizable(false);
        this.SetIsResizable(false);
    }
}
