using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private AppWindow _apw = null!;
    private OverlappedPresenter _presenter = null!;

    public void GetAppWindowAndPresenter()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _apw = AppWindow.GetFromWindowId(wndId);
        _presenter = _apw.Presenter as OverlappedPresenter ?? throw new InvalidOperationException("Presenter is not of type OverlappedPresenter");
    }

    public MainWindow()
    {
        this.InitializeComponent();

        GetAppWindowAndPresenter();
        _presenter.IsMaximizable = false;
        _presenter.IsMinimizable = false;
        this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(328, 198, 328, 198));
        this.SetIsMinimizable(false);
        this.SetIsMaximizable(false);
        this.SetIsResizable(false);
        this.SetIsAlwaysOnTop(true);

        Window window = this;
        window.ExtendsContentIntoTitleBar = true; // Enable custom titlebar
        window.SetTitleBar(AppTitleBar); // Set titlebar as <Border /> from MainWindow.xaml
    }

    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        await ShowDialog();
    }

    public async Task ShowDialog()
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = RootGrid.XamlRoot,
            Title = "Hello",
            Content = "Hello, World!",
            CloseButtonText = "Close"
        };
        await dialog.ShowAsync();
    }

}
