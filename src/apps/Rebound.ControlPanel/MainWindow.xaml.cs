using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.ControlPanel.Views;
using Rebound.Helpers;
using Rebound.Helpers.Windowing;
using WinUIEx;

namespace Rebound.ControlPanel;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        RootFrame.Navigate(typeof(RootPage));
        this.SetWindowIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "ControlPanel.ico"));
    }

    public void InvokeWithArguments(string args)
    {
        (RootFrame.Content as RootPage).InvokeWithArguments(args);
    }

    private void WindowEx_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        LoadDragArea();
    }

    private void WindowEx_PositionChanged(object sender, Windows.Graphics.PointInt32 e)
    {
        LoadDragArea();
    }

    private void WindowEx_WindowStateChanged(object sender, WindowState e)
    {
        LoadDragArea();
    }

    private void LoadDragArea()
    {
        var dpi = this.GetDpiForWindow() / 96;
        AppWindow.TitleBar.SetDragRectangles([new((int)(48 * dpi), 0, (int)((AppWindow.Size.Width - 48) * dpi), (int)(32 * dpi))]);
    }
}