using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
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
        for (int i = 0; i < context.ItemCount; i++)
        {
            var element = (FrameworkElement)context.GetOrCreateElementAt(i);

            if (element.DataContext is DesktopItem desktopItem)
            {
                // Subscribe to property changes if not already subscribed
                if (!IsSubscribedToPropertyChanges(element))
                {
                    desktopItem.PropertyChanged += OnDesktopItemPropertyChanged;
                    MarkAsSubscribedToPropertyChanges(element);
                }

                // Arrange the element at the specified position
                var position = new Point(desktopItem.X, desktopItem.Y);
                element.Arrange(new Rect(position, element.DesiredSize));
            }
        }

        return finalSize;
    }

    private void OnDesktopItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesktopItem.X) || e.PropertyName == nameof(DesktopItem.Y))
        {
            // Invalidate the layout to trigger re-arrangement
            InvalidateArrange();
        }
    }

    private bool IsSubscribedToPropertyChanges(FrameworkElement element)
    {
        return LayoutHelper.GetIsSubscribed(element);
    }

    private void MarkAsSubscribedToPropertyChanges(FrameworkElement element)
    {
        LayoutHelper.SetIsSubscribed(element, true);
    }
}

public static class LayoutHelper
{
    public static readonly DependencyProperty IsSubscribedProperty =
        DependencyProperty.RegisterAttached(
            "IsSubscribed",
            typeof(bool),
            typeof(LayoutHelper),
            new PropertyMetadata(false));

    public static bool GetIsSubscribed(DependencyObject obj) => (bool)obj.GetValue(IsSubscribedProperty);
    public static void SetIsSubscribed(DependencyObject obj, bool value) => obj.SetValue(IsSubscribedProperty, value);
}