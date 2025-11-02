// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Win32;
using WinRT;
using static System.Net.Mime.MediaTypeNames;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using BOOL = TerraFX.Interop.Windows.BOOL;
using HWND = TerraFX.Interop.Windows.HWND;
using LPARAM = TerraFX.Interop.Windows.LPARAM;
using LRESULT = TerraFX.Interop.Windows.LRESULT;
using WPARAM = TerraFX.Interop.Windows.WPARAM;

#pragma warning disable CA1515 // Consider making public types internal
#pragma warning disable CA1416 // Validate platform compatibility

namespace Rebound.Core.UI;

// ============================================================================
// ENUMS
// ============================================================================

public enum DialogIcon
{
    None,
    Info,
    Warning,
    Error,
    Shield
}

// ============================================================================
// EVENT ARGS
// ============================================================================

public class IslandsWindowClosingEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class XamlInitializedEventArgs : EventArgs
{
}

public class AppWindowInitializedEventArgs : EventArgs
{
}

// ============================================================================
// WINDOW LIST MANAGER
// ============================================================================

public static class _windowList
{
    public static readonly List<IslandsWindow> _openWindows = [];
    public static bool KeepAlive = false;

    public static void RegisterWindow(IslandsWindow window)
    {
        _openWindows.Add(window);
        window.Closed += (s, e) =>
        {
            _openWindows.Remove(window);
            if (_openWindows.Count == 0 && !KeepAlive)
            {
                Windows.UI.Xaml.Application.Current.Exit();
                Process.GetCurrentProcess().Kill();
            }
        };
    }
}

// ============================================================================
// REBOUND DIALOG
// ============================================================================

public sealed class ReboundDialog : IslandsWindow
{
    private TaskCompletionSource<bool> _tcs = new();

    public static async Task ShowAsync(string title, string message, DialogIcon icon = DialogIcon.Info)
    {
        using var dlg = new ReboundDialog(title, message, icon);
        dlg.Create();
        await dlg._tcs.Task.ConfigureAwait(false);
    }

    public ReboundDialog(string title, string message, DialogIcon icon)
    {
        Title = title;
        IsPersistenceEnabled = false;

        XamlInitialized += (_, _) =>
        {
            var page = new Page();
            BackdropMaterial.SetApplyToRootOrPageBackground(page, true);

            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                CornerRadius = new CornerRadius(8)
            };

            // Title bar
            var titleBar = new Border
            {
                Padding = new Thickness(12, 8, 0, 0),
            };

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            titleBar.Child = titleText;
            rootGrid.Children.Add(titleBar);

            // Content area
            var contentGrid = new Grid
            {
                Padding = new Thickness(20, 20, 20, 0),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            Grid.SetRow(contentGrid, 1);

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var iconImg = new Windows.UI.Xaml.Controls.Image
            {
                Source = LoadSystemIcon(icon),
                Width = 48,
                Height = 48,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            stack.Children.Add(iconImg);

            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            textStack.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 320
            });

            stack.Children.Add(textStack);
            contentGrid.Children.Add(stack);

            // Footer bar
            var footerBar = new Border
            {
                Background = (Brush)Windows.UI.Xaml.Application.Current.Resources["SystemControlBackgroundAltMediumLowBrush"],
                Padding = new Thickness(24),
                Margin = new(-20, 0, -20, 0)
            };
            Grid.SetRow(footerBar, 3);

            var footerGrid = new Grid();
            var okButton = new Button
            {
                Content = "OK",
                Style = (Style)Windows.UI.Xaml.Application.Current.Resources["AccentButtonStyle"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 100
            };

            okButton.Click += (_, _) =>
            {
                _tcs.TrySetResult(true);
                Close();
            };

            footerGrid.Children.Add(okButton);
            footerBar.Child = footerGrid;
            contentGrid.Children.Add(footerBar);

            rootGrid.Children.Add(contentGrid);

            page.Content = rootGrid;
            Content = page;
        };

        AppWindowInitialized += (_, _) =>
        {
            Title = title;
            AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow?.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
            Width = 480;
            Height = 256;
            IsMaximizable = false;
            IsMinimizable = false;
            IsResizable = false;
            CenterWindow();
        };
    }

    private static unsafe BitmapImage? LoadSystemIcon(DialogIcon icon)
    {
        string dll = System.Environment.SystemDirectory + "\\imageres.dll";
        int iconIndex = icon switch
        {
            DialogIcon.Warning => 79,
            DialogIcon.Error => 98,
            DialogIcon.Info => 81,
            DialogIcon.Shield => 1028,
            _ => 277
        };

        Windows.Win32.UI.WindowsAndMessaging.HICON largeIcon = default;
        fixed (char* dllPtr = dll)
        {
            PInvoke.ExtractIconEx(new(dllPtr), iconIndex, &largeIcon, null, 1);
        }

        Windows.Win32.UI.WindowsAndMessaging.ICONINFO iconInfo;
        if (!PInvoke.GetIconInfo(largeIcon, &iconInfo))
            return null;

        Windows.Win32.Graphics.Gdi.BITMAP bmp;
        if (PInvoke.GetObject(iconInfo.hbmColor, sizeof(Windows.Win32.Graphics.Gdi.BITMAP), &bmp) == 0)
            return null;

        int width = bmp.bmWidth;
        int height = Math.Abs(bmp.bmHeight);

        var hdcScreen = PInvoke.GetDC(new());
        var hdcMem = PInvoke.CreateCompatibleDC(hdcScreen);
        var hBitmap = PInvoke.CreateCompatibleBitmap(hdcScreen, width, height);
        var old = PInvoke.SelectObject(hdcMem, hBitmap);

        PInvoke.DrawIconEx(hdcMem, 0, 0, largeIcon, width, height, 0, new(), Windows.Win32.UI.WindowsAndMessaging.DI_FLAGS.DI_NORMAL);

        int bytesPerPixel = 4;
        int stride = width * bytesPerPixel;
        int bufferSize = stride * height;
        byte[] pixelData = new byte[bufferSize];

        fixed (byte* pPixels = pixelData)
        {
            Windows.Win32.Graphics.Gdi.BITMAPINFO bmi = new()
            {
                bmiHeader =
                {
                    biSize = (uint)sizeof(Windows.Win32.Graphics.Gdi.BITMAPINFOHEADER),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = (uint)BI.BI_RGB
                }
            };

            _ = PInvoke.GetDIBits(hdcMem, hBitmap, 0, (uint)height, pPixels, &bmi, Windows.Win32.Graphics.Gdi.DIB_USAGE.DIB_RGB_COLORS);
        }

        PInvoke.SelectObject(hdcMem, old);
        PInvoke.DeleteDC(hdcMem);
        _ = PInvoke.ReleaseDC(new(), hdcScreen);
        PInvoke.DeleteObject(hBitmap);
        PInvoke.DeleteObject(iconInfo.hbmColor);
        PInvoke.DeleteObject(iconInfo.hbmMask);
        PInvoke.DestroyIcon(largeIcon);

        using var stream = new InMemoryRandomAccessStream();
        var encoder = BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).AsTask().Result;
        {
            encoder.SetPixelData(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
                (uint)width,
                (uint)height,
                96, 96,
                pixelData
            );
            encoder.FlushAsync().AsTask().Wait();
        }

        var image = new BitmapImage();
        stream.Seek(0);
        image.SetSource(stream);

        return image;
    }
}

// ============================================================================
// ISLANDS WINDOW
// ============================================================================

public partial class IslandsWindow : ObservableObject, IDisposable
{
    // ------------------------------------------------------------------------
    // STATIC FIELDS
    // ------------------------------------------------------------------------

    private const string WindowClassName = "XamlIslandsClass";
    private static readonly ushort _classAtom;
    private static WindowsXamlManager? _xamlManager;

    // ------------------------------------------------------------------------
    // INSTANCE FIELDS - State
    // ------------------------------------------------------------------------

    public bool _closed { get; private set; }
    private bool _disposed;
    private volatile bool _xamlInitialized;
    private bool _internalResize;
    private bool _isInitializing;
    private bool _boundsApplied;

    // ------------------------------------------------------------------------
    // INSTANCE FIELDS - Handles
    // ------------------------------------------------------------------------

    private GCHandle _thisHandle;
    private HWND _xamlHwnd;
    private HWND _coreHwnd;

    // ------------------------------------------------------------------------
    // INSTANCE FIELDS - WinRT Objects
    // ------------------------------------------------------------------------

    private DesktopWindowXamlSource? _desktopWindowXamlSource;
    private CoreWindow? _coreWindow;

    // ------------------------------------------------------------------------
    // INSTANCE FIELDS - COM Pointers
    // ------------------------------------------------------------------------

    private TerraFX.Interop.Windows.ComPtr<IDesktopWindowXamlSourceNative2> _nativeSource;
    private TerraFX.Interop.Windows.ComPtr<ICoreWindowInterop> _coreWindowInterop;

    // ------------------------------------------------------------------------
    // INSTANCE FIELDS - Window Bounds
    // ------------------------------------------------------------------------

    private WindowBounds _pendingBounds = new();
    private WindowBounds _lastNormalBounds = new(); // In-memory cache for normal state bounds

    // ------------------------------------------------------------------------
    // PUBLIC PROPERTIES - Window Handles
    // ------------------------------------------------------------------------

    public HWND Handle { get; private set; }
    public AppWindow? AppWindow { get; private set; }

    // ------------------------------------------------------------------------
    // PUBLIC PROPERTIES - Observable (Window Capabilities)
    // ------------------------------------------------------------------------

    [ObservableProperty] public partial bool IsMaximizable { get; set; } = true;
    [ObservableProperty] public partial bool IsMinimizable { get; set; } = true;
    [ObservableProperty] public partial bool IsResizable { get; set; } = true;

    // ------------------------------------------------------------------------
    // PUBLIC PROPERTIES - Observable (Size Constraints)
    // ------------------------------------------------------------------------

    [ObservableProperty] public partial int MinWidth { get; set; } = 0;
    [ObservableProperty] public partial int MinHeight { get; set; } = 0;
    [ObservableProperty] public partial int MaxWidth { get; set; } = int.MaxValue;
    [ObservableProperty] public partial int MaxHeight { get; set; } = int.MaxValue;

    // ------------------------------------------------------------------------
    // PUBLIC PROPERTIES - Observable (Window State)
    // ------------------------------------------------------------------------

    [ObservableProperty] public partial string Title { get; set; } = "UWP XAML Islands Window";
    [ObservableProperty] public partial UIElement? Content { get; set; }
    [ObservableProperty] public partial int Width { get; set; } = 800;
    [ObservableProperty] public partial int Height { get; set; } = 600;
    [ObservableProperty] public partial int X { get; set; } = -1;
    [ObservableProperty] public partial int Y { get; set; } = -1;

    // ------------------------------------------------------------------------
    // PUBLIC PROPERTIES - Observable (Persistence)
    // ------------------------------------------------------------------------

    [ObservableProperty] public partial bool IsPersistenceEnabled { get; set; } = false;
    [ObservableProperty] public partial string PersistenceKey { get; set; } = nameof(IslandsWindow);
    [ObservableProperty] public partial string PersistanceFileName { get; set; } = "rebound";

    // ------------------------------------------------------------------------
    // EVENTS
    // ------------------------------------------------------------------------

    public event EventHandler<XamlInitializedEventArgs>? XamlInitialized;
    public event EventHandler<AppWindowInitializedEventArgs>? AppWindowInitialized;
    public event EventHandler<IslandsWindowClosingEventArgs>? Closing;
    public event EventHandler? Closed;

    // ------------------------------------------------------------------------
    // NESTED CLASSES
    // ------------------------------------------------------------------------

    private class WindowBounds
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
    }

    private enum WindowDisplayState
    {
        Normal = 0,
        Maximized = 1,
        Minimized = 2,
        Fullscreen = 3
    }

    // ------------------------------------------------------------------------
    // STATIC CONSTRUCTOR
    // ------------------------------------------------------------------------

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static IslandsWindow()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        unsafe
        {
            fixed (char* className = WindowClassName)
            {
                WNDCLASSW wc = new()
                {
                    lpfnWndProc = &WndProcStatic,
                    lpszClassName = className,
                    hInstance = GetModuleHandleW(null)
                };

                _classAtom = RegisterClassW(&wc);
            }
        }

        unsafe
        {
            InternalLoadLibrary("twinapi.appcore.dll");
            InternalLoadLibrary("threadpoolwinrt.dll");
        }

        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    // ------------------------------------------------------------------------
    // STATIC METHODS
    // ------------------------------------------------------------------------

    [UnmanagedCallersOnly]
    private static unsafe LRESULT WndProcStatic(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        var ptr = GetWindowLongPtr(hwnd, GWLP.GWLP_USERDATA);
        if (ptr == IntPtr.Zero)
            return DefWindowProc(hwnd, msg, wParam, lParam);

        var handle = GCHandle.FromIntPtr(ptr);
        var window = (IslandsWindow)handle.Target!;
        return window.WndProc(hwnd, msg, wParam, lParam);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InternalLoadLibrary(string lib)
    {
        fixed (char* libName = lib)
            LoadLibraryW(libName);
    }

    // ------------------------------------------------------------------------
    // CONSTRUCTOR
    // ------------------------------------------------------------------------

    public IslandsWindow() { }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Window Lifecycle
    // ------------------------------------------------------------------------

    public unsafe void Create()
    {
        ThrowIfDisposed();

        _isInitializing = true;
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        IntPtr gcPtr = GCHandle.ToIntPtr(_thisHandle);

        // Load persistence BEFORE storing pending bounds
        WindowDisplayState targetDisplayState = WindowDisplayState.Normal;

        if (IsPersistenceEnabled)
        {
            var persistedBounds = LoadPersistedBounds();
            targetDisplayState = (WindowDisplayState)SettingsManager.GetValue($"{PersistenceKey}_DisplayState", PersistanceFileName, 0);

            // Only apply persisted bounds if they exist (not first-time launch)
            if (persistedBounds.Width > 0 && persistedBounds.Height > 0)
            {
                Width = persistedBounds.Width;
                Height = persistedBounds.Height;
                X = persistedBounds.X;
                Y = persistedBounds.Y;
            }
            else
            {
                // First-time launch: use Windows default position (top-left)
                X = 0;
                Y = 0;
            }
        }

        // Initialize last normal bounds with current values
        _lastNormalBounds.X = X;
        _lastNormalBounds.Y = Y;
        _lastNormalBounds.Width = Width;
        _lastNormalBounds.Height = Height;

        _pendingBounds.X = X;
        _pendingBounds.Y = Y;
        _pendingBounds.Width = Width;
        _pendingBounds.Height = Height;

        fixed (char* className = WindowClassName)
        fixed (char* windowName = Title)
        {
            Handle = CreateWindowExW(
                WS_EX_NOREDIRECTIONBITMAP | WS_EX_APPWINDOW,
                className,
                windowName,
                0,
                CW_USEDEFAULT, CW_USEDEFAULT,
                CW_USEDEFAULT, CW_USEDEFAULT,
                HWND.NULL,
                HMENU.NULL,
                GetModuleHandleW(null),
                null);

            SetWindowLongPtrW(Handle, GWLP.GWLP_USERDATA, gcPtr);

            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindow.Closing += OnAppWindowClosing;
            AppWindow.Changed += OnAppWindowChanged;

            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            InitializeXaml();

            SetWindowLongPtrW(Handle, GWL.GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
            SetWindowPos(Handle, HWND.HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            _isInitializing = false;

            ApplyPendingBounds();

            // Apply display state after bounds
            if (targetDisplayState == WindowDisplayState.Maximized)
            {
                ShowWindow(Handle, SW.SW_SHOWMAXIMIZED);
            }
            else
            {
                ShowWindow(Handle, SW.SW_SHOWNORMAL);
            }

            BringWindowToTop(Handle);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);
            SetFocus(Handle);

            _windowList.RegisterWindow(this);
        }
    }

    public unsafe void InitializeXaml()
    {
        ThrowIfDisposed();

        if (_xamlInitialized) return;

        _desktopWindowXamlSource = new DesktopWindowXamlSource();

        var raw = ((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr);
        ThrowIfFailed(raw->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)_nativeSource.GetAddressOf()));

        ThrowIfFailed(_nativeSource.Get()->AttachToWindow(Handle));
        ThrowIfFailed(_nativeSource.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd)));

        RECT clientRect;
        GetClientRect(Handle, &clientRect);
        var clientWidth = clientRect.right - clientRect.left;
        var clientHeight = clientRect.bottom - clientRect.top;
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, clientWidth, clientHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        _coreWindow = CoreWindow.GetForCurrentThread();
        var coreRaw = ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr);
        ThrowIfFailed(coreRaw->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)_coreWindowInterop.GetAddressOf()));
        ThrowIfFailed(_coreWindowInterop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd)));

        _xamlInitialized = true;
        XamlInitialized?.Invoke(this, new XamlInitializedEventArgs());
        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

        using var themeListener = new ThemeListener();
        themeListener.ThemeChanged += (args) =>
        {
            unsafe
            {
                int darkMode = args.CurrentTheme == ApplicationTheme.Dark ? 1 : 0;
                DwmSetWindowAttribute(Handle,
                    (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    &darkMode, sizeof(int));
            }
        };

        unsafe
        {
            var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &backdrop, sizeof(int));
            int dark = themeListener.CurrentTheme == ApplicationTheme.Light ? 0 : 1;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &dark, sizeof(int));
        }
    }

    public void Close()
    {
        if (_closed) return;

        var args = new IslandsWindowClosingEventArgs();
        Closing?.Invoke(this, args);
        if (args.Handled) return;

        SaveWindowState();
        _closed = true;

        if (AppWindow != null)
        {
            AppWindow.Closing -= OnAppWindowClosing;
        }

        Dispose();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (AppWindow != null)
        {
            AppWindow.Closing -= OnAppWindowClosing;
            AppWindow.Changed -= OnAppWindowChanged;
        }

        if (_thisHandle.IsAllocated)
        {
            SetWindowLongPtrW(Handle, GWLP.GWLP_USERDATA, IntPtr.Zero);
            _thisHandle.Free();
        }

        _nativeSource.Dispose();
        _nativeSource = default;
        _coreWindowInterop.Dispose();
        _coreWindowInterop = default;

        _desktopWindowXamlSource?.Dispose();
        _coreWindow = null;

        if (AppWindow != null)
        {
            AppWindow.Destroy();
            AppWindow = null;
        }

        if (Handle != HWND.NULL)
        {
            DestroyWindow(Handle);
            Handle = HWND.NULL;
        }

        GC.SuppressFinalize(this);
    }

    ~IslandsWindow()
    {
        Dispose();
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Window Operations
    // ------------------------------------------------------------------------

    public void Activate()
    {
        if (AppWindow != null)
        {
            if (IsIconic(Handle) != 0)
            {
                ShowWindow(Handle, SW.SW_RESTORE);
            }
            SetForegroundWindow(Handle);
        }
        else
        {
            Create();
        }
    }

    public void Minimize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Minimize();
    }

    public void Maximize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Maximize();
    }

    public void Restore()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Restore();
    }

    public void BringToFront()
    {
        TerraFX.Interop.Windows.Windows.ShowWindow(Handle, SW.SW_SHOW);
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(Handle);
        TerraFX.Interop.Windows.Windows.SetActiveWindow(Handle);
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Window Positioning
    // ------------------------------------------------------------------------

    public void MoveAndResize(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void Move(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void CenterWindow()
    {
        if (AppWindow == null) return;

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var size = AppWindow.Size;

        int centerX = workArea.X + (workArea.Width - size.Width) / 2;
        int centerY = workArea.Y + (workArea.Height - size.Height) / 2;

        AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));

        var scale = Display.GetScale(Handle);
        X = (int)(centerX / scale);
        Y = (int)(centerY / scale);
    }

    public unsafe void ApplyPendingBounds()
    {
        if (AppWindow == null) return;

        _boundsApplied = true;

        var scale = Display.GetScale(Handle);

        _internalResize = true;
        try
        {
            // Convert logical to physical
            int physicalWidth = (int)(_pendingBounds.Width * scale);
            int physicalHeight = (int)(_pendingBounds.Height * scale);

            // Resize first
            AppWindow.Resize(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

            // Then position (or center if default)
            if (_pendingBounds.X >= 0 && _pendingBounds.Y >= 0)
            {
                int physicalX = (int)(_pendingBounds.X * scale);
                int physicalY = (int)(_pendingBounds.Y * scale);
                AppWindow.Move(new Windows.Graphics.PointInt32(physicalX, physicalY));

                X = _pendingBounds.X;
                Y = _pendingBounds.Y;
            }
            else
            {
                CenterWindow();
            }
        }
        finally
        {
            _internalResize = false;
        }
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Window Styling
    // ------------------------------------------------------------------------

    public void SetExtendedWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE style)
    {
        var exStyle = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE)GetWindowLongPtrW(Handle, GWL.GWL_EXSTYLE);
        exStyle |= style;
        SetWindowLongPtrW(Handle, GWL.GWL_EXSTYLE, (nint)exStyle);
        SetWindowPos(
            Handle,
            HWND.NULL,
            0, 0, 0, 0,
            SWP.SWP_NOMOVE | SWP.SWP_NOSIZE | SWP.SWP_NOZORDER | SWP.SWP_FRAMECHANGED
        );
    }

    public void SetWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE style)
    {
        var ws = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE)GetWindowLongPtrW(Handle, GWL.GWL_STYLE);
        ws |= style;
        SetWindowLongPtrW(Handle, GWL.GWL_STYLE, (nint)ws);
        SetWindowPos(
            Handle,
            HWND.NULL,
            0, 0, 0, 0,
            SWP.SWP_NOMOVE | SWP.SWP_NOSIZE | SWP.SWP_NOZORDER | SWP.SWP_FRAMECHANGED
        );
    }

    public void SetWindowOpacity(byte opacity)
    {
        SetExtendedWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED);
        SetLayeredWindowAttributes(Handle, 0, opacity, LWA.LWA_ALPHA);
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Message Handling
    // ------------------------------------------------------------------------

    public unsafe bool PreTranslateMessage(MSG* msg)
    {
        if (!_xamlInitialized) return false;
        BOOL outResult = false;
        if (!_closed && _nativeSource.Get() != null)
            ThrowIfFailed(_nativeSource.Get()->PreTranslateMessage(msg, &outResult));
        return outResult;
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS - Persistence
    // ------------------------------------------------------------------------

    public void LoadWindowState()
    {
        if (!IsPersistenceEnabled) return;

        int persistedX = SettingsManager.GetValue($"{PersistenceKey}_X", PersistanceFileName, X);
        int persistedY = SettingsManager.GetValue($"{PersistenceKey}_Y", PersistanceFileName, Y);
        int persistedWidth = SettingsManager.GetValue($"{PersistenceKey}_Width", PersistanceFileName, Width == 0 ? 800 : Width);
        int persistedHeight = SettingsManager.GetValue($"{PersistenceKey}_Height", PersistanceFileName, Height == 0 ? 600 : Height);

        if (persistedWidth > 0 && persistedHeight > 0)
        {
            X = persistedX;
            Y = persistedY;
            Width = persistedWidth;
            Height = persistedHeight;
        }
    }

    // ------------------------------------------------------------------------
    // PRIVATE METHODS - Persistence
    // ------------------------------------------------------------------------

    private WindowBounds LoadPersistedBounds()
    {
        return new WindowBounds
        {
            X = SettingsManager.GetValue($"{PersistenceKey}_X", PersistanceFileName, -1),
            Y = SettingsManager.GetValue($"{PersistenceKey}_Y", PersistanceFileName, -1),
            Width = SettingsManager.GetValue($"{PersistenceKey}_Width", PersistanceFileName, -1),
            Height = SettingsManager.GetValue($"{PersistenceKey}_Height", PersistanceFileName, -1)
        };
    }

    private WindowDisplayState GetCurrentDisplayState()
    {
        if (AppWindow?.Presenter is not OverlappedPresenter presenter)
            return WindowDisplayState.Normal;

        return presenter.State switch
        {
            OverlappedPresenterState.Maximized => WindowDisplayState.Maximized,
            OverlappedPresenterState.Minimized => WindowDisplayState.Minimized,
            _ => WindowDisplayState.Normal
        };
    }

    private unsafe void UpdateLastNormalBoundsFromHWND()
    {
        if (Handle == HWND.NULL || GetCurrentDisplayState() != WindowDisplayState.Normal)
            return;

        RECT rect;
        GetWindowRect(Handle, &rect);

        var scale = Display.GetScale(Handle);

        // Convert physical pixels to logical (DPI-independent)
        _lastNormalBounds.X = (int)((rect.left) / scale);
        _lastNormalBounds.Y = (int)((rect.top) / scale);
        _lastNormalBounds.Width = (int)((rect.right - rect.left) / scale);
        _lastNormalBounds.Height = (int)((rect.bottom - rect.top) / scale);
    }

    private void SaveWindowState()
    {
        if (!IsPersistenceEnabled || AppWindow == null) return;

        var currentState = GetCurrentDisplayState();

        // Always save display state
        SettingsManager.SetValue($"{PersistenceKey}_DisplayState", PersistanceFileName, (int)currentState);

        // Update last normal bounds one final time before saving (if in normal state)
        UpdateLastNormalBoundsFromHWND();

        // Save the cached normal bounds (in logical/DPI-independent pixels)
        SettingsManager.SetValue($"{PersistenceKey}_X", PersistanceFileName, _lastNormalBounds.X);
        SettingsManager.SetValue($"{PersistenceKey}_Y", PersistanceFileName, _lastNormalBounds.Y);
        SettingsManager.SetValue($"{PersistenceKey}_Width", PersistanceFileName, _lastNormalBounds.Width);
        SettingsManager.SetValue($"{PersistenceKey}_Height", PersistanceFileName, _lastNormalBounds.Height);
    }

    // ------------------------------------------------------------------------
    // PRIVATE METHODS - Event Handlers
    // ------------------------------------------------------------------------

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var closingArgs = new IslandsWindowClosingEventArgs();
        Closing?.Invoke(this, closingArgs);

        if (closingArgs.Handled)
        {
            args.Cancel = true;
            return;
        }

        SaveWindowState();

        _closed = true;
        Dispose();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        // When presenter changes (e.g., window is restored from maximized)
        if (args.DidPresenterChange && AppWindow?.Presenter is OverlappedPresenter presenter)
        {
            // When window is in Normal state, update the cached normal bounds
            if (presenter.State == OverlappedPresenterState.Restored && !_isInitializing && !_internalResize)
            {
                _lastNormalBounds.X = X;
                _lastNormalBounds.Y = Y;
                _lastNormalBounds.Width = Width;
                _lastNormalBounds.Height = Height;
            }
        }
    }

    // ------------------------------------------------------------------------
    // PRIVATE METHODS - Message Handling
    // ------------------------------------------------------------------------

    internal void OnResize(int physicalWidth, int physicalHeight)
    {
        if (_xamlHwnd != default)
        {
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, physicalWidth, physicalHeight,
                SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
        }

        if (_coreHwnd != default)
        {
            SendMessageW(_coreHwnd, WM_SIZE, (WPARAM)physicalWidth, (LPARAM)physicalHeight);
        }

        if (AppWindow != null && !_isInitializing)
        {
            var scale = Display.GetScale(Handle);
            var logicalWidth = (int)(physicalWidth / scale);
            var logicalHeight = (int)(physicalHeight / scale);

            _internalResize = true;
            try
            {
                Width = logicalWidth;
                Height = logicalHeight;

                // Update last normal bounds if window is in normal state
                if (GetCurrentDisplayState() == WindowDisplayState.Normal)
                {
                    _lastNormalBounds.Width = logicalWidth;
                    _lastNormalBounds.Height = logicalHeight;
                }
            }
            finally
            {
                _internalResize = false;
            }
        }
    }

    private void UpdatePositionFromAppWindow()
    {
        if (AppWindow == null || _internalResize) return;

        var scale = Display.GetScale(Handle);
        var position = AppWindow.Position;

        _internalResize = true;
        try
        {
            X = (int)(position.X / scale);
            Y = (int)(position.Y / scale);

            // Update last normal bounds if window is in normal state
            if (GetCurrentDisplayState() == WindowDisplayState.Normal)
            {
                _lastNormalBounds.X = X;
                _lastNormalBounds.Y = Y;
            }
        }
        finally
        {
            _internalResize = false;
        }
    }

    private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_CREATE:
                if (!_xamlInitialized)
                    InitializeXaml();
                break;

            case WM_MOVE:
                UpdatePositionFromAppWindow();
                break;

            case WM_SIZE:
                {
                    int width = LOWORD(lParam);
                    int height = HIWORD(lParam);
                    OnResize(width, height);
                    break;
                }

            case WM_DPICHANGED:
                {
                    int newDpi = (int)(wParam & 0xFFFF);
                    float newScale = newDpi / 96.0f;

                    OnDpiChanged(newScale);

                    RECT* suggestedRect = (RECT*)lParam;
                    SetWindowPos(hwnd, HWND.NULL,
                        suggestedRect->left, suggestedRect->top,
                        suggestedRect->right - suggestedRect->left,
                        suggestedRect->bottom - suggestedRect->top,
                        SWP_NOZORDER | SWP_NOACTIVATE);

                    return 0;
                }

            case WM_SETTINGCHANGE:
            case WM_THEMECHANGED:
                ProcessCoreWindowMessage(msg, wParam, lParam);
                break;

            case WM_NCLBUTTONDBLCLK:
                if (!IsMaximizable)
                    return new LRESULT(0);
                break;

            case WM_SETFOCUS:
                OnSetFocus();
                break;

            case WM_DESTROY:
                PostQuitMessage(0);
                break;

            default:
                return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return new LRESULT(0);
    }

    private unsafe void OnDpiChanged(float newScale)
    {
        if (AppWindow is null || AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        presenter.PreferredMinimumWidth = (int)(MinWidth * newScale);
        presenter.PreferredMaximumWidth = (int)(MaxWidth * newScale);
        presenter.PreferredMinimumHeight = (int)(MinHeight * newScale);
        presenter.PreferredMaximumHeight = (int)(MaxHeight * newScale);

        int scaledX = (int)(X * newScale);
        int scaledY = (int)(Y * newScale);
        int scaledWidth = (int)(Width * newScale);
        int scaledHeight = (int)(Height * newScale);

        _internalResize = true;
        try
        {
            AppWindow.Move(new Windows.Graphics.PointInt32(scaledX, scaledY));
            AppWindow.Resize(new Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));
        }
        finally
        {
            _internalResize = false;
        }

        RECT clientRect;
        GetClientRect(Handle, &clientRect);
        int clientWidth = clientRect.right - clientRect.left;
        int clientHeight = clientRect.bottom - clientRect.top;
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, clientWidth, clientHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
    }

    internal void ProcessCoreWindowMessage(uint message, WPARAM wParam, LPARAM lParam)
    {
        if (_coreHwnd != default)
            SendMessageW(_coreHwnd, message, wParam, lParam);
    }

    internal void OnSetFocus()
    {
        if (_xamlHwnd != default)
            SetFocus(_xamlHwnd);
    }

    // ------------------------------------------------------------------------
    // OBSERVABLE PROPERTY CHANGED HANDLERS
    // ------------------------------------------------------------------------

    partial void OnTitleChanged(string oldValue, string newValue)
    {
        if (AppWindow != null)
            AppWindow.Title = newValue;
    }

    partial void OnContentChanged(UIElement value)
    {
        if (_xamlInitialized && _desktopWindowXamlSource != null && value != null)
        {
            _desktopWindowXamlSource.Content = value;
        }
    }

    partial void OnXChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var scale = Display.GetScale(Handle);
            int physicalX = (int)(value * scale);
            AppWindow.Move(new Windows.Graphics.PointInt32(physicalX, AppWindow.Position.Y));
        }
    }

    partial void OnYChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var scale = Display.GetScale(Handle);
            int physicalY = (int)(value * scale);
            AppWindow.Move(new Windows.Graphics.PointInt32(AppWindow.Position.X, physicalY));
        }
    }

    partial void OnWidthChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var currentSize = AppWindow.Size;
            var physicalWidth = (int)(value * Display.GetScale(Handle));
            AppWindow.Resize(new(physicalWidth, currentSize.Height));
        }
    }

    partial void OnHeightChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var currentSize = AppWindow.Size;
            var physicalHeight = (int)(value * Display.GetScale(Handle));
            AppWindow.Resize(new(currentSize.Width, physicalHeight));
        }
    }

    partial void OnMinWidthChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(Handle);
            presenter.PreferredMinimumWidth = (int)(value * scale);
        }
    }

    partial void OnMaxWidthChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(Handle);
            presenter.PreferredMaximumWidth = (int)(value * scale);
        }
    }

    partial void OnMinHeightChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(Handle);
            presenter.PreferredMinimumHeight = (int)(value * scale);
        }
    }

    partial void OnMaxHeightChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(Handle);
            presenter.PreferredMaximumHeight = (int)(value * scale);
        }
    }

    partial void OnIsResizableChanged(bool value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).IsResizable = value;
    }

    partial void OnIsMaximizableChanged(bool value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).IsMaximizable = value;
    }

    partial void OnIsMinimizableChanged(bool value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).IsMinimizable = value;
    }

    // ------------------------------------------------------------------------
    // UTILITY METHODS
    // ------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(IslandsWindow));
    }
}