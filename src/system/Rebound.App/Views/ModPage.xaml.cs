using Microsoft.UI.Xaml.Controls;
using Rebound.Forge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Rebound.Hub.Views;

internal class GlyphToIconSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
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

internal class ModSeverityToInfoBarSeverityConverter : IValueConverter
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
    public DataTemplate BoolSettingTemplate { get; set; }
    public DataTemplate StringSettingTemplate { get; set; }
    public DataTemplate EnumSettingTemplate { get; set; }
    public DataTemplate InfoBarTemplate { get; set; }
    public DataTemplate LabelTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
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

public sealed partial class ModPage : Page
{
    Mod Mod { get; set; } = null!;

    internal ModPage(Mod mod)
    {
        Mod = mod;
        this.InitializeComponent();
    }
}
