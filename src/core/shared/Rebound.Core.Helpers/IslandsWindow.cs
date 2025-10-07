// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using System;
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
public class IslandsWindowClosingEventArgs : EventArgs
{
    public bool Handled { get; set; } = false;
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
        var dlg = new ReboundDialog(title, message, icon);
        dlg.Create(); // creates and shows window
        await dlg._tcs.Task;
    }

    public ReboundDialog(string title, string message, DialogIcon icon)
    {
        Title = title;

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
                Padding = new Thickness(8),
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
            this.Title = title;
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow?.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            this.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
            this.Width = 480;
            this.Height = 256;
            this.IsMaximizable = false;
            this.IsMinimizable = false;
            this.IsResizable = false;
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

            PInvoke.GetDIBits(hdcMem, hBitmap, 0, (uint)height, pPixels, &bmi, Windows.Win32.Graphics.Gdi.DIB_USAGE.DIB_RGB_COLORS);
        }

        // Clean up GDI resources
        PInvoke.SelectObject(hdcMem, old);
        PInvoke.DeleteDC(hdcMem);
        PInvoke.ReleaseDC(new(), hdcScreen);
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

    bool _closed;

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

    static IslandsWindow()
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
    [ObservableProperty] public partial int Width { get; set; }
    [ObservableProperty] public partial int Height { get; set; }
    [ObservableProperty] public partial int X { get; set; }
    [ObservableProperty] public partial int Y { get; set; }
    [ObservableProperty] public partial int MaxWidth { get; set; } = int.MaxValue;
    [ObservableProperty] public partial int MaxHeight { get; set; } = int.MaxValue;
    [ObservableProperty] public partial string Title { get; set; } = "UWP XAML Islands Window";
    [ObservableProperty] public partial UIElement Content { get; set; }
    [ObservableProperty] public partial bool IsPersistenceEnabled { get; set; } = false;
    [ObservableProperty] public partial string PersistenceKey { get; set; } = nameof(IslandsWindow);
    [ObservableProperty] public partial string PersistanceFileName { get; set; } = "rebound";

    public event EventHandler<XamlInitializedEventArgs>? XamlInitialized;
    public event EventHandler<AppWindowInitializedEventArgs>? AppWindowInitialized;
    public event EventHandler<IslandsWindowClosingEventArgs>? Closing;
    public event EventHandler? Closed;

    public IslandsWindow() { }

    public void Activate()
    {
        if (AppWindow != null)
        {
            if (TerraFX.Interop.Windows.Windows.IsIconic(Handle) != 0) // if minimized
            {
                TerraFX.Interop.Windows.Windows.ShowWindow(Handle, TerraFX.Interop.Windows.SW.SW_RESTORE); // restore window
            }
            TerraFX.Interop.Windows.Windows.SetForegroundWindow(Handle); // 
        }
        else
        {
            Create();
        }
    }

    public void Minimize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).Minimize();
    }

    public void Maximize()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).Maximize();
    }

    public void Restore()
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).Restore();
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

    private void LoadWindowState()
    {
        if (!IsPersistenceEnabled) return;

        X = SettingsHelper.GetValue($"{PersistenceKey}_X", PersistanceFileName, X);
        Y = SettingsHelper.GetValue($"{PersistenceKey}_Y", PersistanceFileName, Y);
        Width = SettingsHelper.GetValue($"{PersistenceKey}_Width", PersistanceFileName, Width == 0 ? 800 : Width);
        Height = SettingsHelper.GetValue($"{PersistenceKey}_Height", PersistanceFileName, Height == 0 ? 600 : Height);
    }

    private void SaveWindowState()
    {
        if (!IsPersistenceEnabled) return;

        SettingsHelper.SetValue($"{PersistenceKey}_X", PersistanceFileName, X);
        SettingsHelper.SetValue($"{PersistenceKey}_Y", PersistanceFileName, Y);
        SettingsHelper.SetValue($"{PersistenceKey}_Width", PersistanceFileName, Width);
        SettingsHelper.SetValue($"{PersistenceKey}_Height", PersistanceFileName, Height);
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
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        IntPtr gcPtr = GCHandle.ToIntPtr(_thisHandle);

        fixed (char* className = WindowClassName)
        fixed (char* windowName = Title)
        {
            Handle = CreateWindowExW(
                WS_EX_NOREDIRECTIONBITMAP | WS_EX_APPWINDOW,
                className,
                windowName,
                0,
                X == default ? CW_USEDEFAULT : X,
                Y == default ? CW_USEDEFAULT : Y,
                Width == default ? CW_USEDEFAULT : Width,
                Height == default ? CW_USEDEFAULT : Height,
                HWND.NULL,
                HMENU.NULL,
                GetModuleHandleW(null),
                null);

            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, gcPtr);

            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindow.Closing += OnAppWindowClosing;
            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            LoadWindowState();

            InitializeXaml();

            SetWindowLongPtrW(Handle, GWL.GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
            SetWindowPos(Handle, HWND.HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            ShowWindow(Handle, SW.SW_SHOW);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);

            MSG msg;
            while (GetMessageW(&msg, HWND.NULL, 0, 0))
            {
                if (!_closed)
                {
                    if (!PreTranslateMessage(&msg))
                    {
                        TranslateMessage(&msg);
                        DispatchMessageW(&msg);
                    }
                }
                else return;
            }
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
    internal unsafe bool PreTranslateMessage(MSG* msg)
    {
        if (!_xamlInitialized) return false;
        BOOL outResult = false;
        // _nativeSource is cached; avoid re-QI every time
        if (!_closed && _nativeSource.Get() != null) ThrowIfFailed(_nativeSource.Get()->PreTranslateMessage(msg, &outResult));
        return outResult;
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

    // Add these fields near the top with other fields
    private bool _internalResize = false;

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

    // Update the OnResize method to set the flag and handle DPI:
    internal void OnResize(int width, int height)
    {
        // avoid calling if not set
        if (_xamlHwnd != default)
        {
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, width, height, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
        }

        if (_coreHwnd != default)
        {
            SendMessageW(_coreHwnd, WM_SIZE, (WPARAM)width, (LPARAM)height);
        }

        // Convert physical pixels to logical pixels
        var logicalWidth = (int)(width / Display.GetScale(AppWindow));
        var logicalHeight = (int)(height / Display.GetScale(AppWindow));

        // Set flag to prevent circular updates
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
            (AppWindow.Presenter as OverlappedPresenter).IsResizable = value;
    }

    partial void OnIsMaximizableChanged(bool value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).IsMaximizable = value;
    }

    partial void OnIsMinimizableChanged(bool value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).IsMinimizable = value;
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
        if (_disposed) throw new ObjectDisposedException(nameof(IslandsWindow));
    }
}