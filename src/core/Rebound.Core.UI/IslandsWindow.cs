// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Rebound.Core.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using WinRT;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using BOOL = TerraFX.Interop.Windows.BOOL;
using HWND = TerraFX.Interop.Windows.HWND;
using LPARAM = TerraFX.Interop.Windows.LPARAM;
using LRESULT = TerraFX.Interop.Windows.LRESULT;
using WPARAM = TerraFX.Interop.Windows.WPARAM;

namespace Rebound.Core.UI;

public enum DialogIcon
{
    None,
    Info,
    Warning,
    Error,
    Shield
}

public class ThemeRegistryListener : IDisposable
{
    public const int REG_NOTIFY_CHANGE_NAME = 0x00000001;
    public const int REG_NOTIFY_CHANGE_ATTRIBUTES = 0x00000002;
    public const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;
    public const int REG_NOTIFY_CHANGE_SECURITY = 0x00000008;

    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";
    private readonly RegistryKey registryKey;
    private Task? watchTask;
    private CancellationTokenSource? cts;

    public event Action<bool>? ThemeChanged; // true = dark mode, false = light mode

    public ThemeRegistryListener()
    {
        registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false) ?? throw new Exception("Failed to open registry key.");
    }

    public void Start()
    {
        if (watchTask != null)
            throw new InvalidOperationException("Already started.");

        cts = new CancellationTokenSource();
        watchTask = Task.Run(() => WatchRegistry(cts.Token));
    }

    public void Stop()
    {
        cts?.Cancel();
        watchTask = null;
        cts = null;
    }

    private void WatchRegistry(CancellationToken token)
    {
        var regHandle = registryKey.Handle;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Wait for changes on the key or cancellation
                Microsoft.Win32.SafeHandles.SafeRegistryHandle handle = regHandle;
                var waitResult = TerraFX.Interop.Windows.Windows.RegNotifyChangeKeyValue(HKEY.HKEY_CURRENT_USER,
                    true,
                    REG_NOTIFY_CHANGE_LAST_SET,
                    HANDLE.NULL,
                    false);

                if (waitResult == 0)
                {
                    var isDarkMode = !IsLightTheme();
                    ThemeChanged?.Invoke(isDarkMode);
                }

                if (token.IsCancellationRequested)
                    break;
            }
            catch
            {
                break;
            }
        }
    }

    public bool IsLightTheme()
    {
        var val = registryKey.GetValue(ValueName);
        if (val is int intValue)
            return intValue != 0;
        return true; // default to light if missing
    }

    public void Dispose()
    {
        Stop();
        registryKey.Dispose();
    }
}

public class IslandsWindowClosingEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class XamlInitializedEventArgs : EventArgs { }

public class AppWindowInitializedEventArgs : EventArgs { }

public static class WindowList
{
    public static readonly List<IslandsWindow> OpenWindows = [];
    public static bool KeepAlive { get; set; }

    /// <summary>
    /// Register the window to the global open windows list. Used for tracking how many windows are open
    /// in an app for automatic closing. To override automatic app closing, set <see cref="KeepAlive"/> to true.
    /// </summary>
    /// <param name="window">The window to add to the collection.</param>
    internal static void RegisterWindow(IslandsWindow window)
    {
        OpenWindows.Add(window);
        window.OnClosed += (s, e) =>
        {
            OpenWindows.Remove(window);
            if (OpenWindows.Count == 0 && !KeepAlive)
            {
                Application.Current.Exit();
                Process.GetCurrentProcess().Kill();
            }
        };
    }
}

public sealed partial class ReboundDialog : IslandsWindow
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>
    /// Shows a simple dialog window in WinUI 2 XAML islands, based on the Win32 message box. For quick usage
    /// of a disposable dialog used for notifying the user of various things, use this method instead of
    /// creating an instance of the <see cref="ReboundDialog"/> class.
    /// </summary>
    /// <param name="title">The title of the window. Appears in the title bar, taskbar, and header.</param>
    /// <param name="message">Message content for the dialog.</param>
    /// <param name="icon">The icon used by the dialog. Queried from the system.</param>
    /// <returns>A task corresponding to the object.</returns>
    public static async Task ShowAsync(string title, string message, DialogIcon icon = DialogIcon.Info, int height = 256)
    {
        using var dlg = new ReboundDialog(title, message, icon, height);
        dlg.Create();
        await dlg._tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Initializes an instance of the <see cref="ReboundDialog"/> class: a simple dialog window in WinUI 2 XAML islands,
    /// based on the Win32 message box. For a quick usage of a disposable dialog used for notifying the user of various things,
    /// use the <see cref="ShowAsync"/> method instead of creating an instance of this class.
    /// </summary>
    /// <param name="title">The title of the window. Appears in the title bar, taskbar, and header.</param>
    /// <param name="message">Message content for the dialog.</param>
    /// <param name="icon">The icon used by the dialog. Queried from the system.</param>
    public ReboundDialog(string title, string message, DialogIcon icon, int height)
    {
        Title = title;
        IsPersistenceEnabled = false;

        XamlInitialized += (_, _) =>
        {
            var page = new Page();

            // Mica (mandatory as always)
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
                Background = (Brush)Application.Current.Resources["SystemControlBackgroundAltMediumLowBrush"],
                Padding = new Thickness(24),
                Margin = new(-20, 0, -20, 0)
            };
            Grid.SetRow(footerBar, 3);

            var footerGrid = new Grid();
            var okButton = new Button
            {
                Content = "OK",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"],
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
            Height = height;
            IsMaximizable = false;
            IsMinimizable = false;
            IsResizable = false;
            CenterWindow();
        };
    }

    private static unsafe BitmapImage? LoadSystemIcon(DialogIcon icon)
    {
        string dll = Environment.SystemDirectory + "\\imageres.dll";
        int iconIndex = icon switch
        {
            DialogIcon.Warning => 79,
            DialogIcon.Error => 98,
            DialogIcon.Info => 81,
            DialogIcon.Shield => 1028,
            _ => 277
        };

        HICON largeIcon = default;
        fixed (char* dllPtr = dll)
        {
            ExtractIconEx(dllPtr, iconIndex, &largeIcon, null, 1);
        }

        ICONINFO iconInfo;
        if (!GetIconInfo(largeIcon, &iconInfo))
            return null;

        Windows.Win32.Graphics.Gdi.BITMAP bmp;
        if (GetObject(iconInfo.hbmColor, sizeof(Windows.Win32.Graphics.Gdi.BITMAP), &bmp) == 0)
            return null;

        int width = bmp.bmWidth;
        int height = Math.Abs(bmp.bmHeight);

        var hdcScreen = GetDC(new());
        var hdcMem = CreateCompatibleDC(hdcScreen);
        var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        var old = SelectObject(hdcMem, hBitmap);

        DrawIconEx(hdcMem, 0, 0, largeIcon, width, height, 0, new(), (uint)Windows.Win32.UI.WindowsAndMessaging.DI_FLAGS.DI_NORMAL);

        int bytesPerPixel = 4;
        int stride = width * bytesPerPixel;
        int bufferSize = stride * height;
        byte[] pixelData = new byte[bufferSize];

        fixed (byte* pPixels = pixelData)
        {
            BITMAPINFO bmi = new()
            {
                bmiHeader =
                {
                    biSize = (uint)sizeof(BITMAPINFOHEADER),
                    biWidth = width,
                    biHeight = -height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = (uint)BI.BI_RGB
                }
            };

            _ = GetDIBits(hdcMem, hBitmap, 0, (uint)height, pPixels, &bmi, (uint)Windows.Win32.Graphics.Gdi.DIB_USAGE.DIB_RGB_COLORS);
        }

        SelectObject(hdcMem, old);
        DeleteDC(hdcMem);
        _ = ReleaseDC(new(), hdcScreen);
        DeleteObject(hBitmap);
        DeleteObject(iconInfo.hbmColor);
        DeleteObject(iconInfo.hbmMask);
        DestroyIcon(largeIcon);

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

public partial class IslandsWindow : ObservableObject, IDisposable
{
    // Constants and static fields
    private const string WindowClassName = "XamlIslandsClass";
    private static readonly ushort _classAtom;
    private static WindowsXamlManager? _xamlManager;

    // Internal stuff
    private bool _disposed;
    private volatile bool _xamlInitialized;
    private bool _internalResize;
    private bool _isInitializing;

    // Native handles
    private GCHandle _thisHandle;
    private HWND _xamlHwnd;

    // UWP
    private DesktopWindowXamlSource? _desktopWindowXamlSource;

    // WinRT
    private ComPtr<IDesktopWindowXamlSourceNative2> _nativeSource;

    /// <summary>
    /// Indicates if the window has been closed. Use this property to check if the window is still open in case
    /// you need to do operations in contexts when you are not sure if the window is still alive.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// The Win32 handle for the current window. Once the window has been created, this property can no longer
    /// be changed.
    /// </summary>
    public HWND Handle { get; private set; }

    /// <summary>
    /// AppWindow object for the current window. To append AppWindow tasks, subscribe to the 
    /// <see cref="AppWindowInitialized"/> event to ensure the AppWindow is ready for the
    /// requested operations.
    /// </summary>
    public AppWindow? AppWindow { get; private set; }

    [ObservableProperty] public partial UIElement? Content { get; set; }
    [ObservableProperty] public partial bool IsMaximizable { get; set; } = true;
    [ObservableProperty] public partial bool IsMinimizable { get; set; } = true;
    [ObservableProperty] public partial bool IsResizable { get; set; } = true;
    [ObservableProperty] public partial int MinWidth { get; set; } = 0;
    [ObservableProperty] public partial int MinHeight { get; set; } = 0;
    [ObservableProperty] public partial int MaxWidth { get; set; } = int.MaxValue;
    [ObservableProperty] public partial int MaxHeight { get; set; } = int.MaxValue;
    [ObservableProperty] public partial string Title { get; set; } = "UWP XAML Islands Window";
    [ObservableProperty] public partial int Width { get; set; } = int.MinValue;
    [ObservableProperty] public partial int Height { get; set; } = int.MinValue;
    [ObservableProperty] public partial int X { get; set; } = int.MinValue;
    [ObservableProperty] public partial int Y { get; set; } = int.MinValue;
    [ObservableProperty] public partial bool IsPersistenceEnabled { get; set; } = false;
    [ObservableProperty] public partial string PersistenceKey { get; set; } = nameof(IslandsWindow);
    [ObservableProperty] public partial string PersistenceFileName { get; set; } = "rebound";

    /// <summary>
    /// Triggered once when the XAML environment has been initialized for the current window.
    /// Subscribe to this event in order to set the <see cref="Content"/> of the current window to
    /// avoid race conditions.
    /// </summary>
    public event EventHandler<XamlInitializedEventArgs>? XamlInitialized;

    /// <summary>
    /// Triggered once when the <see cref="AppWindow"/> has been initialized for the current
    /// window. Subscribe to this event in order to perform AppWindow operations to prevent
    /// race conditions.
    /// </summary>
    public event EventHandler<AppWindowInitializedEventArgs>? AppWindowInitialized;

    /// <summary>
    /// Triggered whenever the window is about to be closed. If you want to display a confirmation
    /// dialog, for example, use this event instead of <see cref="OnClosed"/> and set 
    /// <see cref="IslandsWindowClosingEventArgs.Handled"/> to <see langword="true"/>.
    /// </summary>
    public event EventHandler<IslandsWindowClosingEventArgs>? OnClosing;

    /// <summary>
    /// Triggered whenever the window is fully closed. Use this event for cleanup and other
    /// post-closing operations.
    /// </summary>
    public event EventHandler? OnClosed;

    /// <summary>
    /// An object that defines a UWP XAML Islands window with the <see cref="AppWindow"/> property
    /// for convenience. To show the window, call the <see cref="Create"/> method.
    /// </summary>
    public IslandsWindow() { }

    ~IslandsWindow()
    {
        Dispose();
    }

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

        UIThreadQueue.QueueAction(() =>
        {
            _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
        });
    }

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

    /// <summary>
    /// Creates and shows the current window. When calling this, the <see cref="XamlInitialized"/> 
    /// and <see cref="AppWindowInitialized"/> events will be triggered.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void Create()
    {
        ThrowIfDisposed();

        _isInitializing = true;
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        var gcPtr = GCHandle.ToIntPtr(_thisHandle);

        fixed (char* className = WindowClassName)
        fixed (char* windowName = Title)
        {
            // Create window
            // For the best UX, the window is created hidden, sized, initialized,
            // and only then shown and activated
            Handle = CreateWindowExW(
                WS_EX_APPWINDOW | WS_EX_NOREDIRECTIONBITMAP,
                className,
                windowName,
                WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT, CW_USEDEFAULT,
                CW_USEDEFAULT, CW_USEDEFAULT,
                HWND.NULL,
                HMENU.NULL,
                GetModuleHandleW(null),
                null);

            // Something went seriously wrong
            if (Handle == HWND.NULL)
                throw new InvalidOperationException("Failed to create window");

            // Save the current object to the window's
            // user data for retrieval in the static WndProc
            SetWindowLongPtrW(Handle, GWLP.GWLP_USERDATA, gcPtr);

            // Sizing logic
            var (x, y, width, height) = GetDefaultUwpLikeSize();

            // If the raw values are still set to MinValue, it means
            // the window isn't created with explicit default size
            // and position values, so the window defaults can be applied
            if (Width == int.MinValue) Width = width;
            if (Height == int.MinValue) Height = height;
            if (X == int.MinValue) X = x;
            if (Y == int.MinValue) Y = y;

            SetWindowPos(Handle, HWND.HWND_TOP, X, Y, Width, Height,
                SWP_NOZORDER | SWP_NOACTIVATE);

            // AppWindow
            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindow.Closing += OnAppWindowClosing;
            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            // XAML
            InitializeXaml();
            _isInitializing = false;

            // Window persistence
            ApplyInitialPlacement();

            // Register the window in the global list
            WindowList.RegisterWindow(this);

            // Show the window and activate it now that all the properties
            // have been set
            ShowWindow(Handle, SW.SW_SHOW);
            AllowSetForegroundWindow(ASFW_ANY);
            BringWindowToTop(Handle);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);
            SetFocus(Handle);
        }
    }

    public unsafe void InitializeXaml()
    {
        ThrowIfDisposed();

        // If XAML is already initialized, it shouldn't
        // be initialized again
        if (_xamlInitialized) return;

        // Create the UWP DesktopWindowXamlSource
        _desktopWindowXamlSource = new DesktopWindowXamlSource();

        // Get the raw native interop object for the XAML source
        var raw = ((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr);
        ThrowIfFailed(raw->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)_nativeSource.GetAddressOf()));

        // Attach to the current window and get the XAML HWND
        ThrowIfFailed(_nativeSource.Get()->AttachToWindow(Handle));
        ThrowIfFailed(_nativeSource.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd)));

        // Set the initial size of the XAML island to fill the window
        RECT clientRect;
        GetClientRect(Handle, &clientRect);
        var clientWidth = clientRect.right - clientRect.left;
        var clientHeight = clientRect.bottom - clientRect.top;
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, clientWidth, clientHeight, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        // Obtain the CoreWindow for the XAML island
        //_coreWindow = CoreWindow.GetForCurrentThread();
        //var coreRaw = ((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr);
        //ThrowIfFailed(coreRaw->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)_coreWindowInterop.GetAddressOf()));
        //ThrowIfFailed(_coreWindowInterop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd)));

        // Trigger the XamlInitialized event and set the synchronization context
        _xamlInitialized = true;
        XamlInitialized?.Invoke(this, new XamlInitializedEventArgs());

        // Theme stuff for dark mode support on the window itself
        using var themeListener = new ThemeRegistryListener();
        themeListener.ThemeChanged += (args) =>
        {
            unsafe
            {
                int darkMode = args ? 1 : 0;
                DwmSetWindowAttribute(Handle,
                    (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    &darkMode, sizeof(int));
            }
        };

        // Mica (mandatory)
        unsafe
        {
            var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &backdrop, sizeof(int));
            int dark = themeListener.IsLightTheme() ? 0 : 1;
            DwmSetWindowAttribute(Handle, (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &dark, sizeof(int));
        }
    }

    /// <summary>
    /// Closes the current window. If you want to intercept the closing process,
    /// subscribe to the <see cref="OnClosing"/> event.
    /// </summary>
    public void Close()
    {
        if (Closed) return;

        // Trigger the closing event and handle cancellation
        var args = new IslandsWindowClosingEventArgs();
        OnClosing?.Invoke(this, args);
        if (args.Handled) return;

        // Persistence
        SaveWindowState();
        Closed = true;

        // Unsubscribe from leftover events
        if (AppWindow != null)
        {
            AppWindow.Closing -= OnAppWindowClosing;
        }

        // Dispose and trigger the final close event
        Dispose();
        OnClosed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (AppWindow != null)
        {
            AppWindow.Closing -= OnAppWindowClosing;
        }

        if (_thisHandle.IsAllocated)
        {
            SetWindowLongPtrW(Handle, GWLP.GWLP_USERDATA, IntPtr.Zero);
            _thisHandle.Free();
        }

        _nativeSource.Dispose();
        _nativeSource = default;
        //_coreWindowInterop.Dispose();
        //_coreWindowInterop = default;

        _desktopWindowXamlSource?.Dispose();
        //_coreWindow = null;

        AppWindow?.Destroy();
        AppWindow = null;

        if (Handle != HWND.NULL)
        {
            DestroyWindow(Handle);
            Handle = HWND.NULL;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Activates the current window.
    /// </summary>
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

    /// <summary>
    /// Minimizes the current window.
    /// </summary>
    public void Minimize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Minimize();
    }

    /// <summary>
    /// Maximizes the current window.
    /// </summary>
    public void Maximize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Maximize();
    }

    /// <summary>
    /// Restores the current window.
    /// </summary>
    public void Restore()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter.As<OverlappedPresenter>()).Restore();
    }

    /// <summary>
    /// Brings the current window to the front.
    /// </summary>
    public void BringToFront()
    {
        ShowWindow(Handle, SW.SW_SHOW);
        SetForegroundWindow(Handle);
        SetActiveWindow(Handle);
    }

    /// <summary>
    /// Moves and resizes the window.
    /// </summary>
    /// <param name="x">Horizontal position</param>
    /// <param name="y">Vertical position</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    public void MoveAndResize(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Moves the window.
    /// </summary>
    /// <param name="x">Horizontal position</param>
    /// <param name="y">Vertical position</param>
    public void Move(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Resizes the window.
    /// </summary>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Centers the window to the working area of the current display.
    /// </summary>
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

    /// <summary>
    /// Set extended window style bits.
    /// </summary>
    /// <param name="style">Extended window style flags</param>
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

    /// <summary>
    /// Set window style bits.
    /// </summary>
    /// <param name="style">Window style flags</param>
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

    /// <summary>
    /// Set the window opacity (0-255).
    /// </summary>
    /// <param name="opacity">Value between 0 and 255 that indicates the window opacity. 0 is invisible and 255 is fully opaque.</param>
    public void SetWindowOpacity(byte opacity)
    {
        SetExtendedWindowStyle(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LAYERED);
        SetLayeredWindowAttributes(Handle, 0, opacity, LWA.LWA_ALPHA);
    }
    
    /// <summary>
    /// Native message pump for the current window object.
    /// </summary>
    /// <param name="msg">Win32 message</param>
    /// <returns></returns>
    public unsafe bool PreTranslateMessage(MSG* msg)
    {
        if (!_xamlInitialized) return false;
        BOOL outResult = false;
        if (!Closed && _nativeSource.Get() != null)
            ThrowIfFailed(_nativeSource.Get()->PreTranslateMessage(msg, &outResult));
        return outResult;
    }

    private unsafe void ForceActivateWindow()
    {
        if (Handle == HWND.NULL) return;

        // Make sure window is visible and up-to-date
        ShowWindow(Handle, SW.SW_SHOW);
        BringWindowToTop(Handle);

        // Get the current foreground window and thread ids
        HWND fgWnd = GetForegroundWindow();
        uint fgThread = GetWindowThreadProcessId(fgWnd, null);
        uint curThread = GetCurrentThreadId();

        // If there is no foreground window or it's the same thread, just try normally
        if (fgWnd == HWND.NULL || fgThread == curThread)
        {
            // Try the usual sequence first
            AllowSetForegroundWindow(ASFW_ANY);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);
            SetFocus(Handle);
            return;
        }

        // Attach input queues so we can temporarily act as the foreground thread
        BOOL attached = AttachThreadInput(curThread, fgThread, true);

        if (attached != 0)
        {
            try
            {
                // Now perform the activation calls while input queues are attached
                AllowSetForegroundWindow(ASFW_ANY);
                SetForegroundWindow(Handle);
                SetActiveWindow(Handle);
                SetFocus(Handle);

                // Ensure window is shown and topmost in z-order for a moment
                BringWindowToTop(Handle);
                SetWindowPos(Handle, HWND.HWND_TOP, 0, 0, 0, 0,
                    SWP.SWP_NOMOVE | SWP.SWP_NOSIZE | SWP.SWP_NOACTIVATE);
            }
            finally
            {
                // Detach the input queues
                AttachThreadInput(curThread, fgThread, false);
            }
        }
        else
        {
            // fallback: try the normal calls (some systems may still honor these)
            AllowSetForegroundWindow(ASFW_ANY);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);
            SetFocus(Handle);
        }
    }

    private unsafe (int X, int Y, int Width, int Height) GetDefaultUwpLikeSize()
    {
        if (Handle == HWND.NULL)
            return (0, 0, 800, 600); // fallback in case Handle isn't valid yet

        // Logical default (from UWP defaults at 150% scale)
        const double baseWidth = 1215.0;
        const double baseHeight = 940.0;

        // Get working area (not full monitor bounds)
        var monitor = MonitorFromWindow(Handle, 2); // MONITOR_DEFAULTTONEAREST
        MONITORINFO info = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(monitor, &info);

        int workWidth = info.rcWork.right - info.rcWork.left;
        int workHeight = info.rcWork.bottom - info.rcWork.top;

        // Convert logical to physical
        double desiredWidth = baseWidth;
        double desiredHeight = baseHeight;

        // Shrink if needed to fit in work area (keep aspect)
        double widthRatio = desiredWidth / workWidth;
        double heightRatio = desiredHeight / workHeight;
        double scaleDown = Math.Max(widthRatio, heightRatio);

        if (scaleDown > 1.0)
        {
            desiredWidth /= scaleDown;
            desiredHeight /= scaleDown;
        }

        int finalWidth = (int)Math.Round(desiredWidth);
        int finalHeight = (int)Math.Round(desiredHeight);

        // Center in work area
        int x = info.rcWork.left + 50;
        int y = info.rcWork.top + 50;

        return (x, y, finalWidth, finalHeight);
    }

    private unsafe void ApplyInitialPlacement()
    {
        if (Handle == HWND.NULL) return;

        WINDOWPLACEMENT placement = new()
        {
            length = (uint)sizeof(WINDOWPLACEMENT)
        };

        if (IsPersistenceEnabled)
        {
            // Try to load persisted placement
            var flags = SettingsManager.GetValue($"{PersistenceKey}_Flags", PersistenceFileName, 0u);
            var showCmd = SettingsManager.GetValue($"{PersistenceKey}_ShowCmd", PersistenceFileName, 0u);
            var left = SettingsManager.GetValue($"{PersistenceKey}_Left", PersistenceFileName, -1);
            var top = SettingsManager.GetValue($"{PersistenceKey}_Top", PersistenceFileName, -1);
            var right = SettingsManager.GetValue($"{PersistenceKey}_Right", PersistenceFileName, -1);
            var bottom = SettingsManager.GetValue($"{PersistenceKey}_Bottom", PersistenceFileName, -1);

            // If we have valid persisted data, use it
            if (left >= 0 && top >= 0 && right > left && bottom > top)
            {
                placement.flags = flags;
                placement.showCmd = showCmd;
                placement.rcNormalPosition = new RECT
                {
                    left = left,
                    top = top,
                    right = right,
                    bottom = bottom
                };

                // Apply window placement (this will restore maximized state too)
                SetWindowPlacement(Handle, &placement);

                // If window was maximized, we stop here — Windows will handle sizing/pos
                if (showCmd == (uint)SW.SW_MAXIMIZE)
                {
                    ShowWindow(Handle, SW.SW_MAXIMIZE);
                    return;
                }

                // Otherwise update logical props
                var scale = Display.GetScale(Handle);
                Width = (int)((right - left) / scale);
                Height = (int)((bottom - top) / scale);
                X = (int)(left / scale);
                Y = (int)(top / scale);
                return;
            }
        }

        // No persisted data or first launch - use default size
        var currentScale = Display.GetScale(Handle);
        int physicalWidth = (int)(Width * currentScale);
        int physicalHeight = (int)(Height * currentScale);

        if (X >= 0 && Y >= 0)
        {
            // User specified position
            int physicalX = (int)(X * currentScale);
            int physicalY = (int)(Y * currentScale);
            SetWindowPos(Handle, HWND.NULL, physicalX, physicalY, physicalWidth, physicalHeight,
                SWP_NOZORDER | SWP_NOACTIVATE);
        }
        else
        {
            // Center window
            SetWindowPos(Handle, HWND.NULL, 0, 0, physicalWidth, physicalHeight,
                SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOMOVE);
            CenterWindow();
        }

        ShowWindow(Handle, SW.SW_SHOWNORMAL);
    }

    private unsafe void SaveWindowState()
    {
        if (!IsPersistenceEnabled || Handle == HWND.NULL) return;

        WINDOWPLACEMENT placement = new()
        {
            length = (uint)sizeof(WINDOWPLACEMENT)
        };

        if (GetWindowPlacement(Handle, &placement) == 0)
            return;

        // Save the complete WINDOWPLACEMENT structure
        // rcNormalPosition always contains the restored window position, even when maximized
        SettingsManager.SetValue($"{PersistenceKey}_Flags", PersistenceFileName, placement.flags);
        SettingsManager.SetValue($"{PersistenceKey}_ShowCmd", PersistenceFileName, placement.showCmd);
        SettingsManager.SetValue($"{PersistenceKey}_Left", PersistenceFileName, placement.rcNormalPosition.left);
        SettingsManager.SetValue($"{PersistenceKey}_Top", PersistenceFileName, placement.rcNormalPosition.top);
        SettingsManager.SetValue($"{PersistenceKey}_Right", PersistenceFileName, placement.rcNormalPosition.right);
        SettingsManager.SetValue($"{PersistenceKey}_Bottom", PersistenceFileName, placement.rcNormalPosition.bottom);
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var closingArgs = new IslandsWindowClosingEventArgs();
        OnClosing?.Invoke(this, closingArgs);

        if (closingArgs.Handled)
        {
            args.Cancel = true;
            return;
        }

        SaveWindowState();

        Closed = true;
        Dispose();
        OnClosed?.Invoke(this, EventArgs.Empty);
    }

    internal void OnResize(int physicalWidth, int physicalHeight)
    {
        if (_xamlHwnd != default)
        {
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, physicalWidth, physicalHeight,
                SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
        }

        if (CoreWindowPriv._coreHwnd != default)
        {
            SendMessageW(CoreWindowPriv._coreHwnd, WM_SIZE, (WPARAM)physicalWidth, (LPARAM)physicalHeight);
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
        if (CoreWindowPriv._coreHwnd != default)
            SendMessageW(CoreWindowPriv._coreHwnd, message, wParam, lParam);
    }

    internal void OnSetFocus()
    {
        if (_xamlHwnd != default)
            SetFocus(_xamlHwnd);
    }

    partial void OnTitleChanged(string oldValue, string newValue)
    {
        if (AppWindow != null)
            AppWindow.Title = newValue;
    }

    partial void OnContentChanged(UIElement? value)
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(IslandsWindow));
    }
}