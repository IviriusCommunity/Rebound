using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    /// <summary>
    /// Method to switch the visual states of the title bar and its buttons based on the current selection state.
    /// </summary>
    /// <param name="buttonsState"></param>
    protected void SwitchState(ButtonsState buttonsState)
    {
        _isWindowFocused = IsWindowFocused(this.CurrentWindow);

        // If the buttons don't exist return
        if (this.CloseButton is null || this.MaximizeRestoreButton is null || this.MinimizeButton is null || _closed) return;

        // Default states
        var minimizeState = !this._isWindowFocused ? "Unfocused" : "Normal";
        var maximizeState = !this._isWindowFocused ? "Unfocused" : "Normal";
        var closeState = !this._isWindowFocused ? "Unfocused" : "Normal";
        var titleBarState = !this._isWindowFocused ? "Unfocused" : IsAccentTitleBarEnabled && IsAccentColorEnabledForTitleBars() ? "FocusedAccent" : "Focused";

        switch (buttonsState)
        {
            // Minimize button
            case ButtonsState.MinimizePointerOver or ButtonsState.MinimizePressed:
                {
                    switch (buttonsState)
                    {
                        case ButtonsState.MinimizePointerOver:
                            minimizeState = "PointerOver";
                            break;
                        case ButtonsState.MinimizePressed:
                            minimizeState = "Pressed";
                            break;
                        default:
                            break;
                    }
                    break;
                }

            // Maximize button
            case ButtonsState.MaximizePointerOver or ButtonsState.MaximizePressed:
                {
                    switch (buttonsState)
                    {
                        case ButtonsState.MaximizePointerOver:
                            maximizeState = "PointerOver";
                            break;
                        case ButtonsState.MaximizePressed:
                            maximizeState = "Pressed";
                            break;
                        default:
                            break;
                    }
                    break;
                }

            // Close button
            case ButtonsState.ClosePointerOver or ButtonsState.ClosePressed:
                {
                    switch (buttonsState)
                    {
                        case ButtonsState.ClosePointerOver:
                            closeState = "PointerOver";
                            break;
                        case ButtonsState.ClosePressed:
                            closeState = "Pressed";
                            break;
                        default:
                            break;
                    }
                    break;
                }

            // No buttons pressed
            case ButtonsState.None:
            {
                break;
            }
        }

        if (!this.IsClosable)
        {
            closeState = "Disabled";
        }

        if (!this.IsMinimizable)
        {
            minimizeState = "Disabled";
        }

        if (!this.IsMaximizable)
        {
            maximizeState = "Disabled";
        }

        if (IsAccentColorEnabledForTitleBars() && IsAccentTitleBarEnabled)
        {
            minimizeState = "Accent" + minimizeState;
            maximizeState = "Accent" + maximizeState;
            closeState = "Accent" + closeState;
        }

        if (IsToolWindow)
        {
            minimizeState = "Tool" + minimizeState;
            maximizeState = "Tool" + maximizeState;
            closeState = "Tool" + closeState;
        }

        if (_isMaximized)
        {
            maximizeState = "Maximized" + maximizeState;
        }

        if (!IsMaximizable && !IsMinimizable)
        {
            closeState = "Singular" + closeState;
        }

        // Handle WinUI tooltips
        if (this.UseWinUIEverywhere)
        {
            var minimizeTooltip = (ToolTip)ToolTipService.GetToolTip(this.MinimizeButton);
            var closeTooltip = (ToolTip)ToolTipService.GetToolTip(this.CloseButton);

            if (minimizeTooltip.IsOpen != (buttonsState == ButtonsState.MinimizePointerOver))
                minimizeTooltip.IsOpen = buttonsState == ButtonsState.MinimizePointerOver;
            if (closeTooltip.IsOpen != (buttonsState == ButtonsState.ClosePointerOver))
                closeTooltip.IsOpen = buttonsState == ButtonsState.ClosePointerOver;
        }

        // Apply the visual states based on the calculated states
        _ = VisualStateManager.GoToState(this.MinimizeButton, minimizeState, true);
        _ = VisualStateManager.GoToState(this.MaximizeRestoreButton, maximizeState, true);
        _ = VisualStateManager.GoToState(this.CloseButton, closeState, true);
        _ = VisualStateManager.GoToState(this, titleBarState, true);
    }
}
