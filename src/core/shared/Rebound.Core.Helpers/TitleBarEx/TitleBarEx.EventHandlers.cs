using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    private void SwitchButtonStatePointerEvent(object sender, PointerRoutedEventArgs e)
    {
        InvokeChecks();
    }

    private void Content_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        InvokeChecks();
    }

    private void ContentLoaded(object sender, RoutedEventArgs e) => InvokeChecks();

    private void CurrentWindow_WindowStateChanged(object? sender, WindowState e) => InvokeChecks();

    private void CurrentWindow_PositionChanged(object? sender, Windows.Graphics.PointInt32 e) => InvokeChecks();

    private void CurrentWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args) => InvokeChecks();

    private void CurrentWindow_Closed(object sender, WindowEventArgs args)
    {
        if (HookIntoClosedEvent)
        {
            args.Handled = !this.IsClosable;
            _closed = this.IsClosable;
        }
    }

    private void CheckMouseButtonDownPointerEvent(object sender, PointerRoutedEventArgs e)
    {
        SwitchState(ButtonsState.None);

        if (!IsLeftMouseButtonDown())
            this.CurrentCaption = SelectedCaptionButton.None;
    }
}
