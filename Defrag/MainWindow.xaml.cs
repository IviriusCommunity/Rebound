using System;
using Microsoft.UI.Xaml;
using Rebound.Defrag.Views;
using WinUIEx;

#nullable enable

namespace Rebound.Defrag;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        this?.InitializeComponent();

        // Initialize the title bar
        TitleBarControl.InitializeForWindow(this, App.Current);

        // Window customization
        this.SetWindowSize(800, 550);
        this.CenterOnScreen();

        RootFrame.Navigate(typeof(MainPage));
    }

    private void TitleBarControl_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarControl.SetWindowIcon(@$"{AppContext.BaseDirectory}/Assets/Rebound.Defrag.ico");
    }
}