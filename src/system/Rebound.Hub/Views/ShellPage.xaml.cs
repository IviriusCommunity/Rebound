// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core;
using Rebound.Core.UI;
using Rebound.Forge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Hub.Views;

internal partial class SearchResult : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial Type? PageType { get; set; }
    [ObservableProperty] public partial object? Parameter { get; set; }

    public override string ToString() => Title;
}

internal sealed partial class ShellPage : Page
{
    private readonly List<SearchResult> SearchResults = new();
    private readonly ObservableCollection<SearchResult> Suggestions = new();

    // Store all catalog mod related nav items and search results separately for toggling
    private readonly List<SearchResult> _catalogSearchResults = new();
    private readonly List<NavigationViewItem> _catalogNavItems = new(); 
    private readonly List<NavigationViewItem> _categoryItems = new();

    public ShellPage()
    {
        InitializeComponent();
        NavigationViewControl.SelectedItem = HomeItem;
        NavigateTo(typeof(HomePage));

        // Group mods by category
        var categoryGroups = Catalog.Mods
            .GroupBy(m => m.Category)
            .OrderBy(g => g.Key);

        foreach (var categoryGroup in categoryGroups)
        {
            var categoryItem = new NavigationViewItem
            {
                Content = categoryGroup.Key.ToString(),
                Tag = categoryGroup.Key.ToString(),
                SelectsOnInvoked = false,
                Icon = new FontIcon
                {
                    Glyph = categoryGroup.Key switch
                    {
                        ModCategory.General => "\uE8B0",
                        ModCategory.Productivity => "\uE762",
                        ModCategory.SystemAdministration => "\uEA18",
                        ModCategory.Customization => "\uE771",
                        ModCategory.Extras => "\uE794",
                        ModCategory.Sideloaded => "\uE74C",
                        _ => "\uE897"
                    }
                }
            };

            var modNavItems = new List<NavigationViewItem>();
            var modSearchResults = new List<SearchResult>();

            foreach (var mod in categoryGroup)
            {
                var modItem = new NavigationViewItem
                {
                    Content = mod.Name,
                    Tag = mod.Name,
                    Icon = new ImageIcon { Source = new BitmapImage(new Uri(mod.Icon!)) }
                };

                var searchResult = new SearchResult
                {
                    Title = mod.Name!,
                    PageType = typeof(ModPage),
                    Parameter = mod
                };

                modNavItems.Add(modItem);
                modSearchResults.Add(searchResult);
            }

            // Add mod items as children of category item
            foreach (var modItem in modNavItems)
            {
                categoryItem.MenuItems.Add(modItem);
            }

            _catalogNavItems.AddRange(modNavItems);
            _catalogSearchResults.AddRange(modSearchResults);
            _categoryItems.Add(categoryItem);
        }

        // Add static items to SearchResults directly
        SearchResults.Add(new SearchResult { Title = "Home", PageType = typeof(HomePage) });
        SearchResults.Add(new SearchResult { Title = "Rebound", PageType = typeof(ReboundPage) });
        SearchResults.Add(new SearchResult { Title = "Settings", PageType = typeof(SettingsPage) });

        Loaded += ShellPage_Loaded;

        App.ReboundService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(App.ReboundService.IsReboundEnabled))
            {
                var enabled = App.ReboundService.IsReboundEnabled;
                ToggleCatalogMods(enabled);
            }
        };

        // Initialize based on current state
        ToggleCatalogMods(App.ReboundService.IsReboundEnabled);
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        MainFrame.Navigate(pageType, parameter);
    }

    private void ToggleCatalogMods(bool isEnabled)
    {
        if (isEnabled)
        {
            // Add categories with their children (mods)
            foreach (var categoryItem in _categoryItems)
            {
                if (!NavigationViewControl.MenuItems.Contains(categoryItem))
                {
                    NavigationViewControl.MenuItems.Add(categoryItem);
                }
            }

            // Add catalog search results
            foreach (var searchResult in _catalogSearchResults)
            {
                if (!SearchResults.Contains(searchResult))
                    SearchResults.Add(searchResult);
            }
        }
        else
        {
            // Remove all category items (and thus mods)
            foreach (var categoryItem in _categoryItems)
            {
                NavigationViewControl.MenuItems.Remove(categoryItem);
            }

            // Remove catalog mod search results
            foreach (var searchResult in _catalogSearchResults)
            {
                SearchResults.Remove(searchResult);
            }
        }

        Suggestions.Clear();
        foreach (var item in SearchResults)
            Suggestions.Add(item);
    }

    private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckForUpdates();
    }

    public async Task CheckForUpdates()
    {
        try
        {
            if (NetworkHelper.Instance.ConnectionInformation.ConnectionType != ConnectionType.Unknown)
            {
                using var client = new HttpClient();
                var url = "https://ivirius.com/reboundhubversion.txt";
                var webContent = await client.GetStringAsync(new Uri(url));

                await UIThreadQueue.QueueActionAsync(() =>
                {
                    if (Variables.ReboundVersion != webContent)
                        UpdateItem.Visibility = Visibility.Visible;
                    return Task.CompletedTask;
                });
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ShellPage] Failed to check for updates.", ex);
        }
    }

    private void NavigationViewControl_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateTo(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "Home":
                    NavigateTo(typeof(HomePage));
                    break;
                case "Rebound":
                    NavigateTo(typeof(ReboundPage));
                    break;
                case "Update":
                    Launcher.LaunchUriAsync(new Uri("https://ivirius.com/download/rebound/")).AsTask().Wait();
                    break;
                default:
                    var mod = Catalog.Mods.FirstOrDefault(m => m.Name == tag);
                    if (mod != null)
                    {
                        NavigateTo(typeof(ModPage), mod);
                    }
                    break;
            }
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchResult result && result.PageType != null)
        {
            NavigateTo(result.PageType, result.Parameter);

            if (result.Parameter is Mod mod)
            {
                var ancestors = new Stack<NavigationViewItem>();
                if (FindNavItemByContent(NavigationViewControl.MenuItems, mod.Name, out var navItem, ancestors) && navItem != null)
                {
                    foreach (var ancestor in ancestors)
                        ancestor.IsExpanded = true;

                    NavigationViewControl.SelectedItem = navItem;
                }
            }
            else
            {
                var ancestors = new Stack<NavigationViewItem>();
                if (FindNavItemByContent(NavigationViewControl.MenuItems, result.Title, out var navItem, ancestors) && navItem != null)
                {
                    foreach (var ancestor in ancestors)
                        ancestor.IsExpanded = true;

                    NavigationViewControl.SelectedItem = navItem;
                }
            }
        }
    }

    private bool FindNavItemByContent(IEnumerable<object> items, string content, out NavigationViewItem? foundItem, Stack<NavigationViewItem> ancestors)
    {
        foreach (var item in items)
        {
            if (item is NavigationViewItem navItem)
            {
                if (navItem.Content?.ToString() == content)
                {
                    foundItem = navItem;
                    return true;
                }

                if (navItem.MenuItems?.Count > 0)
                {
                    ancestors.Push(navItem);
                    if (FindNavItemByContent(navItem.MenuItems, content, out foundItem, ancestors))
                        return true;
                    ancestors.Pop();
                }
            }
        }

        foundItem = null;
        return false;
    }

    // Overload for FindNavItemByContent without ancestors stack (returns the found item)
    private bool FindNavItemByContent(IEnumerable<object> items, string content, out NavigationViewItem foundItem)
    {
        var ancestors = new Stack<NavigationViewItem>();
        return FindNavItemByContent(items, content, out foundItem, ancestors);
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        string query = sender.Text?.Trim() ?? string.Empty;

        List<SearchResult> suitableItems;

        if (string.IsNullOrEmpty(query))
        {
            suitableItems = SearchResults.ToList();
        }
        else
        {
            string[] tokens = query.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            suitableItems = SearchResults
                .Where(r => !string.IsNullOrEmpty(r.Title) &&
                            tokens.All(t => r.Title.Contains(t, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
        }

        if (suitableItems.Count == 0)
        {
            suitableItems.Add(new SearchResult
            {
                Title = "No results found",
                PageType = null,
                Parameter = null
            });
        }

        Suggestions.Clear();
        foreach (var item in suitableItems)
            Suggestions.Add(item);
    }
}