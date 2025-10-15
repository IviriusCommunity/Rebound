// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.Forge;
using System;
using System.Linq;
using System.Net.Http;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Hub.Views;

internal sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        NavigationViewControl.SelectedItem = HomeItem;

        // Branding settings
        if (SettingsHelper.GetValue("ShowBranding", "rebound", true))
            MainFrame.Navigate(typeof(HomePage));
        else
        {
            NavigationViewControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            OverlayFrame.Visibility = Windows.UI.Xaml.Visibility.Visible;
            OverlayFrame.Navigate(typeof(ReboundPage));
        }

        foreach (var mod in Catalog.Mods)
        {
            var item = new Microsoft.UI.Xaml.Controls.NavigationViewItem
            {
                Content = mod.Name,
                Tag = mod.Name,
                Icon = new ImageIcon() { Source = new BitmapImage(new(mod.Icon)) }
            };
            NavigationViewControl.MenuItems.Add(item);
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
                var webContent = await client.GetStringAsync(new Uri(url));

                if (Core.Helpers.Environment.ReboundVersion.REBOUND_VERSION != webContent)
                    UpdateButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ShellPage] Failed to check for updates.", ex);
        }
    }

    private void Navigate(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationViewControl.SelectedItem == HomeItem)
            MainFrame.Navigate(typeof(HomePage));
        if ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationViewControl.SelectedItem == ReboundItem)
            MainFrame.Navigate(typeof(ReboundPage));
        else
        {
            var mod = Catalog.Mods.FirstOrDefault(m => m.Name == (string)((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationViewControl.SelectedItem).Tag);
            if (mod != null)
            {
                MainFrame.Content = new ModPage(mod);
            }
        }
    }
}