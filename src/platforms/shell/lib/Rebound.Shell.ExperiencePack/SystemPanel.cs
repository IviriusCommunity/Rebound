// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.UI;
using System;
using TerraFX.Interop.Windows;
using Windows.UI.Xaml;
using Rebound.Core.Helpers;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.SWP;

#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.Shell.ExperiencePack;

/// <summary>
/// Specifies the possible positions for a system panel on the screen.
/// </summary>
public enum SystemPanelPosition
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// Specifies the visibility behavior of a system panel.
/// </summary>
public enum SystemPanelVisibilityMode
{
    /// <summary>
    /// The panel reserves its own AppBar area and is always visible.
    /// </summary>
    AlwaysVisible,

    /// <summary>
    /// The panel is a floating window with no AppBar area that hides automatically
    /// when another window is approaching. It can be revealed by moving the mouse
    /// to the corresponding screen edge.
    /// </summary>
    AutoHide,

    /// <summary>
    /// The panel is a floating window with no AppBar area that is completely hidden
    /// and requires moving the mouse to the corresponding screen edge to reveal it.
    /// </summary>
    Hidden
}

/// <summary>
/// Represents a system panel with configurable position, floating behavior, visibility mode, size, etc.
/// </summary>
public partial class SystemPanel : ObservableObject
{
    /// <summary>
    /// The panel's position on the screen.
    /// </summary>
    [ObservableProperty] public partial SystemPanelPosition Position { get; set; }

    /// <summary>
    /// True if the panel should appear as floating with margins and rounded corners when no window is nearby,
    /// and expand to full size when a window approaches (only applicable for AlwaysVisible mode). False
    /// for fixed panels.
    /// </summary>
    [ObservableProperty] public partial bool Floating { get; set; }

    /// <summary>
    /// Gets or sets the visibility mode for the system panel.
    /// </summary>
    [ObservableProperty] public partial SystemPanelVisibilityMode VisibilityMode { get; set; }

    /// <summary>
    /// Gets or sets the size value. Height for horizontal panels, width for vertical panels.
    /// </summary>
    [ObservableProperty] public partial int Size { get; set; }

    /// <summary>
    /// Gets or sets whether the panel is currently in floating state.
    /// </summary>
    /// <remarks>
    /// This property is to be used in XAML to render the different states of the panel.
    /// </remarks>
    [ObservableProperty] private partial bool IsFloating { get; set; }

    /// <summary>
    /// Gets the current IslandsWindow instance associated with this object.
    /// </summary>
    public IslandsWindow? Window { get; private set; }

    // -----------------
    // Private variables
    // -----------------

    /// <summary>
    /// Specifies the timer used for proximity monitoring.
    /// </summary>
    private DispatcherTimer? _proximityTimer;

    /// <summary>
    /// Specifies whether the panel is currently hidden.
    /// </summary>
    private bool _isHidden;

    /// <summary>
    /// Specifies whether the mouse is currently within the panel area.
    /// </summary>
    private bool _isMouseInPanel;

    /// <summary>
    /// Specifies the last time the mouse left the panel area.
    /// </summary>
    private DateTime _mouseLeftPanelTime = DateTime.MinValue;

    /// <summary>
    /// Specifies the last time the mouse was near the screen edge.
    /// </summary>
    private DateTime _mouseNearEdgeTime = DateTime.MinValue;

    /// <summary>
    /// Specifies whether the mouse was near the screen edge during the last check.
    /// </summary>
    private bool _wasMouseNearEdge;

    /// <summary>
    /// Specifies the cached visible work area of the panel in logical pixels.
    /// </summary>
    private RECT _logicalPanelVisibleWorkArea;

    /// <summary>
    /// Specifies the last time the panel was revealed.
    /// </summary>
    private DateTime _lastRevealTime = DateTime.MinValue;

    /// <summary>
    /// Specifies whether a window is currently nearby for floating behavior.
    /// </summary>
    private bool _isWindowNearbyForFloating;

    /// <summary>
    /// Specifies the last time the nearby window state changed for floating behavior.
    /// </summary>
    private DateTime _windowNearbyChangeTime = DateTime.MinValue;

    // ---------------------
    // Animations and delays
    // ---------------------

    /// <summary>
    /// Specifies the grace period, in milliseconds, before revealing content.
    /// </summary>
    private const int REVEAL_GRACE_PERIOD = 300;

    /// <summary>
    /// Specifies the delay before starting the floating animation, in milliseconds.
    /// </summary>
    private const int FLOATING_ANIMATION_DELAY = 200;

    /// <summary>
    /// Specifies the duration of the floating animation, in milliseconds.
    /// </summary>
    private const int FLOATING_ANIMATION_DURATION = 150;

    /// <summary>
    /// Specifies the interval, in milliseconds, at which proximity checks are performed.
    /// </summary>
    private const int PROXIMITY_CHECK_INTERVAL = 25;

    /// <summary>
    /// Specifies the duration of the show/hide animation, in milliseconds.
    /// </summary>
    private const int SHOW_HIDE_ANIMATION_DURATION = 350;

    /// <summary>
    /// Specifies the minimum number of pixels the mouse must move before a reveal action is triggered.
    /// </summary>
    /// <remarks>This threshold helps prevent accidental reveals due to minor or unintentional mouse
    /// movements. Adjust this value to fine-tune the sensitivity of mouse-based reveal interactions.</remarks>
    private const int MOUSE_REVEAL_THRESHOLD = 5;

    /// <summary>
    /// Specifies the distance the pointer needs to exceed in order for a window to be considered "nearby".
    /// </summary>
    private const int WINDOW_NEARBY_THRESHOLD = 50;

    /// <summary>
    /// Specifies the delay, in milliseconds, after the mouse leaves the panel before hiding it.
    /// </summary>
    private const int MOUSE_LEAVE_DELAY = 500;

    /// <summary>
    /// Specifies the delay, in milliseconds, after the mouse approaches the screen edge before revealing the panel.
    /// </summary>
    private const int MOUSE_REVEAL_DELAY = 500;

    /// <summary>
    /// Specifies the margin size, in logical pixels, applied to the panel in floating mode.
    /// </summary>
    private const int FLOAT_MARGIN = 8;

    /// <summary>
    /// Specifies the delay, in milliseconds, before revealing the panel in Hidden mode.
    /// </summary>
    private const int HIDDEN_REVEAL_DELAY = 300;

    /// <summary>
    /// Specifies the delay, in milliseconds, before hiding the panel in Hidden mode.
    /// </summary>
    private const int HIDDEN_HIDE_DELAY = 200;

    /// <summary>
    /// Specifies the target frames per second for animations.
    /// </summary>
    private const int ANIMATION_FPS = 120;

    /// <summary>
    /// Used to track state during window enumeration.
    /// </summary>
    private unsafe struct EnumWindowsState
    {
        public HWND PanelHwnd;
        public HMONITOR MonitorHandle;
        public RECT PanelRect;
        public SystemPanelPosition Position;
        public bool FoundNearbyWindow;
    }

    /// <summary>
    /// Configures the window to behave as a non-activating tool window and removes standard window borders and frames.
    /// </summary>
    /// <remarks>This method modifies the window's extended and standard styles to make it appear as a tool
    /// window that does not receive focus or appear in the taskbar. It also ensures the window remains topmost and does
    /// not display standard window decorations. Call this method after the window handle has been created.</remarks>
    unsafe void SetWindowStyles()
    {
        // Handle styles
        var style = GetWindowLongPtrW(Window!.Handle, GWL.GWL_STYLE);
        style &= ~WS.WS_BORDER;
        style &= ~WS.WS_THICKFRAME;
        style &= ~WS.WS_DLGFRAME;
        SetWindowLongPtrW(Window!.Handle, GWL.GWL_STYLE, style);

        // Handle extended styles
        var exStyle = GetWindowLongPtrW(Window!.Handle, GWL.GWL_EXSTYLE);
        exStyle |= WS.WS_EX_TOOLWINDOW;
        exStyle |= WS.WS_EX_NOACTIVATE;
        exStyle &= ~WS.WS_EX_APPWINDOW;
        SetWindowLongPtrW(Window!.Handle, GWL.GWL_EXSTYLE, exStyle);

        // Update window
        SetWindowPos(
            Window!.Handle,
            HWND.HWND_TOPMOST,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
        );
    }

    /// <summary>
    /// Registers the window as an application desktop toolbar (appbar) with the system shell.
    /// </summary>
    /// <remarks>After calling this method, the window is managed as an appbar by the system, which may affect
    /// its positioning and interaction with other appbars or the Windows taskbar. This method should be called before
    /// attempting to use appbar-specific functionality.</remarks>
    unsafe void RegisterAppBar()
    {
        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = Window!.Handle
        };
        SHAppBarMessage(ABM.ABM_NEW, &abd);
    }

    /// <summary>
    /// Initialize the layout for the current panel. Set its default size and position for
    /// the desired configuration.
    /// </summary>
    unsafe void ApplyLayout()
    {
        // Register app bar only for fixed panels
        if (VisibilityMode == SystemPanelVisibilityMode.AlwaysVisible)
            RegisterAppBar();

        // Obtain variables
        var workArea = Display.GetDisplayWorkArea(Window!.Handle);
        var scale = Display.GetScale(Window.Handle);

        // LOGICAL PIXELS
        // Visual thickness includes float margin
        int floatInflation = Floating ? (int)(16 * scale) : 0;
        int logicalVisualThickness = Size + floatInflation;

        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = Window.Handle
        };

        // PHYSICAL PIXELS
        int physicalThickness = (int)(Size * scale);

        // Set metrics
        switch (Position)
        {
            case SystemPanelPosition.Top:
                abd.uEdge = ABE_TOP;
                abd.rc.left = workArea.left;
                abd.rc.right = workArea.right;
                abd.rc.top = workArea.top;
                abd.rc.bottom = abd.rc.top + physicalThickness;
                break;

            case SystemPanelPosition.Bottom:
                abd.uEdge = ABE_BOTTOM;
                abd.rc.left = workArea.left;
                abd.rc.right = workArea.right;
                abd.rc.bottom = workArea.bottom;
                abd.rc.top = abd.rc.bottom - physicalThickness;
                break;

            case SystemPanelPosition.Left:
                abd.uEdge = ABE_LEFT;
                abd.rc.top = workArea.top;
                abd.rc.bottom = workArea.bottom;
                abd.rc.left = workArea.left;
                abd.rc.right = abd.rc.left + physicalThickness;
                break;

            case SystemPanelPosition.Right:
                abd.uEdge = ABE_RIGHT;
                abd.rc.top = workArea.top;
                abd.rc.bottom = workArea.bottom;
                abd.rc.right = workArea.right;
                abd.rc.left = abd.rc.right - physicalThickness;
                break;
        }

        // Apply app bar config
        if (VisibilityMode == SystemPanelVisibilityMode.AlwaysVisible)
        {
            SHAppBarMessage(ABM.ABM_QUERYPOS, &abd);
            SHAppBarMessage(ABM.ABM_SETPOS, &abd);
        }

        // Cache the visible work area
        _logicalPanelVisibleWorkArea = abd.rc;

        int physicalVisualThickness = (int)(logicalVisualThickness * scale);
        int windowX, windowY, windowWidth, windowHeight;

        // Calculate position based on reserved rect and visual thickness
        switch (Position)
        {
            case SystemPanelPosition.Top:
                windowX = (int)(abd.rc.left / scale);
                windowY = (int)(abd.rc.top / scale);
                windowWidth = (int)((abd.rc.right - abd.rc.left) / scale);
                windowHeight = logicalVisualThickness;
                break;

            case SystemPanelPosition.Bottom:
                windowX = (int)(abd.rc.left / scale);
                windowY = (int)((abd.rc.bottom - physicalVisualThickness) / scale);
                windowWidth = (int)((abd.rc.right - abd.rc.left) / scale);
                windowHeight = logicalVisualThickness;
                break;

            case SystemPanelPosition.Left:
                windowX = (int)(abd.rc.left / scale);
                windowY = (int)(abd.rc.top / scale);
                windowWidth = logicalVisualThickness;
                windowHeight = (int)((abd.rc.bottom - abd.rc.top) / scale);
                break;

            case SystemPanelPosition.Right:
                windowX = (int)((abd.rc.right - physicalVisualThickness) / scale);
                windowY = (int)(abd.rc.top / scale);
                windowWidth = logicalVisualThickness;
                windowHeight = (int)((abd.rc.bottom - abd.rc.top) / scale);
                break;

            default:
                windowX = (int)(abd.rc.left / scale);
                windowY = (int)(abd.rc.top / scale);
                windowWidth = (int)((abd.rc.right - abd.rc.left) / scale);
                windowHeight = (int)((abd.rc.bottom - abd.rc.top) / scale);
                break;
        }

        // Requires logical pixels
        Window.MoveAndResize(windowX, windowY, windowWidth, windowHeight);
    }

    /// <summary>
    /// Creates the panel window and sets up its properties and event handlers.
    /// </summary>
    public void Create()
    {
        Window = new IslandsWindow
        {
            IsPersistenceEnabled = false
        };
        Window.AppWindowInitialized += (_, _) =>
        {
            Window.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            Window.AppWindow?.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            SetWindowStyles();
            ApplyLayout();

            switch (VisibilityMode)
            {
                case SystemPanelVisibilityMode.AlwaysVisible:
                    if (Floating)
                        StartProximityMonitoring();
                    break;
                case SystemPanelVisibilityMode.AutoHide:
                case SystemPanelVisibilityMode.Hidden:
                    StartProximityMonitoring();
                    break;
            }
        };
        Window.XamlInitialized += (_, _) =>
        {
            var grid = new Grid()
            {
                Background = new CommunityToolkit.WinUI.Media.AcrylicBrush()
                {
                    BlurAmount = 32,
                    TintOpacity = 0.4,
                    TintColor = Colors.Black,
                    BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop
                }
            };
            if (Floating)
            {
                grid.Margin = new Thickness(FLOAT_MARGIN);
                grid.CornerRadius = new CornerRadius(8);
            }

            // Track mouse enter/leave events
            grid.PointerEntered += (s, e) => _isMouseInPanel = true;
            grid.PointerExited += (s, e) =>
            {
                _isMouseInPanel = false;
                _mouseLeftPanelTime = DateTime.Now;
            };

            Window.Content = grid;
        };
        Window.Create();
        Window.MakeWindowTransparent();
    }

    /// <summary>
    /// Evaluates the current window's proximity state and updates the system panel's visibility according to the
    /// configured visibility mode.
    /// </summary>
    /// <remarks>This method determines the appropriate visibility behavior for the system panel based on the
    /// current visibility mode and floating state. It should be called when the window's proximity or visibility
    /// context changes.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CheckWindowProximity()
    {
        switch (VisibilityMode)
        {
            case SystemPanelVisibilityMode.AlwaysVisible when Floating:
                await HandleFloatingAlwaysVisibleAsync().ConfigureAwait(false);
                return;

            case SystemPanelVisibilityMode.AlwaysVisible:
                return;

            case SystemPanelVisibilityMode.Hidden:
                await HandleHiddenModeAsync().ConfigureAwait(false);
                return;

            case SystemPanelVisibilityMode.AutoHide:
                await HandleAutoHideModeAsync().ConfigureAwait(false);
                return;
        }
    }

    /// <summary>
    /// Handles the logic for maintaining the floating window's always-visible state, including triggering animations
    /// when the proximity of other windows changes.
    /// </summary>
    /// <remarks>This method should be called when the floating window's visibility or proximity state may
    /// have changed. It ensures that any required animations are performed only after a specified delay, preventing
    /// rapid or unnecessary transitions.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleFloatingAlwaysVisibleAsync()
    {
        bool windowNearby = IsAnyWindowNearby();

        // Track state change timing
        if (windowNearby != _isWindowNearbyForFloating)
        {
            _windowNearbyChangeTime = DateTime.Now;
            _isWindowNearbyForFloating = windowNearby;
        }

        // Check if enough time has passed since state change
        bool shouldAnimate =
            (DateTime.Now - _windowNearbyChangeTime).TotalMilliseconds >= FLOATING_ANIMATION_DELAY;

        if (shouldAnimate)
        {
            await AnimateFloatingThicknessAsync(windowNearby).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Animates the floating panel's thickness to smoothly transition between its standard size and expanded size based
    /// on the proximity of a window.
    /// </summary>
    /// <remarks>This method updates the panel's visual style and thickness with a smooth ease-out animation.
    /// The animation is performed asynchronously and does not block the calling thread.</remarks>
    /// <param name="windowNearby">Indicates whether a window is nearby. If <see langword="true"/>, the panel animates to its standard size;
    /// otherwise, it expands to include additional margin.</param>
    /// <returns>A task that represents the asynchronous animation operation.</returns>
    private async Task AnimateFloatingThicknessAsync(bool windowNearby)
    {
        var scale = Display.GetScale(Window!.Handle);
        int totalMargin = FLOAT_MARGIN * 2;

        // Target: with window = size, without window = size + margin
        int targetThickness = windowNearby ? Size : Size + totalMargin;

        RECT currentRect;
        unsafe
        {
#pragma warning disable CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
            GetWindowRect(Window.Handle, &currentRect);
#pragma warning restore CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
        }

        // Compute current thickness based on position
        int currentThickness = Position switch
        {
            SystemPanelPosition.Top or SystemPanelPosition.Bottom =>
                (int)((currentRect.bottom - currentRect.top) / scale),
            SystemPanelPosition.Left or SystemPanelPosition.Right =>
                (int)((currentRect.right - currentRect.left) / scale),
            _ => Size
        };

        // Already at target, no animation needed
        if (Math.Abs(currentThickness - targetThickness) < 2)
            return;

        // Animation logic
        var startTime = DateTime.Now;
        var startThickness = currentThickness;
        var delta = targetThickness - startThickness;

        // Toggle visual style
        IsFloating = !windowNearby;

        while (true)
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / FLOATING_ANIMATION_DURATION, 1.0);
            var easedProgress = 1 - Math.Pow(1 - progress, 3); // Ease-out cubic
            var thickness = (int)Math.Round(startThickness + delta * easedProgress);

            UIThreadQueue.QueueAction(() =>
            {
                ApplyFloatingThickness(thickness, scale);
            });

            if (progress >= 1.0)
                return;

            await Task.Delay(1000 / ANIMATION_FPS).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies the specified logical thickness to the floating panel by adjusting its window position and size.
    /// </summary>
    /// <param name="logicalThickness">
    /// The desired thickness in logical pixels to apply to the panel.
    /// </param>
    /// <param name="scale">
    /// The current display scale factor used to convert logical pixels to physical pixels.
    /// </param>
    private unsafe void ApplyFloatingThickness(int logicalThickness, double scale)
    {
        // Use the reserved AppBar rect, not the work area
        RECT reservedRect = _logicalPanelVisibleWorkArea;

        int physicalThickness = (int)(logicalThickness * scale);

        int x, y, width, height;

        switch (Position)
        {
            case SystemPanelPosition.Top:
                x = reservedRect.left;
                y = reservedRect.top;
                width = reservedRect.right - reservedRect.left;
                height = physicalThickness;
                break;

            case SystemPanelPosition.Bottom:
                x = reservedRect.left;
                y = reservedRect.bottom - physicalThickness;
                width = reservedRect.right - reservedRect.left;
                height = physicalThickness;
                break;

            case SystemPanelPosition.Left:
                x = reservedRect.left;
                y = reservedRect.top;
                width = physicalThickness;
                height = reservedRect.bottom - reservedRect.top;
                break;

            case SystemPanelPosition.Right:
                x = reservedRect.right - physicalThickness;
                y = reservedRect.top;
                width = physicalThickness;
                height = reservedRect.bottom - reservedRect.top;
                break;

            default:
                return;
        }

        SetWindowPos(
            Window!.Handle,
            HWND.HWND_TOPMOST,
            x,
            y, 
            width, 
            height,
            SWP_NOACTIVATE
        );
    }

    /// <summary>
    /// Starts the proximity monitoring timer to periodically check for nearby windows and update the panel's visibility.
    /// </summary>
    private void StartProximityMonitoring()
    {
        UIThreadQueue.QueueAction(() =>
        {
            _proximityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PROXIMITY_CHECK_INTERVAL)
            };
            _proximityTimer.Tick += async (_, _) => await UIThreadQueue.QueueActionAsync(async () => await CheckWindowProximity().ConfigureAwait(false)).ConfigureAwait(false);
            _proximityTimer.Start();
        });
    }

    /// <summary>
    /// Checks if there are any visible windows near the panel's area on the same monitor.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if any visible windows are found near the panel; otherwise, <see langword="false"/>.
    /// </returns>
    private unsafe bool IsAnyWindowNearby()
    {
        RECT panelRect = _logicalPanelVisibleWorkArea;
        var monitorHandle = MonitorFromWindow(Window!.Handle, MONITOR.MONITOR_DEFAULTTONEAREST);

        EnumWindowsState state = new()
        {
            PanelHwnd = Window.Handle,
            MonitorHandle = monitorHandle,
            PanelRect = panelRect,
            Position = Position,
            FoundNearbyWindow = false
        };

        EnumWindows(&EnumWindowCallback, (LPARAM)(&state));

        return state.FoundNearbyWindow;
    }

    /// <summary>
    /// Enumeration callback to check each window's proximity to the panel.
    /// </summary>
    /// <param name="hwnd">
    /// The handle of the window being evaluated.
    /// </param>
    /// <param name="lParam">
    /// The pointer to the <see cref="EnumWindowsState"/> structure containing the panel's state.
    /// </param>
    /// <returns>
    /// <see cref="BOOL.TRUE"/> to continue enumeration; <see cref="BOOL.FALSE"/> to stop enumeration if a nearby window is found.
    /// </returns>
    [UnmanagedCallersOnly]
    private static unsafe BOOL EnumWindowCallback(HWND hwnd, LPARAM lParam)
    {
        // Retrieve the state from lParam
        var state = (EnumWindowsState*)lParam;

        // If the handle corresponds to the panel itself, skip it
        if (hwnd == state->PanelHwnd)
            return true;

        // Skip hidden or minimized windows
        if (!IsWindowVisible(hwnd))
            return true;

        if (IsIconic(hwnd))
            return true;

        // Check window extended styles
        var exStyle = GetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE);
        bool isToolWindow = (exStyle & WS.WS_EX_TOOLWINDOW) != 0;
        bool isAppWindow = (exStyle & WS.WS_EX_APPWINDOW) != 0;

        // Skip tool windows
        if (isToolWindow && !isAppWindow)
            return true;

        // Skip owned windows
        HWND owner = GetWindow(hwnd, GW_OWNER);
        if (owner != HWND.NULL && !isAppWindow)
            return true;

        // Retrieve the window title
        const int titleBufferSize = 256;
        char* titleBuffer = stackalloc char[titleBufferSize];
        int titleLength = GetWindowTextW(hwnd, titleBuffer, titleBufferSize);
        string title = new string(titleBuffer, 0, titleLength);

        // Retrieve the window class
        const int classBufferSize = 256;
        char* classBuffer = stackalloc char[classBufferSize];
        uint classLength = RealGetWindowClassW(hwnd, classBuffer, classBufferSize);
        string windowClass = new string(classBuffer, 0, (int)classLength);

        // Skip windows with no title and not an app window
        if (titleLength == 0 && !isAppWindow)
            return true;

        // Skip core windows (UWP)
        if (windowClass == "Windows.UI.Core.CoreWindow")
            return true;

        // Obtain the target window's size
        RECT windowRect;
        GetWindowRect(hwnd, &windowRect);

        // Handle UWP ApplicationFrameWindow
        if (windowClass == "ApplicationFrameWindow")
        {
            // UWP is weird
            if (windowRect.right - windowRect.left <= 0 ||
                windowRect.bottom - windowRect.top <= 0 ||
                windowRect.top < -30000)
            {
                return true;
            }
        }

        // For non-UWP windows, do basic validation too
        int windowWidth = windowRect.right - windowRect.left;
        int windowHeight = windowRect.bottom - windowRect.top;

        // If the window has no size, skip it
        if (windowWidth <= 0 || windowHeight <= 0)
            return true;

        // Knowing the window is top level and visible, check proximity
        if (IsWindowNearPanel(windowRect, state->PanelRect, state->Position))
        {
            state->FoundNearbyWindow = true;
            return false;
        }

        // No proximity detected, continue enumeration
        return true;
    }

    /// <summary>
    /// Calculates whether a given window is near the panel based on its position.
    /// </summary>
    /// <param name="windowRect">The given window's rect.</param>
    /// <param name="panelRect">The current panel's rect.</param>
    /// <param name="position">The position of the current panel.</param>
    /// <returns>
    /// <see langword="true"/> if the window is near the panel; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsWindowNearPanel(RECT windowRect, RECT panelRect, SystemPanelPosition position)
    {
        return position switch
        {
            SystemPanelPosition.Top => windowRect.top <= panelRect.bottom + WINDOW_NEARBY_THRESHOLD,
            SystemPanelPosition.Bottom => windowRect.bottom >= panelRect.top - WINDOW_NEARBY_THRESHOLD,
            SystemPanelPosition.Left => windowRect.left <= panelRect.right + WINDOW_NEARBY_THRESHOLD,
            SystemPanelPosition.Right => windowRect.right >= panelRect.left - WINDOW_NEARBY_THRESHOLD,
            _ => false,
        };
    }

    /// <summary>
    /// Checks if the mouse cursor is near the edge of the screen corresponding to the panel's position.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the mouse is near the edge; otherwise, <see langword="false"/>.
    /// </returns>
    private unsafe bool IsMouseNearEdge()
    {
        // Get cursor position on the screen
        POINT cursorPos;
        GetCursorPos(&cursorPos);

        // Get the monitor bounds
        var monitorRect = Display.GetDisplayArea(Window!.Handle);

        return Position switch
        {
            SystemPanelPosition.Top => cursorPos.y <= monitorRect.top + MOUSE_REVEAL_THRESHOLD,
            SystemPanelPosition.Bottom => cursorPos.y >= monitorRect.bottom - MOUSE_REVEAL_THRESHOLD,
            SystemPanelPosition.Left => cursorPos.x <= monitorRect.left + MOUSE_REVEAL_THRESHOLD,
            SystemPanelPosition.Right => cursorPos.x >= monitorRect.right - MOUSE_REVEAL_THRESHOLD,
            _ => false,
        };
    }

    /// <summary>
    /// Handles the logic for the Hidden visibility mode, revealing or hiding the panel based on mouse proximity to the screen edge.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task HandleHiddenModeAsync()
    {
        // Check mouse proximity to edge
        bool mouseNear = IsMouseNearEdge();

        // Track edge entry time
        if (mouseNear && !_wasMouseNearEdge)
            _mouseNearEdgeTime = DateTime.Now;
        else if (!mouseNear)
            _mouseNearEdgeTime = DateTime.MinValue;

        // Update last state
        _wasMouseNearEdge = mouseNear;

        // Evaluate timing conditions
        bool hasMouseBeenNearLongEnough =
            mouseNear &&
            (DateTime.Now - _mouseNearEdgeTime).TotalMilliseconds >= HIDDEN_REVEAL_DELAY;

        bool hasMouseLeftLongEnough =
            !_isMouseInPanel &&
            !mouseNear &&
            (DateTime.Now - _mouseLeftPanelTime).TotalMilliseconds >= HIDDEN_HIDE_DELAY;

        // Reveal
        if (hasMouseBeenNearLongEnough && _isHidden)
        {
            _isHidden = false;
            _lastRevealTime = DateTime.Now;
            await AnimatePositionAsync(0).ConfigureAwait(false);
            return;
        }

        // Hide
        if (hasMouseLeftLongEnough && !_isHidden)
        {
            _isHidden = true;

            // Reset intent state
            _mouseNearEdgeTime = DateTime.MinValue;
            _wasMouseNearEdge = false;

            var scale = Display.GetScale(Window!.Handle);
            int floatInflation = Floating ? (int)(16 * scale) : 0;
            int thickness = (int)(Size * scale) + floatInflation;

            await AnimatePositionAsync(thickness).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the logic for the AutoHide visibility mode, revealing or hiding the panel based on mouse proximity and window presence.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task HandleAutoHideModeAsync()
    {
        // Mouse proximity check
        bool mouseNear = IsMouseNearEdge();
        bool windowNearby = IsAnyWindowNearby();

        // Edge timing
        if (mouseNear && !_wasMouseNearEdge)
            _mouseNearEdgeTime = DateTime.Now;
        else if (!mouseNear)
            _mouseNearEdgeTime = DateTime.MinValue;

        // Update last state
        _wasMouseNearEdge = mouseNear;

        // Evaluate timing conditions
        bool hasMouseBeenNearLongEnough =
            mouseNear &&
            (DateTime.Now - _mouseNearEdgeTime).TotalMilliseconds >= MOUSE_REVEAL_DELAY;

        bool hasMouseLeftLongEnough =
            !_isMouseInPanel &&
            (DateTime.Now - _mouseLeftPanelTime).TotalMilliseconds >= MOUSE_LEAVE_DELAY;

        bool isInRevealGracePeriod =
            (DateTime.Now - _lastRevealTime).TotalMilliseconds < REVEAL_GRACE_PERIOD;

        // Determine actions
        bool shouldReveal =
            hasMouseBeenNearLongEnough ||
            _isMouseInPanel ||
            !windowNearby;

        bool shouldHide =
            windowNearby &&
            !mouseNear &&
            !_isMouseInPanel &&
            hasMouseLeftLongEnough &&
            !isInRevealGracePeriod;

        // Execute actions
        if (shouldHide && !_isHidden)
            await AnimateHideAsync().ConfigureAwait(false);
        else if (shouldReveal && _isHidden)
            await AnimateShowAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Animates the panel to show itself by sliding into view.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task AnimateShowAsync()
    {
        // Update state
        _isHidden = false;
        _lastRevealTime = DateTime.Now;

        // Animate to visible position
        await AnimatePositionAsync(0).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the current offset of the panel from its hidden position based on its RECT.
    /// </summary>
    /// <param name="rect">
    /// The RECT structure representing the panel's current position and size.
    /// </param>
    /// <returns>
    /// An integer representing the current offset in logical pixels.
    /// </returns>
    private int GetCurrentOffset(RECT rect)
    {
        // Get work area and scale
        var workArea = Display.GetDisplayWorkArea(Window!.Handle);
        var scale = Display.GetScale(Window.Handle);

        // Calculate offset based on position
        return Position switch
        {
            SystemPanelPosition.Top => (int)((workArea.top - rect.top) / scale),
            SystemPanelPosition.Bottom => (int)((rect.bottom - workArea.bottom) / scale),
            SystemPanelPosition.Left => (int)((workArea.left - rect.left) / scale),
            SystemPanelPosition.Right => (int)((rect.right - workArea.right) / scale),
            _ => 0,
        };
    }

    /// <summary>
    /// Animates the panel to hide itself by sliding out of view.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task AnimateHideAsync()
    {
        _isHidden = true;

        // Reset mouse intent tracking
        _mouseNearEdgeTime = DateTime.MinValue;
        _wasMouseNearEdge = false;

        // Get target offset
        var scale = Display.GetScale(Window!.Handle);
        int floatInflation = Floating ? (int)(16 * scale) : 0;
        int thickness = (int)(Size * scale) + floatInflation;

        // Animate to hidden position
        await AnimatePositionAsync(thickness).ConfigureAwait(false);
    }

    /// <summary>
    /// Animates the panel's position to the specified target offset.
    /// </summary>
    /// <param name="targetOffset">
    /// The target offset in logical pixels to animate to.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    private async Task AnimatePositionAsync(int targetOffset)
    {
        // Get the starting offset and scale
        var startTime = DateTime.Now;
        var scale = Display.GetScale(Window!.Handle);

        //Get current rect
        RECT currentRect;
        unsafe
        {
#pragma warning disable CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
            GetWindowRect(Window.Handle, &currentRect);
#pragma warning restore CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
        }

        // Compute starting offset
        var startOffset = GetCurrentOffset(currentRect);
        var delta = targetOffset - startOffset;

        // Animation logic
        while (true)
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / SHOW_HIDE_ANIMATION_DURATION, 1.0);
            var easedProgress = 1 - Math.Pow(1 - progress, 3);
            var currentOffset = startOffset + delta * easedProgress;

            UIThreadQueue.QueueAction(() =>
            {
                ApplyOffset((int)Math.Round(currentOffset), scale);
            });

            if (progress >= 1.0)
                return;

            await Task.Delay(1000 / ANIMATION_FPS).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies the specified offset to the panel's position based on its visibility progress.
    /// </summary>
    /// <param name="offset">
    /// The offset in logical pixels to apply to the panel's position.
    /// </param>
    /// <param name="scale">
    /// The current display scale factor used to convert logical pixels to physical pixels.
    /// </param>
    private unsafe void ApplyOffset(int offset, double scale)
    {
        // Get monitor area and work area
        var area = Display.GetDisplayArea(Window!.Handle);
        var workArea = Display.GetDisplayWorkArea(Window!.Handle);

        // Visual thickness includes float margin
        int floatInflation = Floating ? (int)(16 * scale) : 0;
        int logicalThickness = Size + floatInflation;
        int physicalThickness = (int)(logicalThickness * scale);

        // Calculate the gap between work area and monitor edge (already in physical pixels)
        int workAreaOffsetLeft = workArea.left - area.left;
        int workAreaOffsetTop = workArea.top - area.top;
        int workAreaOffsetRight = area.right - workArea.right;
        int workAreaOffsetBottom = area.bottom - workArea.bottom;

        int x = 0, y = 0, width = 0, height = 0;

        // offset is in logical pixels from AnimateHideAsync, convert to physical for progress calc
        double hideProgress = logicalThickness > 0 ? offset / (double)logicalThickness : 0;
        int offsetPhysical = (int)(offset * scale);

        switch (Position)
        {
            case SystemPanelPosition.Top:
                x = workArea.left;
                y = workArea.top - offsetPhysical - (int)(workAreaOffsetTop * hideProgress);
                width = workArea.right - workArea.left;
                height = physicalThickness;
                break;

            case SystemPanelPosition.Bottom:
                x = workArea.left;
                y = workArea.bottom - physicalThickness + offsetPhysical + (int)(workAreaOffsetBottom * hideProgress);
                width = workArea.right - workArea.left;
                height = physicalThickness;
                break;

            case SystemPanelPosition.Left:
                x = workArea.left - offsetPhysical - (int)(workAreaOffsetLeft * hideProgress);
                y = workArea.top;
                width = physicalThickness;
                height = workArea.bottom - workArea.top;
                break;

            case SystemPanelPosition.Right:
                x = workArea.right - physicalThickness + offsetPhysical + (int)(workAreaOffsetRight * hideProgress);
                y = workArea.top;
                width = physicalThickness;
                height = workArea.bottom - workArea.top;
                break;
        }

        // Apply new position
        SetWindowPos(
            Window.Handle,
            HWND.HWND_TOPMOST,
            x,
            y, 
            width,
            height,
            SWP_NOACTIVATE
        );
    }

    /// <summary>
    /// Disposes of the system panel, stopping any active timers and releasing resources.
    /// </summary>
    public void Dispose()
    {
        _proximityTimer?.Stop();
        _proximityTimer = null;
    }
}

public class SystemPanelsService
{
    public ObservableCollection<SystemPanel> PanelItems { get; } = [];

    public void Initialize()
    {
        foreach (var panel in PanelItems)
            SpawnPanel(panel);

        //PanelItems.CollectionChanged += OnPanelsChanged;
    }

    static void SpawnPanel(SystemPanel panel)
    {
        UIThreadQueue.QueueAction(panel.Create);
    }
}