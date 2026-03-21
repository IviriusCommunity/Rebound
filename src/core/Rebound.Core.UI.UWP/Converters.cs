// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Rebound.Core.UI.UWP.Converters;

public partial class CplIconConverter : IValueConverter
{
    private const string GlyphPrefix = "glyph:";
    private const string ImagePrefix = "img:";

    public static object? ConvertIcon(object value, Type targetType, object parameter, string language)
    {
        try
        {
            if (value is not string icon || string.IsNullOrEmpty(icon))
                return null;

            if (icon.StartsWith(GlyphPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var glyph = icon[GlyphPrefix.Length..];
                if (string.IsNullOrEmpty(glyph))
                    return null;
                return new FontIcon { Glyph = glyph };
            }

            if (icon.StartsWith(ImagePrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var path = icon[ImagePrefix.Length..];
                if (string.IsNullOrEmpty(path))
                    return null;
                if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
                    return null;
                return new ImageIcon { Source = new BitmapImage(uri) };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertIcon(value, targetType, parameter, language);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public partial class StringToUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            return new Uri(path);
        }
        return new Uri(string.Empty);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Uri uri)
        {
            return uri.AbsolutePath;
        }
        return string.Empty;
    }
}
