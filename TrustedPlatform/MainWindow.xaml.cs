using Microsoft.UI.Xaml.Media;
using Rebound.Helpers;
using WinUIEx;

#nullable enable

namespace Rebound.TrustedPlatform;

public sealed partial class MainWindow : WindowEx
{
    public TitleBarService TitleBarService { get; set; }

    public MainWindow()
    {
        this?.InitializeComponent();
        this?.SetWindowSize(1100, 750);
        SystemBackdrop = new MicaBackdrop();
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(MainPage));

        TitleBarService = new(this, AccentStrip, TitleBarIcon, WindowTitle, Close, CrimsonMaxRes, Minimize, MaxResGlyph, ContentGrid);
        TitleBarService.SetWindowIcon($"Assets\\icon.ico");

        MinHeight = 600;
        MinWidth = 500; 
        
        var rects = Display.GetDPIAwareDisplayRect(this);
        if (rects.Height < 900 || rects.Width < 1200)
        {
            this?.MoveAndResize(25, 25, Width - 150, rects.Height - 250);
        }
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.MainAppWindow = null;
    }
}