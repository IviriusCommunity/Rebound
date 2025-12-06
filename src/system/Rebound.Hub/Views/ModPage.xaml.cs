// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Rebound.Forge;
using System;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Rebound.Hub.Views;

internal partial class VariantSettingsConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int index && parameter is Mod mod)
        {
            if (index >= 0 && index < mod.Variants.Count)
            {
                return mod.Variants[index].Settings;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ModToTasksListConverter_Page : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Mod mod)
        {
            TextBlock tb = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new(0, 0, 24, 24),
                HorizontalAlignment = HorizontalAlignment.Stretch
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

internal partial class GlyphToIconSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return new FontIcon
            {
                Glyph = s
            };
        }
        else return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

internal partial class ModSeverityToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ModInfoBarSeverity severity
            ? (InfoBarSeverity)(int)severity
            : InfoBarSeverity.Informational;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is InfoBarSeverity severity
            ? (ModInfoBarSeverity)(int)severity
            : ModInfoBarSeverity.Informational;
    }
}

internal partial class ModSettingTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BoolSettingTemplate { get; set; }
    public DataTemplate? StringSettingTemplate { get; set; }
    public DataTemplate? EnumSettingTemplate { get; set; }
    public DataTemplate? InfoBarTemplate { get; set; }
    public DataTemplate? LabelTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ModBoolSetting => BoolSettingTemplate,
            ModStringSetting => StringSettingTemplate,
            ModEnumSetting => EnumSettingTemplate,
            ModInfoBar => InfoBarTemplate,
            ModLabel => LabelTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}

internal sealed partial class ModPage : Page
{
    private Mod? Mod { get; set; }
    private bool isLoaded = false;
    private bool isNavigated = false;

    public ModPage()
    {
        InitializeComponent();
    }

    private void ItemsControl_Loaded(object sender, RoutedEventArgs e)
    {
        isLoaded = true;
        TryUpdateSettings();

        if (Mod != null)
        {
            Mod.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(Mod.SelectedVariantIndex))
                {
                    UpdateSettings();
                }
            };
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e?.Parameter is Mod mod)
        {
            Mod = mod;
            DataContext = Mod;
            await Mod.UpdateIntegrityAsync().ConfigureAwait(false);
            isNavigated = true;
            TryUpdateSettings();
        }
    }

    private void TryUpdateSettings()
    {
        // Only update when both conditions are met
        if (isLoaded && isNavigated)
        {
            UpdateSettings();
        }
    }

    private async void UpdateSettings()
    {
        if (Mod == null) return;

        // Small delay to ensure visual tree is ready
        await Task.Delay(50);

        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        {
            var itemsControl = FindItemsControlInVisualTree(Content);
            var progressRing = FindProgressRingInVisualTree(Content);

            if (itemsControl != null && progressRing != null)
            {
                progressRing.Visibility = Visibility.Visible;
                itemsControl.Visibility = Visibility.Collapsed;

                if (Mod.SelectedVariantIndex >= 0 && Mod.SelectedVariantIndex < Mod.Variants.Count)
                {
                    itemsControl.ItemsSource = Mod.Variants[Mod.SelectedVariantIndex].Settings;
                }

                itemsControl.Visibility = Visibility.Visible;
                progressRing.Visibility = Visibility.Collapsed;
            }
        });
    }

    private ItemsControl? FindItemsControlInVisualTree(DependencyObject? parent)
    {
        if (parent == null) return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // Match against THIS page's Mod instance specifically
            if (child is ItemsControl itemsControl && ReferenceEquals(itemsControl.Tag, Mod))
            {
                return itemsControl;
            }

            var result = FindItemsControlInVisualTree(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private Microsoft.UI.Xaml.Controls.ProgressRing? FindProgressRingInVisualTree(DependencyObject? parent)
    {
        if (parent == null) return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is Microsoft.UI.Xaml.Controls.ProgressRing progressRing && progressRing.Tag is "ItemsControlRing")
            {
                return progressRing;
            }

            var result = FindProgressRingInVisualTree(child);
            if (result != null)
                return result;
        }

        return null;
    }
}
