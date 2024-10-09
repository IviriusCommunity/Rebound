using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Graphics.Display;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Rebound.Helpers;
using Windows.Graphics;
using WinUIEx;
using WindowMessageMonitor = WinUIEx.Messaging.WindowMessageMonitor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.TrustedPlatform;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{
    public TitleBarService TitleBarService { get; set; }
    public MainWindow()
    {
        this.InitializeComponent();
        this.SetWindowSize(1100, 750);
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(MainPage));

        TitleBarService = new(this, AccentStrip, TitleBarIcon, WindowTitle, Close, CrimsonMaxRes, Minimize, MaxResGlyph, ContentGrid);
        TitleBarService.SetWindowIcon($"Assets\\icon.ico");

        this.MinHeight = 600;
        this.MinWidth = 500; 
        
        var rects = Display.GetDPIAwareDisplayRect(this);
        if (rects.Height < 900 || rects.Width < 1200)
        {
            this.MoveAndResize(25, 25, Width - 150, rects.Height - 250);
        }
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.m_window = null;
    }
}
