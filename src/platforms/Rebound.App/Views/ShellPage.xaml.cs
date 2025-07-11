﻿using System.Net.Http;
using CommunityToolkit.WinUI.Helpers;
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
        try
        {
            if (NetworkHelper.Instance.ConnectionInformation.ConnectionType != ConnectionType.Unknown)
            {
                using var client = new HttpClient();
                var url = "https://ivirius.com/reboundhubversion.txt";
                var webContent = await client.GetStringAsync(url);

                if (Helpers.Environment.ReboundVersion.REBOUND_VERSION != webContent)
                {
                    UpdateButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
            }
        }
        catch
        {

        }
    }

    private void Navigate(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == HomeItem) MainFrame.Navigate(typeof(HomePage));
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == ReboundItem) MainFrame.Navigate(typeof(Rebound11Page));
        if ((NavigationViewItem)NavigationViewControl.SelectedItem == RectifyItem) MainFrame.Navigate(typeof(Rectify11Page));
    }
}