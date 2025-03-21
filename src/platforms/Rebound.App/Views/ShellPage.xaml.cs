using Microsoft.UI.Xaml.Controls;

namespace Rebound.Views;

internal sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        NavigationViewControl.SelectedItem = HomeItem;
        MainFrame.Navigate(typeof(HomePage));
    }

    private void Navigate(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == HomeItem) MainFrame.Navigate(typeof(HomePage));
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == ReboundItem) MainFrame.Navigate(typeof(Rebound11Page));
    }
}