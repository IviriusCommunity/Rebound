using System;
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
        UpdateDragRegion();
    }

    private void WindowEx_PositionChanged(object sender, Windows.Graphics.PointInt32 e)
    {
        UpdateDragRegion();
    }

    private async void WindowEx_WindowStateChanged(object sender, WindowState e)
    {
        await Task.Delay(100).ConfigureAwait(true);
        UpdateDragRegion();
    }

    private void UpdateDragRegion()
    {
        if (RootFrame.Content is RootPage rootPage)
        {
            rootPage.UpdateDragRegion();
        }
    }
}