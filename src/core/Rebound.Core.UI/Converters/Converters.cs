// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Rebound.Core.UI.Converters;

public partial class IconStringToIconSourceConverter : IValueConverter
{
    private const string GlyphPrefix = "glyph:";
    private const string ImagePrefix = "img:";
    private const string PathPrefix = "path:";

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

            if (icon.StartsWith(PathPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var path = icon[PathPrefix.Length..];
                if (string.IsNullOrEmpty(path))
                    return null;
                return new PathIcon { Data = PathMarkupToGeometry(path) };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Source - https://stackoverflow.com/a/25258401
    // Posted by Prakash Selvaraj
    // Retrieved 2026-07-13, License - CC BY-SA 3.0

    private static Geometry? PathMarkupToGeometry(string pathMarkup)
    {
        string xaml =
        "<Path " +
        "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
        "<Path.Data>" + pathMarkup + "</Path.Data></Path>";
        var path = XamlReader.Load(xaml) as Microsoft.UI.Xaml.Shapes.Path;
        // Detach the PathGeometry from the Path
        Geometry? geometry = path?.Data;
        path?.Data = null;
        return geometry;
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
