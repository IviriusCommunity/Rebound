// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Rebound.Shell.Desktop;

public partial class GridLayout : VirtualizingLayout
{
    public GridLayout()
    {

    }

    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        double maxX = 0;
        double maxY = 0;

        for (var i = 0; i < context?.ItemCount; i++)
        {
            var element = context.GetOrCreateElementAt(i);
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (((FrameworkElement)element).DataContext is DesktopItem desktopItem)
            {
                var x = desktopItem.X + element.DesiredSize.Width;
                var y = desktopItem.Y + element.DesiredSize.Height;
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return new Size(maxX, maxY);
    }

    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        for (var i = 0; i < context?.ItemCount; i++)
        {
            var element = (FrameworkElement)context.GetOrCreateElementAt(i);

            if (element.DataContext is DesktopItem desktopItem)
            {
                // Subscribe only once
                if (!IsSubscribedToPropertyChanges(element))
                {
                    desktopItem.PropertyChanged += OnDesktopItemPropertyChanged;
                    MarkAsSubscribedToPropertyChanges(element);
                }

                // Clamp negative positions if needed (optional)
                var x = Math.Max(0, desktopItem.X);
                var y = Math.Max(0, desktopItem.Y);

                // Align to layout rounding if desired
                x = Math.Floor(x);
                y = Math.Floor(y);

                // Arrange the item
                var arrangeRect = new Rect(new Point(x, y), element.DesiredSize);
                element.Arrange(arrangeRect);
            }
        }

        return finalSize;
    }

    private void OnDesktopItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is (nameof(DesktopItem.X)) or (nameof(DesktopItem.Y)))
        {
            // Invalidate the layout to trigger re-arrangement
            InvalidateArrange();
        }
    }

    private static bool IsSubscribedToPropertyChanges(FrameworkElement element) => LayoutHelper.GetIsSubscribed(element);

    private static void MarkAsSubscribedToPropertyChanges(FrameworkElement element) => LayoutHelper.SetIsSubscribed(element, true);
}

public static class LayoutHelper
{
    public static readonly DependencyProperty IsSubscribedProperty =
        DependencyProperty.RegisterAttached(
            "IsSubscribed",
            typeof(bool),
            typeof(LayoutHelper),
            new PropertyMetadata(false));

    public static bool GetIsSubscribed(DependencyObject obj) => (bool?)obj?.GetValue(IsSubscribedProperty) ?? false;
    public static void SetIsSubscribed(DependencyObject obj, bool value) => obj?.SetValue(IsSubscribedProperty, value);
}