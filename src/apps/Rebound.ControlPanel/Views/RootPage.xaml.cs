// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Core.UI.UWP.Converters;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewItemBase = Microsoft.UI.Xaml.Controls.NavigationViewItemBase;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Rebound.ControlPanel.Views;

internal sealed partial class RootPage : Page
{
    // The \\\\ is a workaround for this thing: https://github.com/CommunityToolkit/Labs-Windows/issues/788
    // Remove once fixed
    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")]
    public partial string UserPicturePath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = false)]
    public partial bool CanGoBack { get; set; }

    [GeneratedDependencyProperty(DefaultValue = false)]
    public partial bool CanGoForward { get; set; }

    public RootPage()
    {
        InitializeComponent();
        BuildNavItems(NavView.MenuItems, CplItemPairs.CplItems);
    }

    private static void BuildNavItems(IList<object> target, IEnumerable<CplItem> source)
    {
        foreach (var item in source)
        {
            var navItem = new NavigationViewItem
            {
                Content = item.Name,
                Tag = item,
                IsEnabled = item.IsEnabled,
                SelectsOnInvoked = item.SelectsOnInvoked,
                Icon = (IconElement?)CplIconConverter.ConvertIcon(item.Icon!, typeof(Windows.UI.Xaml.UIElement), null, null)
            };

            if (item.Children.Count > 0)
                BuildNavItems(navItem.MenuItems, item.Children);

            target.Add(navItem);
        }
    }

    private NavigationViewItemBase? GetNavViewItemFromTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
            return null;
        return SearchItems(NavView.MenuItems, tag);
    }

    private static NavigationViewItemBase? SearchItems(IList<object> items, string tag)
    {
        foreach (var item in items)
        {
            if (item is NavigationViewItem navItem
                && navItem.Tag is CplItem cplItem)
            {
                if (cplItem.Tag == tag)
                    return navItem;
                var result = SearchItems(navItem.MenuItems, tag);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer is not NavigationViewItem navItem)
                return;
            if (navItem.Tag is not CplItem item)
                return;
            await CplItemPairs.InvokeAsync(item);
        }
        catch { }
    }

    [RelayCommand]
    public void GoBack()
    {
        try
        {
            if (RootFrame.CanGoBack)
                RootFrame.GoBack();
        }
        catch { }
    }

    [RelayCommand]
    public void GoForward()
    {
        try
        {
            if (RootFrame.CanGoForward)
                RootFrame.GoForward();
        }
        catch { }
    }

    [RelayCommand]
    public void GoHome()
    {
        try
        {
            RootFrame.Navigate(typeof(HomePage));
        }
        catch { }
    }

    [RelayCommand]
    public void TogglePane()
        => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private void RootFrame_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        UIThreadQueue.QueueAction(() =>
        {
            UserPicturePath = UserInformation.GetUserPicturePath() ?? string.Empty;
        });

        // Navigate to home on first load
        //RootFrame.Navigate(typeof(HomePage));
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Sync nav item selection to current page
        var item = CplItemPairs.GetFromPage(e.SourcePageType);
        NavView.SelectedItem = item?.Tag != null
            ? GetNavViewItemFromTag(item.Tag)
            : null;

        AddressBar.Text = item?.Name ?? string.Empty;

        // Sync back button state
        GoBackCommand.NotifyCanExecuteChanged();

        CanGoBack = RootFrame.CanGoBack;
        CanGoForward = RootFrame.CanGoForward;
    }
}