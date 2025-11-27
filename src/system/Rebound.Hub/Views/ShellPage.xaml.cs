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
    private readonly List<SearchResult> SearchResults = [];
    private readonly ObservableCollection<SearchResult> Suggestions = [];

    private void NavigateTo(Type pageType, object? parameter = null)
    {
        MainFrame.Navigate(pageType, parameter);
    }

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
            // Create the category NavigationViewItem
            var categoryItem = new Microsoft.UI.Xaml.Controls.NavigationViewItem
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

            // Add mods as sub-items
            foreach (var mod in categoryGroup)
            {
                var modItem = new Microsoft.UI.Xaml.Controls.NavigationViewItem
                {
                    Content = mod.Name,
                    Tag = mod.Name,
                    Icon = new ImageIcon { Source = new BitmapImage(new Uri(mod.Icon!)) }
                };
                SearchResults.Add(new SearchResult
                {
                    Title = mod.Name!,
                    PageType = typeof(ModPage),
                    Parameter = mod
                });
                categoryItem.MenuItems.Add(modItem);
            }

            NavigationViewControl.MenuItems.Add(categoryItem);
        }

        SearchResults.Add(new()
        {
            Title = "Home",
            PageType = typeof(HomePage)
        });
        SearchResults.Add(new()
        {
            Title = "Rebound",
            PageType = typeof(ReboundPage)
        });
        SearchResults.Add(new()
        {
            Title = "Settings",
            PageType = typeof(SettingsPage)
        });

        Loaded += ShellPage_Loaded;
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
        // Settings button
        if (args.IsSettingsInvoked)
        {
            NavigateTo(typeof(SettingsPage));
            return;
        }

        // Normal item
        if (args.InvokedItemContainer is Microsoft.UI.Xaml.Controls.NavigationViewItem item &&
            item.Tag is string tag)
        {
            // Home
            if (tag == "Home")
            {
                NavigateTo(typeof(HomePage));
                return;
            }

            // Rebound
            if (tag == "Rebound")
            {
                NavigateTo(typeof(ReboundPage));
                return;
            }

            // Rebound
            if (tag == "Update")
            {
                Launcher.LaunchUriAsync(new("https://ivirius.com/download/rebound/")).Wait();
                return;
            }

            // Mods
            var mod = Catalog.Mods.FirstOrDefault(m => m.Name == tag);
            if (mod != null)
            {
                NavigateTo(typeof(ModPage), mod);
            }
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchResult result && result.PageType != null)
        {
            NavigateTo(result.PageType, result.Parameter);
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        string query = sender.Text?.Trim() ?? string.Empty;

        List<SearchResult> suitableItems = [];

        if (string.IsNullOrEmpty(query))
        {
            suitableItems = SearchResults.ToList();
        }
        else
        {
            string[] tokens = query.ToUpperInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries);
            suitableItems = SearchResults
                .Where(r => !string.IsNullOrEmpty(r.Title) &&
                            tokens.All(t => r.Title.Contains(t, StringComparison.InvariantCultureIgnoreCase))).ToList();
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
        {
            Suggestions.Add(item);
        }
    }
}