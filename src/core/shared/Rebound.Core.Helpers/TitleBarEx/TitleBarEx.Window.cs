using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using static Riverside.Toolkit.Helpers.NativeHelper;
using System;
using WinUIEx;
using System.Threading.Tasks;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    private void InvokeChecks()
    {
        UpdateWindowProperties();
        CheckMaximization();
        LoadDragRegion();
        if (_loaded)
        {
            SwitchState(ButtonsState.None);
        }
    }

    public virtual void LoadDragRegion()
    {
        try
        {
            // If the window has been closed, break the loop
            if (_closed) return;

            // Check if every condition is met
            if (this.CurrentWindow?.AppWindow is not null && this.IsAutoDragRegionEnabled)
            {
                // Width (Scaled window width)
                int width = (int)(this.CurrentWindow.Bounds.Width * Display.Scale(this.CurrentWindow));

                // Height (Scaled control actual height)
                int height = (int)((this.ActualHeight + _buttonDownHeight) * Display.Scale(this.CurrentWindow));

                // Set the drag region for the window's title bar
                this.CurrentWindow.AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, width, height)]);
            }
        }
        catch
        {
            return;
        }
    }

    public void SetWindowIcon(Uri titleBarPath, string taskbarPath)
    {
        try
        {
            // Attempt to set the title bar icon
            if (this.TitleBarIcon is not null)
            {
                this.TitleBarIcon.Source = new BitmapImage(titleBarPath);
            }
        }
        catch
        {

        }

        try
        {
            // Set the window icon
            this.CurrentWindow?.SetIcon(taskbarPath);
        }
        catch
        {

        }
    }

    private bool wasMaximized = false;

    private void CheckMaximization()
    {
        if (_closed || !_allowSizeCheck) return;

        if (this.CurrentWindow?.Presenter is OverlappedPresenter presenter)
        {
            switch (presenter.State)
            {
                case OverlappedPresenterState.Maximized:
                    HandleMaximizedState();
                    break;

                case OverlappedPresenterState.Restored:
                    HandleRestoredState();
                    break;
            }
        }
        else
        {
            HandleUnknownState();
        }

        wasMaximized = _isMaximized;

        // Local method to handle the maximized state
        void HandleMaximizedState()
        {
            if (this.MemorizeWindowPosition) SetValue($"{this.WindowTag}Maximized", true);

            _additionalHeight = WND_FRAME_TOP_MAXIMIZED; // Required for window drag region
            _isMaximized = true; // Required for NCHITTEST

            if (wasMaximized != _isMaximized) SwitchState(ButtonsState.None);
        }

        // Local method to handle the restored state
        void HandleRestoredState()
        {
            if (this.MemorizeWindowPosition)
            {
                SetValue($"{this.WindowTag}Maximized", false);
                SetValue<double>($"{this.WindowTag}PositionX", this.CurrentWindow.AppWindow.Position.X);
                SetValue<double>($"{this.WindowTag}PositionY", this.CurrentWindow.AppWindow.Position.Y);
                SetValue<double>($"{this.WindowTag}Width", this.CurrentWindow.AppWindow.Size.Width);
                SetValue<double>($"{this.WindowTag}Height", this.CurrentWindow.AppWindow.Size.Height);
            }

            _additionalHeight = WND_FRAME_TOP_NORMAL; // Required for window drag region
            _isMaximized = false; // Required for NCHITTEST

            if (wasMaximized != _isMaximized)
            {
                SwitchState(ButtonsState.None);
            }
        }

        // Local method to handle unknown presenter states
        void HandleUnknownState()
        {
            if (this.MemorizeWindowPosition) SetValue($"{this.WindowTag}Maximized", true);

            _additionalHeight = 0; // Required for window drag region
            _isMaximized = false; // Required for NCHITTEST
        }
    }

    public void UpdateWindowProperties()
    {
        try
        {
            // Update window capabilities
            this.CanMaximize = !_isMaximized && this.IsMaximizable;
            this.CanMove = !_isMaximized;
            this.CanSize = this.CurrentWindow is not null && !_isMaximized && this.CurrentWindow.IsResizable;
            this.CanRestore = _isMaximized && this.IsMaximizable;

            CurrentWindow?.ToggleExtendedWindowStyle(IsToolWindow, ExtendedWindowStyle.ToolWindow);

            if (this.MinimizeButton is not null && this.MaximizeRestoreButton is not null && Application.Current is not null && this.CloseButton is not null && this.TitleBarIcon != null)
            {
                TitleBarIcon.Visibility = ShowIcon ? Visibility.Visible : Visibility.Collapsed;

                if (this.CurrentWindow is not null)
                {
                    // Maximize
                    this.CurrentWindow.IsMaximizable = this.IsMaximizable;
                    this.MaximizeRestoreButton.IsEnabled = this.IsMaximizable;

                    // Minimize
                    this.CurrentWindow.IsMinimizable = this.IsMinimizable;
                    this.MinimizeButton.IsEnabled = this.IsMinimizable;

                    // Close
                    this.CloseButton.IsEnabled = this.IsClosable;
                }

                CheckMaximization();

                UpdateAccentStripVisibility();

                if (!IsMinimizable && !IsMaximizable)
                {
                    // If both minimize and maximize are disabled, hide the buttons
                    this.MinimizeButton.Visibility = Visibility.Collapsed;
                    this.MaximizeRestoreButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Show buttons if at least one is enabled
                    this.MinimizeButton.Visibility = Visibility.Visible;
                    this.MaximizeRestoreButton.Visibility = Visibility.Visible;
                }
            }
        }
        catch
        {

        }
    }

    private async void UpdateWindowSizeAndPosition()
    {
        // Exit if window position memory is disabled
        if (!this.MemorizeWindowPosition) return;

        // Prevent unnecessary size checks
        _allowSizeCheck = false;

        // Check if the window position is saved
        if (GetValue<object>($"{this.WindowTag}PositionX") is not null)
        {
            // Move and resize the window based on saved values
            MoveAndResize();

            // If the window was maximized, restore and maximize it again
            if (GetValue<object>($"{this.WindowTag}Maximized") is bool and true)
            {
                // Allow some time for the move/resize to take effect
                await Task.Delay(10);

                // Maximize the window
                this.CurrentWindow?.Maximize();
                _isMaximized = true;
            }
        }

        // Ensure window is maximized if the value is set to true
        if (GetValue<object>($"{this.WindowTag}Maximized") is bool maximized && maximized)
        {
            this.CurrentWindow?.Maximize();
            _isMaximized = true;
        }

        // Allow size checks to resume
        _allowSizeCheck = true;

        // Small delay before switching state
        await Task.Delay(50);

        // Reset button states
        SwitchState(ButtonsState.None);

        // Local method for applying dimensions
        void MoveAndResize() => this.CurrentWindow?.MoveAndResize(
                GetValue<double>($"{this.WindowTag}PositionX") / Display.Scale(this.CurrentWindow),
                GetValue<double>($"{this.WindowTag}PositionY") / Display.Scale(this.CurrentWindow),
                GetValue<double>($"{this.WindowTag}Width") / Display.Scale(this.CurrentWindow),
                GetValue<double>($"{this.WindowTag}Height") / Display.Scale(this.CurrentWindow));
    }

    public void UpdateAccentStripVisibility()
    {
        // If the window has been closed stop checking
        if (_closed) return;

        // Update based on accent title bar settings
        if (IsAccentColorEnabledForTitleBars() && this.IsAccentTitleBarEnabled && IsWindowFocused(this.CurrentWindow))
        {
            // Accent enabled
            if (this.AccentStrip is not null) this.AccentStrip.Visibility = Visibility.Visible;
        }
        else
        {
            // Accent disabled
            if (this.AccentStrip is not null) this.AccentStrip.Visibility = Visibility.Collapsed;
        }

        //SwitchState(ButtonsState.None);
    }
}

public static class WindowExtensions
{
    // Window messages
    private const int WM_SYSCOMMAND = 0x0112; // System command

    // System commands
    private const int SC_MOVE = 0xF010;       // Move window
    private const int SC_SIZE = 0xF000;       // Resize window

    public static IntPtr GetHwnd(this WindowEx windowEx)
        // Get the native window handle (HWND)
        => WinRT.Interop.WindowNative.GetWindowHandle(windowEx);

    public static void InvokeResize(this WindowEx windowEx) => PInvoke.PostMessage((HWND)windowEx.GetHwnd(), WM_SYSCOMMAND, SC_SIZE, IntPtr.Zero);

    public static void InvokeMove(this WindowEx windowEx) => PInvoke.PostMessage((HWND)windowEx.GetHwnd(), WM_SYSCOMMAND, SC_MOVE, IntPtr.Zero);
}
