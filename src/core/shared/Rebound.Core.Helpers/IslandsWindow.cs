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

namespace Rebound.Core.Helpers;

internal static class _windowList
{
    public static readonly List<IslandsWindow> _openWindows = [];

    public static void RegisterWindow(IslandsWindow window)
    {
        _openWindows.Add(window);
        window.Closed += (s, e) =>
        {
            _openWindows.Remove(window);
            if (_openWindows.Count == 0)
            {
                Windows.UI.Xaml.Application.Current.Exit();
                Process.GetCurrentProcess().Kill();
            }
        };
    }
}

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

public sealed class ReboundDialog : IslandsWindow
{
    private TaskCompletionSource<bool> _tcs = new();

    public static async Task ShowAsync(string title, string message, DialogIcon icon = DialogIcon.Info)
    {
        using var dlg = new ReboundDialog(title, message, icon);
        dlg.Create(); // creates and shows window
        await dlg._tcs.Task.ConfigureAwait(false);
    }

    public ReboundDialog(string title, string message, DialogIcon icon)
    {
        Title = title;
        IsPersistenceEnabled = false;

        XamlInitialized += (_, _) =>
        {
            // Create the dialog page
            var page = new Page();
            BackdropMaterial.SetApplyToRootOrPageBackground(page, true);

            // Root grid for full layout (title bar + content)
            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // Title bar
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Content
                },
                CornerRadius = new CornerRadius(8)
            };

            // ==== TITLE BAR ====
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

            // ==== CONTENT AREA ====
            var contentGrid = new Grid
            {
                Padding = new Thickness(20, 20, 20, 0),
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto } // footer
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

            // ==== FOOTER BAR ====
            var footerBar = new Border
            {
                Background = (Brush)Windows.UI.Xaml.Application.Current.Resources["SystemControlBackgroundAltMediumLowBrush"],
                Padding = new Thickness(24),
                Margin = new (-20, 0, -20, 0)
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

            // Attach content to the Page and then to the window
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
        PInvoke.ExtractIconEx(dll.ToPCWSTR(), iconIndex, &largeIcon, null, 1);

        /*if (largeIcon == Windows.Win32.UI.WindowsAndMessaging.HICON.NULL)
            return null;*/

        // Get icon info
        Windows.Win32.UI.WindowsAndMessaging.ICONINFO iconInfo;
        if (!PInvoke.GetIconInfo(largeIcon, &iconInfo))
            return null;

        Windows.Win32.Graphics.Gdi.BITMAP bmp;
        if (PInvoke.GetObject(iconInfo.hbmColor, sizeof(Windows.Win32.Graphics.Gdi.BITMAP), &bmp) == 0)
            return null;

        int width = bmp.bmWidth;
        int height = Math.Abs(bmp.bmHeight);

        // Create compatible DC
        var hdcScreen = PInvoke.GetDC(new());
        var hdcMem = PInvoke.CreateCompatibleDC(hdcScreen);
        var hBitmap = PInvoke.CreateCompatibleBitmap(hdcScreen, width, height);
        var old = PInvoke.SelectObject(hdcMem, hBitmap);

        // Draw the icon into the memory DC
        PInvoke.DrawIconEx(hdcMem, 0, 0, largeIcon, width, height, 0, new(), Windows.Win32.UI.WindowsAndMessaging.DI_FLAGS.DI_NORMAL);

        // Convert HBITMAP → byte array (BGRA)
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
                biHeight = -height, // top-down DIB
                biPlanes = 1,
                biBitCount = 32,
                biCompression = (uint)BI.BI_RGB
            }
            };

            _ = PInvoke.GetDIBits(hdcMem, hBitmap, 0, (uint)height, pPixels, &bmi, Windows.Win32.Graphics.Gdi.DIB_USAGE.DIB_RGB_COLORS);
        }

        // Clean up GDI resources
        PInvoke.SelectObject(hdcMem, old);
        PInvoke.DeleteDC(hdcMem);
        _ = PInvoke.ReleaseDC(new(), hdcScreen);
        PInvoke.DeleteObject(hBitmap);
        PInvoke.DeleteObject(iconInfo.hbmColor);
        PInvoke.DeleteObject(iconInfo.hbmMask);
        PInvoke.DestroyIcon(largeIcon);

        // Convert to BitmapImage
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

public enum DialogIcon
{
    None,
    Info,
    Warning,
    Error,
    Shield
}

public partial class IslandsWindow : ObservableObject, IDisposable
{
    private const string WindowClassName = "XamlIslandsClass";
    private static readonly ushort _classAtom;

    public bool _closed { get; private set; }

    // Static WndProc (unmanaged entry)
    [UnmanagedCallersOnly]
    private static unsafe LRESULT WndProcStatic(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        // Fast-path: fetch GWLP_USERDATA once
        var ptr = GetWindowLongPtr(hwnd, GWLP.GWLP_USERDATA);
        if (ptr == IntPtr.Zero)
            return DefWindowProc(hwnd, msg, wParam, lParam);

        // Fast retrieval of managed instance
        var handle = GCHandle.FromIntPtr(ptr);
        var window = (IslandsWindow)handle.Target!;
        return window.WndProc(hwnd, msg, wParam, lParam);
    }

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static IslandsWindow()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        // Register window class once (safe if multiple instances)
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

                // If class already registered, RegisterClassW returns 0 and GetLastError() == ERROR_CLASS_ALREADY_EXISTS.
                _classAtom = RegisterClassW(&wc);
            }
        }

        // Load legacy libs once (if needed) to avoid per-window load
        unsafe
        {
            // optional legacy compatibility - ignore failures
            InternalLoadLibrary("twinapi.appcore.dll"); InternalLoadLibrary("threadpoolwinrt.dll");
        }

        // Initialize XAML manager for the thread
        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InternalLoadLibrary(string lib)
    {
        fixed (char* libName = lib)
            LoadLibraryW(libName);
    }

    private GCHandle _thisHandle;
    private volatile bool _xamlInitialized;
    private bool _disposed;

    private HWND _xamlHwnd;
    private HWND _coreHwnd;

    private DesktopWindowXamlSource? _desktopWindowXamlSource;
    private static WindowsXamlManager? _xamlManager;
    private CoreWindow? _coreWindow;

    // Cached native COM interfaces to avoid repeated QueryInterface
    private TerraFX.Interop.Windows.ComPtr<IDesktopWindowXamlSourceNative2> _nativeSource;
    private TerraFX.Interop.Windows.ComPtr<ICoreWindowInterop> _coreWindowInterop;

    // Public properties from your original
    public HWND Handle { get; private set; }
    public AppWindow? AppWindow { get; private set; }

    [ObservableProperty] public partial bool IsMaximizable { get; set; } = true;
    [ObservableProperty] public partial bool IsMinimizable { get; set; } = true;
    [ObservableProperty] public partial bool IsResizable { get; set; } = true;
    [ObservableProperty] public partial int MinWidth { get; set; } = 0;
    [ObservableProperty] public partial int MinHeight { get; set; } = 0;
    [ObservableProperty] public partial int MaxWidth { get; set; } = int.MaxValue;
    [ObservableProperty] public partial int MaxHeight { get; set; } = int.MaxValue;
    [ObservableProperty] public partial string Title { get; set; } = "UWP XAML Islands Window";
    [ObservableProperty] public partial UIElement? Content { get; set; }
    [ObservableProperty] public partial bool IsPersistenceEnabled { get; set; } = false;
    [ObservableProperty] public partial string PersistenceKey { get; set; } = nameof(IslandsWindow);
    [ObservableProperty] public partial string PersistanceFileName { get; set; } = "rebound";

    private bool _internalResize;
    private bool _isInitializing;
    private bool _boundsApplied;
    private WindowBounds _pendingBounds = new();

    // Stored in LOGICAL pixels (DPI-independent)
    [ObservableProperty] public partial int Width { get; set; } = 800;
    [ObservableProperty] public partial int Height { get; set; } = 600;
    [ObservableProperty] public partial int X { get; set; } = -1; // -1 = center/default
    [ObservableProperty] public partial int Y { get; set; } = -1;

    private class WindowBounds
    {
        public int X { get; set; } = -1;
        public int Y { get; set; } = -1;
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
    }

    public event EventHandler<XamlInitializedEventArgs>? XamlInitialized;
    public event EventHandler<AppWindowInitializedEventArgs>? AppWindowInitialized;
    public event EventHandler<IslandsWindowClosingEventArgs>? Closing;
    public event EventHandler? Closed;

    public IslandsWindow() { }

    public void Activate()
    {
        if (AppWindow != null)
        {
            if (IsIconic(Handle) != 0) // if minimized
            {
                ShowWindow(Handle, SW.SW_RESTORE); // restore window
            }
            SetForegroundWindow(Handle); // 
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

    public void Close()
    {
        if (_closed) return;

        var args = new IslandsWindowClosingEventArgs();
        Closing?.Invoke(this, args);
        if (args.Handled) return; // cancel closing if someone handled it

        SaveWindowState();

        _closed = true;

        // Destroy the AppWindow first (important!)
        if (AppWindow != null)
        {
            AppWindow.Closing -= OnAppWindowClosing;
            AppWindow.Destroy();
            AppWindow = null;
        }

        // Dispose internal resources
        Dispose();

        // Fire Closed event
        Closed?.Invoke(this, EventArgs.Empty);
    }

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

    public void LoadWindowState()
    {
        if (!IsPersistenceEnabled) return;

        // Only apply persisted values if they are valid (non-default)
        int persistedX = SettingsHelper.GetValue($"{PersistenceKey}_X", PersistanceFileName, X);
        int persistedY = SettingsHelper.GetValue($"{PersistenceKey}_Y", PersistanceFileName, Y);
        int persistedWidth = SettingsHelper.GetValue($"{PersistenceKey}_Width", PersistanceFileName, Width == 0 ? 800 : Width);
        int persistedHeight = SettingsHelper.GetValue($"{PersistenceKey}_Height", PersistanceFileName, Height == 0 ? 600 : Height);

        if (persistedWidth >0 && persistedHeight >0)
        {
            X = persistedX;
            Y = persistedY;
            Width = persistedWidth;
            Height = persistedHeight;
        }
    }

    public void SetExtendedWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE style)
    {
        // Get current extended style
        var exStyle = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE)GetWindowLongPtrW(Handle, GWL.GWL_EXSTYLE);
        // Add the new style
        exStyle |= style;
        SetWindowLongPtrW(Handle, GWL.GWL_EXSTYLE, (nint)exStyle);
        // Apply the changes
        SetWindowPos(
            Handle,
            HWND.NULL,
            0, 0, 0, 0,
            SWP.SWP_NOMOVE | SWP.SWP_NOSIZE | SWP.SWP_NOZORDER | SWP.SWP_FRAMECHANGED
        );
    }

    public void SetWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE style)
    {
        // Get current style
        var ws = (Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE)GetWindowLongPtrW(Handle, GWL.GWL_STYLE);
        // Add the new style
        ws |= style;
        SetWindowLongPtrW(Handle, GWL.GWL_STYLE, (nint)ws);
        // Apply the changes
        SetWindowPos(
            Handle,
            HWND.NULL,
            0, 0, 0, 0,
            SWP.SWP_NOMOVE | SWP.SWP_NOSIZE | SWP.SWP_NOZORDER | SWP.SWP_FRAMECHANGED
        );
    }

    public void SetWindowOpacity(byte opacity)
    {
        // Ensure layered style is set
        SetExtendedWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED);
        // Apply opacity (0-255)
        SetLayeredWindowAttributes(Handle, 0, opacity, LWA.LWA_ALPHA);
    }

    // ---------- Activation & message loop ----------

    public unsafe void Create()
    {
        ThrowIfDisposed();

        _isInitializing = true;
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        IntPtr gcPtr = GCHandle.ToIntPtr(_thisHandle);

        // 1. Load persisted state FIRST (if enabled)
        if (IsPersistenceEnabled)
        {
            var persistedBounds = LoadPersistedBounds();
            // Only override if valid persisted values exist
            if (persistedBounds.Width > 0 && persistedBounds.Height > 0)
            {
                Width = persistedBounds.Width;
                Height = persistedBounds.Height;
                X = persistedBounds.X;
                Y = persistedBounds.Y;
            }
        }

        // Store the logical bounds before window creation
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

            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, gcPtr);

            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindow.Closing += OnAppWindowClosing;

            // 2. Fire event BEFORE applying bounds (so user can override)
            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            InitializeXaml();

            SetWindowLongPtrW(Handle, GWL.GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
            SetWindowPos(Handle, HWND.HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            _isInitializing = false;

            // 3. Apply bounds after window is shown and stable
            ApplyPendingBounds();

            ShowWindow(Handle, SW.SW_SHOWNORMAL);
            BringWindowToTop(Handle);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);
            SetFocus(Handle);

            _windowList.RegisterWindow(this);
        }
    }

    // ---------- XAML initialization (idempotent, cached) ----------
    public unsafe void InitializeXaml()
    {
        ThrowIfDisposed();

        if (_xamlInitialized) return; // fast path if already initialized

        // Create DesktopWindowXamlSource and cache native pointer
        _desktopWindowXamlSource = new DesktopWindowXamlSource();

        // Query and cache IDesktopWindowXamlSourceNative2 once
        var raw = ((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr);
        ThrowIfFailed(raw->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)_nativeSource.GetAddressOf()));

        // Attach and retrieve XAML child HWND
        ThrowIfFailed(_nativeSource.Get()->AttachToWindow(Handle));
        ThrowIfFailed(_nativeSource.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd)));

        // Resize to client area immediately and avoid extra allocations
        RECT clientRect;
        GetClientRect(Handle, &clientRect);
        var clientWidth = clientRect.right - clientRect.left;
        var clientHeight = clientRect.bottom - clientRect.top;
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, clientWidth, clientHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        // Cache CoreWindow and its interop once
        _coreWindow = CoreWindow.GetForCurrentThread();
        var coreRaw = ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr);
        ThrowIfFailed(coreRaw->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)_coreWindowInterop.GetAddressOf()));
        ThrowIfFailed(_coreWindowInterop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd)));

        // Mark initialized, set up SyncContext and theme settings
        _xamlInitialized = true;
        XamlInitialized?.Invoke(this, new XamlInitializedEventArgs());
        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

        // Theme handling: create theme listener and set DWM attributes once
        using var themeListener = new ThemeListener();
        // Hook event (no capture allocations loop)
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

        // Apply initial backdrop & dark-mode attributes
        unsafe
        {
            var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &backdrop, sizeof(int));
            int dark = themeListener.CurrentTheme == ApplicationTheme.Light ? 0 : 1;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &dark, sizeof(int));
        }

        // Keep reference to themeListener to avoid immediate disposal by GC
        // (If you need to unsubscribe later, expose it as a field and handle in Dispose.)
    }

    // Cached PreTranslateMessage using cached _nativeSource
    public unsafe bool PreTranslateMessage(MSG* msg)
    {
        if (!_xamlInitialized) return false;
        BOOL outResult = false;
        // _nativeSource is cached; avoid re-QI every time
        if (!_closed && _nativeSource.Get() != null) ThrowIfFailed(_nativeSource.Get()->PreTranslateMessage(msg, &outResult));
        return outResult;
    }

    public unsafe void ApplyPendingBounds()
    {
        if (AppWindow == null || !IsPersistenceEnabled) return;

        _boundsApplied = true;

        var scale = Display.GetScale(AppWindow);

        // Convert logical to physical
        int physicalWidth = (int)(_pendingBounds.Width * scale);
        int physicalHeight = (int)(_pendingBounds.Height * scale);

        _internalResize = true;
        try
        {
            // Resize first
            AppWindow.Resize(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

            // Then position (or center if default)
            if (_pendingBounds.X >= 0 && _pendingBounds.Y >= 0)
            {
                int physicalX = (int)(_pendingBounds.X * scale);
                int physicalY = (int)(_pendingBounds.Y * scale);
                AppWindow.Move(new Windows.Graphics.PointInt32(physicalX, physicalY));

                // Update logical properties to reflect actual position (fixes Issue #1)
                X = _pendingBounds.X;
                Y = _pendingBounds.Y;
            }
            else
            {
                // Center on screen
                CenterWindow();
            }
        }
        finally
        {
            _internalResize = false;
        }
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

        // Update logical properties to reflect actual position
        var scale = Display.GetScale(AppWindow);
        X = (int)(centerX / scale);
        Y = (int)(centerY / scale);
    }

    private WindowBounds LoadPersistedBounds()
    {
        return new WindowBounds
        {
            X = SettingsHelper.GetValue($"{PersistenceKey}_X", PersistanceFileName, -1),
            Y = SettingsHelper.GetValue($"{PersistenceKey}_Y", PersistanceFileName, -1),
            Width = SettingsHelper.GetValue($"{PersistenceKey}_Width", PersistanceFileName, -1),
            Height = SettingsHelper.GetValue($"{PersistenceKey}_Height", PersistanceFileName, -1)
        };
    }

    private void SaveWindowState()
    {
        if (!IsPersistenceEnabled || AppWindow == null) return;

        var scale = Display.GetScale(AppWindow);

        // Compensate for title bar extension
        // When ExtendsContentIntoTitleBar is true, Windows removes ~6-7 physical pixels
        // We need to save the adjusted height so it restores correctly
        int heightToSave = Height;
        if (AppWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            int titleBarCompensation = (int)(8 * scale); // 8 logical pixels * DPI scale
            heightToSave += (int)(titleBarCompensation / scale); // Add back in logical pixels
        }

        // Always save current logical values
        SettingsHelper.SetValue($"{PersistenceKey}_X", PersistanceFileName, X);
        SettingsHelper.SetValue($"{PersistenceKey}_Y", PersistanceFileName, Y);
        SettingsHelper.SetValue($"{PersistenceKey}_Width", PersistanceFileName, Width);
        SettingsHelper.SetValue($"{PersistenceKey}_Height", PersistanceFileName, heightToSave);
    }

    internal void OnResize(int physicalWidth, int physicalHeight)
    {
        // Update XAML island
        if (_xamlHwnd != default)
        {
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, physicalWidth, physicalHeight,
                SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
        }

        if (_coreHwnd != default)
        {
            SendMessageW(_coreHwnd, WM_SIZE, (WPARAM)physicalWidth, (LPARAM)physicalHeight);
        }

        // Convert physical to logical and update properties
        if (AppWindow != null && !_isInitializing)
        {
            var scale = Display.GetScale(AppWindow);
            var logicalWidth = (int)(physicalWidth / scale);
            var logicalHeight = (int)(physicalHeight / scale);

            _internalResize = true;
            try
            {
                Width = logicalWidth;
                Height = logicalHeight;
            }
            finally
            {
                _internalResize = false;
            }
        }
    }

    // Helper method to update position when window moves
    private void UpdatePositionFromAppWindow()
    {
        if (AppWindow == null || _internalResize) return;

        var scale = Display.GetScale(AppWindow);
        var position = AppWindow.Position;

        _internalResize = true;
        try
        {
            X = (int)(position.X / scale);
            Y = (int)(position.Y / scale);
        }
        finally
        {
            _internalResize = false;
        }
    }

    // ---------- Window proc / message handlers ----------
    private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_CREATE:
                // Create might be called if created earlier — Ensure idempotence
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
                    int newDpi = (int)(wParam & 0xFFFF); // DPI is in the low-order word
                    float newScale = newDpi / 96.0f;

                    OnDpiChanged(newScale);

                    // Windows gives you a suggested new window rect in lParam:
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
                    return new LRESULT(0); // ignore double-click if not maximizable
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

        // Reapply DPI-dependent constraints
        presenter.PreferredMinimumWidth = (int)(MinWidth * newScale);
        presenter.PreferredMaximumWidth = (int)(MaxWidth * newScale);
        presenter.PreferredMinimumHeight = (int)(MinHeight * newScale);
        presenter.PreferredMaximumHeight = (int)(MaxHeight * newScale);

        // Convert logical values to physical pixels
        int scaledX = (int)(X * newScale);
        int scaledY = (int)(Y * newScale);
        int scaledWidth = (int)(Width * newScale);
        int scaledHeight = (int)(Height * newScale);

        // Set flag to prevent OnWidth/Height circular updates
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

        // Update XAML island bounds
        RECT clientRect;
        GetClientRect(Handle, &clientRect);
        int clientWidth = clientRect.right - clientRect.left;
        int clientHeight = clientRect.bottom - clientRect.top;
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, clientWidth, clientHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
    }

    partial void OnTitleChanged(string oldValue, string newValue)
    {
        if (AppWindow != null)
            AppWindow.Title = newValue;
    }

    partial void OnContentChanged(UIElement value)
    {
        // fast path - set content if initialized and non-null
        if (_xamlInitialized && _desktopWindowXamlSource != null && value != null)
        {
            _desktopWindowXamlSource.Content = value;
        }
    }

    partial void OnXChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var scale = Display.GetScale(AppWindow);
            int physicalX = (int)(value * scale);
            AppWindow.Move(new Windows.Graphics.PointInt32(physicalX, AppWindow.Position.Y));
        }
    }

    partial void OnYChanged(int value)
    {
        if (!_internalResize && AppWindow != null)
        {
            var scale = Display.GetScale(AppWindow);
            int physicalY = (int)(value * scale);
            AppWindow.Move(new Windows.Graphics.PointInt32(AppWindow.Position.X, physicalY));
        }
    }

    // Replace the commented-out methods with these:
    partial void OnWidthChanged(int value)
    {
        // Only resize AppWindow if the change came from outside (not from WM_SIZE)
        if (!_internalResize && AppWindow != null)
        {
            // Convert logical pixels to physical pixels
            var currentSize = AppWindow.Size;
            var physicalWidth = (int)(value * Display.GetScale(AppWindow));
            AppWindow.Resize(new(physicalWidth, currentSize.Height));
        }
    }

    partial void OnHeightChanged(int value)
    {
        // Only resize AppWindow if the change came from outside (not from WM_SIZE)
        if (!_internalResize && AppWindow != null)
        {
            // Convert logical pixels to physical pixels
            var currentSize = AppWindow.Size;
            var physicalHeight = (int)(value * Display.GetScale(AppWindow));
            AppWindow.Resize(new(currentSize.Width, physicalHeight));
        }
    }

    public void BringToFront()
    {
        TerraFX.Interop.Windows.Windows.ShowWindow(Handle, SW.SW_SHOW);
        TerraFX.Interop.Windows.Windows.SetForegroundWindow(Handle);
        TerraFX.Interop.Windows.Windows.SetActiveWindow(Handle);
    }

    partial void OnMinWidthChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(AppWindow);
            presenter.PreferredMinimumWidth = (int)(value * scale);
        }
    }

    partial void OnMaxWidthChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(AppWindow);
            presenter.PreferredMaximumWidth = (int)(value * scale);
        }
    }

    partial void OnMinHeightChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(AppWindow);
            presenter.PreferredMinimumHeight = (int)(value * scale);
        }
    }

    partial void OnMaxHeightChanged(int value)
    {
        if (AppWindow is not null && AppWindow.Presenter is OverlappedPresenter presenter)
        {
            var scale = Display.GetScale(AppWindow);
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

    // ---------- IDisposable / cleanup ----------
    public unsafe void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Free pinned GCHandle and clear userdata
        if (_thisHandle.IsAllocated)
        {
            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, IntPtr.Zero);
            _thisHandle.Free();
        }

        // Release cached COM pointers
        _nativeSource.Dispose();
        _nativeSource = default;
        _coreWindowInterop.Dispose();
        _coreWindowInterop = default;

        // Release WinRT/XAML objects (keep manager alive if needed)
        _desktopWindowXamlSource?.Dispose();
        //_xamlManager?.Dispose();
        _coreWindow = null;

        // Destroy the Win32 window if still present
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(IslandsWindow));
    }
}