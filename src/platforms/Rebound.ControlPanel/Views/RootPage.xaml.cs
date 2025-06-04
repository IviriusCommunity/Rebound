using System.Xml.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

public sealed partial class RootPage : Page
{
    public RootPage()
    {
        InitializeComponent();
        AddTab();
    }

    [RelayCommand]
    public void AddTab()
    {
        var frame = new Frame();
        frame.Navigate(typeof(MainPage));
        RootTabView.TabItems.Add(new TabViewItem()
        {
            Content = frame,
            Header = "Home",
            IconSource = new FontIconSource()
            {
                Glyph = "\uE80F"
            }
        });
        RootTabView.SelectedIndex = RootTabView.TabItems.Count - 1;
    }

    public void InvokeWithArguments(string args)
    {
        if (args == @"/name Microsoft.AdministrativeTools")
        {
            // Placeholder
        }
    }

    private void RootTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        RootTabView.TabItems.Remove(args.Tab);
    }

    private void RootTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        UpdateDragRegion();
        if (RootTabView.TabItems.Count is 0)
        {
            App.MainAppWindow.Close();
        }
    }

    public void UpdateDragRegion()
    {
        var visual = DragGrid.TransformToVisual(null);
        App.MainAppWindow.AppWindow.TitleBar.SetDragRectangles(new Windows.Graphics.RectInt32[]
        {
            new((int)visual.TransformPoint(new(0, 0)).X, (int)visual.TransformPoint(new(0, 0)).Y, (int)DragGrid.ActualWidth, (int)DragGrid.ActualHeight)
        });
    }
}