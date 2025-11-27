// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Rebound.Forge;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;

namespace Rebound.Hub.Views;

internal partial class ModToTasksListConverter_Page : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Mod mod)
        {
            TextBlock tb = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(0, 0, 24, 24)
            };

            bool firstLine = false;

            foreach (var cog in mod.Cogs)
            {
                tb.Inlines.Add(new Run()
                {
                    Text = $"{(firstLine ? "\n" : "")}•   {cog.TaskDescription}",
                });
                firstLine = true;
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

    public ModPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e?.Parameter is Mod mod)
        {
            Mod = mod;
            DataContext = Mod; // If you want
            await Mod.UpdateIntegrityAsync().ConfigureAwait(false);
        }
    }
}
