using Microsoft.UI.Xaml.Media;
using Rebound.Helpers;
using WinUIEx;

#nullable enable

namespace Rebound.TrustedPlatform;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        this?.InitializeComponent();
        this?.SetWindowSize(1100, 750);
        _ = RootFrame.Navigate(typeof(MainPage));

        //TitleBarService.SetWindowIcon($"Assets\\icon.ico");

        MinWidth = 200;

        var rects = Display.GetDPIAwareDisplayRect(this);
        if (rects.Height < 900 || rects.Width < 1200)
        {
            this?.MoveAndResize(25, 25, Width - 150, rects.Height - 250);
        }

        TitleBarControl.InitializeForWindow(this, App.Current);
    }

    private void TitleBarControl_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarControl.SetWindowIcon($"Assets/icon.ico");
    }
}