using System.Net.Http;
using CommunityToolkit.WinUI.Helpers;
using Windows.UI.Xaml.Controls;
using Rebound.Helpers;

namespace Rebound.Views;

internal sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        NavigationViewControl.SelectedItem = HomeItem;
        if (SettingsHelper.GetValue("ShowBranding", "rebound", true))
        {
            MainFrame.Navigate(typeof(HomePage));
        }
        else
        {
            NavigationViewControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            OverlayFrame.Visibility = Windows.UI.Xaml.Visibility.Visible;
            OverlayFrame.Navigate(typeof(ReboundPage));
        }
        CheckForUpdates();
    }

    public async void CheckForUpdates()
    {
        try
        {
            if (NetworkHelper.Instance.ConnectionInformation.ConnectionType != ConnectionType.Unknown)
            {
                using var client = new HttpClient();
                var url = "https://ivirius.com/reboundhubversion.txt";
                var webContent = await client.GetStringAsync(url);

                if (Helpers.Environment.ReboundVersion.REBOUND_VERSION != webContent)
                {
                    UpdateButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
        }
        catch
        {

        }
    }

    private void Navigate(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationViewControl.SelectedItem == HomeItem) MainFrame.Navigate(typeof(HomePage));
        if ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationViewControl.SelectedItem == ReboundItem) MainFrame.Navigate(typeof(ReboundPage));
    }
}