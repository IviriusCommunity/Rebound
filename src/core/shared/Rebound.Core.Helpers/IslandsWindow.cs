// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Rebound.Core.Helpers;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
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
    private ComPtr<IDesktopWindowXamlSourceNative2> _nativeSource;
    private ComPtr<ICoreWindowInterop> _coreWindowInterop;

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

        // pin instance and store pointer in window userdata
        _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal); // normal is fine; we free in Dispose
        IntPtr gcPtr = GCHandle.ToIntPtr(_thisHandle);

        // CreateWindowEx: class name is static, Title can change — pass Title's pointer temporarily
        fixed (char* className = WindowClassName)
        fixed (char* windowName = Title)
        {
            // 1. Create window hidden but with WS_EX_APPWINDOW
            Handle = CreateWindowExW(
                WS_EX_NOREDIRECTIONBITMAP | WS_EX_APPWINDOW, // ensure taskbar presence
                className,
                windowName,
                0, // hidden initially
                X == default ? CW_USEDEFAULT : X,
                Y == default ? CW_USEDEFAULT : Y,
                Width == default ? CW_USEDEFAULT : Width,
                Height == default ? CW_USEDEFAULT : Height,
                HWND.NULL,
                HMENU.NULL,
                GetModuleHandleW(null),
                null);

            // 2. Store pointer/userdata
            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, gcPtr);

            // 3. Create AppWindow wrapper
            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindow.Closing += OnAppWindowClosing;
            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            // 4. Initialize XAML (now you can do heavy work without showing)
            InitializeXaml();

            // 5. Set visible style and update frame
            SetWindowLongPtrW(Handle, GWL.GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
            SetWindowPos(Handle, HWND.HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            // 6. Show window and bring to front
            ShowWindow(Handle, SW.SW_SHOW);
            SetForegroundWindow(Handle);
            SetActiveWindow(Handle);

            // message loop: keep simple and minimal per-iteration work
            MSG msg;
            while (GetMessageW(&msg, HWND.NULL, 0, 0))
            {
                if (!_closed)
                {
                    // PreTranslateMessage uses cached native pointer now => cheap
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
        if (AppWindow != null)
            AppWindow.Move(new(value, Y));
    }

    partial void OnYChanged(int value)
    {
        if (AppWindow != null)
            AppWindow.Move(new(X, value));
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
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).PreferredMinimumWidth = value;
    }

    partial void OnMaxWidthChanged(int value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).PreferredMaximumWidth = value;
    }

    partial void OnMinHeightChanged(int value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).PreferredMinimumHeight = value;
    }

    partial void OnMaxHeightChanged(int value)
    {
        if (AppWindow != null && AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
            (AppWindow.Presenter as OverlappedPresenter).PreferredMaximumHeight = value;
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