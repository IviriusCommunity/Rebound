// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Hub.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Rebound.Hub.Views;

internal partial class CategoryToBoolOrVisibilityConverter : IValueConverter
{
    /// <summary>
    /// The expected category to compare against.
    /// </summary>
    public ModCategory ExpectedValue { get; set; }

    /// <summary>
    /// Output mode: "Bool" or "Visibility".
    /// </summary>
    public string OutputType { get; set; } = "Bool";

    /// <summary>
    /// Whether to invert the output.
    /// </summary>
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool matches = Equals(value, ExpectedValue);

        if (Invert)
            matches = !matches;

        return OutputType switch
        {
            "Visibility" => matches ? Visibility.Visible : Visibility.Collapsed,
            "Bool" => matches,
            _ => throw new InvalidOperationException($"Unsupported OutputType: {OutputType}"),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

internal partial class NotMandatoryToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is InstallationTemplate template)
        {
            return template != InstallationTemplate.Mandatory ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class InvertBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ModCategoryToVisibilityConverter : IValueConverter
{
    public ModCategory TargetCategory { get; set; }
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ModCategory category)
        {
            bool matches = category == TargetCategory;
            bool shouldShow = Invert ? !matches : matches;
            return shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ModCategoryToRowSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Mandatory mods don't have variant selector, so icon spans 2 rows
        // Others have variant selector, so icon spans 3 rows
        if (value is ModCategory category)
        {
            // Check if this mod would have variants shown (we can't directly check Variants.Count here)
            // For now, assume all non-mandatory might have variants
            return category == ModCategory.Sideloaded ? 3 : 2;
        }
        return 2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class CategoryToDescriptionMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Mandatory mods need bottom margin, others don't (variant selector handles spacing)
        if (value is ModCategory category)
        {
            // For mandatory (no variants), add bottom margin
            return "0,0,0,16";
        }
        return "0,0,0,0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class MandatoryToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Hide action buttons for mandatory mods
        if (value is InstallationTemplate template)
        {
            return template == InstallationTemplate.Mandatory ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class VariantCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ModToTasksListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Mod mod)
        {
            TextBlock tb = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Width = 320,
                MaxWidth = 320
            };

            tb.Inlines.Add(new Run()
            {
                Text = "This mod does the following:\n",
                FontWeight = FontWeights.SemiBold
            });

            // Check if a variant is selected
            if (mod.SelectedVariantIndex >= 0 && mod.SelectedVariantIndex < mod.Variants.Count)
            {
                var selectedVariant = mod.Variants[mod.SelectedVariantIndex];

                // Add variant name if there are multiple variants
                if (mod.Variants.Count > 1)
                {
                    tb.Inlines.Add(new Run()
                    {
                        Text = $"\n[{selectedVariant.Name}]\n",
                        FontStyle = FontStyle.Italic,
                    });
                }

                // List all cogs from the selected variant
                foreach (var cog in selectedVariant.Cogs)
                {
                    tb.Inlines.Add(new Run()
                    {
                        Text = $"\n•   {cog.TaskDescription}",
                    });
                }
            }
            else
            {
                // No variant selected - show a placeholder message
                tb.Inlines.Add(new Run()
                {
                    Text = "\n(No variant selected)",
                    FontStyle = FontStyle.Italic,
                });
            }

            return tb;
        }
        else return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ReboundPage : Page
{
    public ReboundPage()
    {
        InitializeComponent();
    }

    private async void ReboundView_Loaded(object sender, RoutedEventArgs e)
    {
        //App.ReboundService.CheckForUpdates();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Mod mod && mod.PreferredInstallationTemplate != InstallationTemplate.Mandatory)
        {
            if (App.MainWindow?.Content is Frame frame && frame.Content is ShellPage shellPage)
            {
                var ancestors = new Stack<Microsoft.UI.Xaml.Controls.NavigationViewItem>();
                if (FindNavItemByContent(shellPage.NavigationViewControl.MenuItems, mod.Name, out var navItem, ancestors) && navItem != null)
                {
                    // Expand all ancestors
                    foreach (var ancestor in ancestors)
                    {
                        ancestor.IsExpanded = true;
                    }

                    shellPage.NavigationViewControl.SelectedItem = navItem;
                }

                shellPage.NavigateTo(typeof(ModPage), mod);
            }
        }
    }

    private bool FindNavItemByContent(IEnumerable<object> items, string content, out Microsoft.UI.Xaml.Controls.NavigationViewItem? foundItem, Stack<Microsoft.UI.Xaml.Controls.NavigationViewItem> ancestors)
    {
        foreach (var item in items)
        {
            if (item is Microsoft.UI.Xaml.Controls.NavigationViewItem navItem)
            {
                if (navItem.Content?.ToString() == content)
                {
                    foundItem = navItem;
                    return true;
                }

                var submenu = navItem.MenuItems;
                if (submenu != null && submenu.Count > 0)
                {
                    ancestors.Push(navItem);
                    if (FindNavItemByContent(submenu, content, out foundItem, ancestors))
                    {
                        return true;
                    }
                    ancestors.Pop();
                }
            }
        }

        foundItem = null;
        return false;
    }
}