// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Windows.UI.Xaml.Data;

namespace Rebound.Core.UI.UWP.Converters;

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
