using System.Net.Http;
using Microsoft.UI.Xaml.Controls;

namespace Rebound.Views;

internal sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        NavigationViewControl.SelectedItem = HomeItem;
        MainFrame.Navigate(typeof(HomePage));
        CheckForUpdates();
    }

    public async void CheckForUpdates()
    {
        using var client = new HttpClient();
        var url = "https://ivirius.com/reboundhubversion.txt";
        var webContent = await client.GetStringAsync(url);

        if (Helpers.Environment.ReboundVersion.REBOUND_VERSION != webContent)
        {
            UpdateButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private void Navigate(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == HomeItem) MainFrame.Navigate(typeof(HomePage));
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == ReboundItem) MainFrame.Navigate(typeof(Rebound11Page));
    }
}