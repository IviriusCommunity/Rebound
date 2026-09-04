// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TerraFX.Interop.Windows;
using WinUIEx;
using static TerraFX.Interop.Windows.SC;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;

namespace Rebound.Core.UI.Controls;

[TemplatePart(Name = "PART_IconCaption", Type = typeof(Image))]
[TemplatePart(Name = "PART_DragRegion", Type = typeof(Grid))]

[TemplatePart(Name = "PART_MaximizeCaptionButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_CloseCaptionButton", Type = typeof(Button))]
public sealed partial class TitleBar : Control
{
    private WindowEx? _window;
    private Image? _iconCaption;
    private Grid? _dragRegion;
    private Button? _maximizeButton;
    private Button? _closeButton;

    private bool _isClosed;

    [GeneratedDependencyProperty] public partial string? Title { get; set; }

    [GeneratedDependencyProperty] public partial string? Subtitle { get; set; }

    [GeneratedDependencyProperty] public partial string? Badge { get; set; }

    [GeneratedDependencyProperty] public partial ImageSource? Icon { get; set; }

    [GeneratedDependencyProperty] public partial Visibility IconVisibility { get; private set; }

    [GeneratedDependencyProperty] public partial Visibility SubtitleVisibility { get; private set; }

    [GeneratedDependencyProperty] public partial Visibility BadgeVisibility { get; private set; }

    partial void OnIconPropertyChanged(DependencyPropertyChangedEventArgs e)
        => IconVisibility = Icon == null ? Visibility.Collapsed : Visibility.Visible;

    partial void OnSubtitlePropertyChanged(DependencyPropertyChangedEventArgs e)
        => SubtitleVisibility = Subtitle == null ? Visibility.Collapsed : Visibility.Visible;

    partial void OnBadgePropertyChanged(DependencyPropertyChangedEventArgs e)
        => BadgeVisibility = Badge == null ? Visibility.Collapsed : Visibility.Visible;

    public TitleBar()
    {
        DefaultStyleKey = typeof(TitleBar);
        IconVisibility = Icon == null ? Visibility.Collapsed : Visibility.Visible;
        SubtitleVisibility = Subtitle == null ? Visibility.Collapsed : Visibility.Visible;
        BadgeVisibility = Badge == null ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconCaption = GetTemplateChild("PART_IconCaption") as Image;
        _iconCaption?.DoubleTapped += IconCaption_DoubleTapped;
        _iconCaption?.Tapped += IconCaption_Tapped;
        _dragRegion = GetTemplateChild("PART_DragRegion") as Grid;

        _maximizeButton = GetTemplateChild("PART_MaximizeCaptionButton") as Button;
    }

    public void Initialize(WindowEx window)
    {
        _window = window;
        _window.SizeChanged += Window_SizeChanged;
        _window.PositionChanged += Window_PositionChanged;
        _window.Activated += Window_Activated;
        _window.WindowStateChanged += Window_WindowStateChanged;
        _window.PresenterChanged += Window_PresenterChanged;
        _window.Closed += _window_Closed;
        SizeChanged += TitleBar_SizeChanged;
    }

    private void _window_Closed(object sender, WindowEventArgs args)
        => _isClosed = true;

    private CancellationTokenSource? _tapCts;

    private async void IconCaption_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
        _tapCts?.Cancel();
        _tapCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(100, _tapCts.Token).ConfigureAwait(true);

            if (_window != null)
            {
                unsafe
                {
                    var handle = new HWND((void*)_window.GetWindowHandle());
                    PostMessageW(handle, WM_SYSCOMMAND, SC_KEYMENU & 0xFFF0, 0);
                }
            }
        }
        catch (TaskCanceledException)
        {

        }
    }

    private void IconCaption_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        e.Handled = true;
        _tapCts?.Cancel();
        _window?.Close();
    }

    private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e) => RecalculateNCAreas();
    private void Window_PresenterChanged(object? sender, Microsoft.UI.Windowing.AppWindowPresenter e) => DispatcherQueue.TryEnqueue(RecalculateNCAreas);
    private void Window_WindowStateChanged(object? sender, WindowState e) => DispatcherQueue.TryEnqueue(RecalculateNCAreas);
    private void Window_Activated(object sender, WindowActivatedEventArgs args) => DispatcherQueue.TryEnqueue(RecalculateNCAreas);
    private void Window_PositionChanged(object? sender, Windows.Graphics.PointInt32 e) => DispatcherQueue.TryEnqueue(RecalculateNCAreas);
    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args) => DispatcherQueue.TryEnqueue(RecalculateNCAreas);

    private void RecalculateNCAreas()
    {
        if (_window == null || _isClosed)
            return;

        var root = _window.Content;
        var scale = _window.Content.XamlRoot.RasterizationScale;
        var incps = InputNonClientPointerSource.GetForWindowId(
            _window!.AppWindow.Id);

        /*if (_iconCaption != null)
        {
            var iconPoint = _iconCaption.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
            var iconRect = new Windows.Graphics.RectInt32(
                (int)(iconPoint.X * scale),
                (int)(iconPoint.Y * scale),
                (int)(_iconCaption.ActualWidth * scale),
                (int)(_iconCaption.ActualHeight * scale));

            incps.SetRegionRects(
                NonClientRegionKind.Icon,
                [iconRect]);
        }*/
        if (_dragRegion != null)
        {
            var dragPoint = _dragRegion.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
            var dragRect = new Windows.Graphics.RectInt32(
                (int)(dragPoint.X * scale),
                (int)(dragPoint.Y * scale),
                (int)(_dragRegion.ActualWidth * scale),
                (int)(_dragRegion.ActualHeight * scale));

            incps.SetRegionRects(
                NonClientRegionKind.Caption,
                [dragRect]);
        }
        if (_maximizeButton != null)
        {
            var maxPoint = _maximizeButton.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0, 0));
            var maxRect = new Windows.Graphics.RectInt32(
                (int)(maxPoint.X * scale),
                (int)(maxPoint.Y * scale),
                (int)(_maximizeButton.ActualWidth * scale),
                (int)(_maximizeButton.ActualHeight * scale));

            incps.SetRegionRects(
                NonClientRegionKind.Maximize,
                [maxRect]);
        }
    }
}
