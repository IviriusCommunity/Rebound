// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Rebound.Helpers;
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
    private WindowsXamlManager? _xamlManager;
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
    [ObservableProperty] public partial int MaxWidth { get; set; } = int.MaxValue;
    [ObservableProperty] public partial int MaxHeight { get; set; } = int.MaxValue;
    [ObservableProperty] public partial string Title { get; set; } = "UWP XAML Islands Window";
    [ObservableProperty] public partial UIElement Content { get; set; }

    public event EventHandler<XamlInitializedEventArgs>? XamlInitialized;
    public event EventHandler<AppWindowInitializedEventArgs>? AppWindowInitialized;

    public IslandsWindow() { }

    public void Activate()
    {
        if (AppWindow != null)
        {
            AppWindow.Activate();
        }
        else
        {
            Create();
        }
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
            // Create with WS_EX_NOREDIRECTIONBITMAP for XAML islands drawing
            Handle = CreateWindowExW(
                WS_EX_NOREDIRECTIONBITMAP,
                className,
                windowName,
                WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
                HWND.NULL, HMENU.NULL, GetModuleHandleW(null), null);

            // write pointer once
            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, gcPtr);

            // AppWindow from the created HWND (fast)
            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));
            AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

            // Initialize XAML once (no-op if already initialized)
            InitializeXaml();

            // message loop: keep simple and minimal per-iteration work
            MSG msg;
            while (GetMessageW(&msg, HWND.NULL, 0, 0))
            {
                // PreTranslateMessage uses cached native pointer now => cheap
                if (!PreTranslateMessage(&msg))
                {
                    TranslateMessage(&msg);
                    DispatchMessageW(&msg);
                }
            }
        }
    }

    // ---------- XAML initialization (idempotent, cached) ----------
    public unsafe void InitializeXaml()
    {
        ThrowIfDisposed();

        if (_xamlInitialized) return; // fast path if already initialized

        // Initialize XAML manager for the thread
        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();

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
        var themeListener = new ThemeListener();
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
        ThrowIfFailed(_nativeSource.Get()->PreTranslateMessage(msg, &outResult));
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

    internal void OnResize(int width, int height)
    {
        // avoid calling if not set
        if (_xamlHwnd != default)
        {
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, width, height, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);
        }

        if (_coreHwnd != default)
        {
            // SendMessageW's WPARAM/LPARAM expectation — keep same shape as before
            SendMessageW(_coreHwnd, WM_SIZE, (WPARAM)width, (LPARAM)height);
        }

        Width = width;
        Height = height;
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
            var ptr = GCHandle.ToIntPtr(_thisHandle);
            try
            {
                // Clear window userdata if possible
                if (Handle != HWND.NULL)
                    SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, IntPtr.Zero);
            }
            catch { /* swallow - best-effort */ }

            _thisHandle.Free();
        }

        // Release cached COM pointers
        if (_nativeSource.Get() != null)
        {
            _nativeSource.Dispose();
            _nativeSource = default;
        }

        if (_coreWindowInterop.Get() != null)
        {
            _coreWindowInterop.Dispose();
            _coreWindowInterop = default;
        }

        // Release WinRT/XAML objects in deterministic order
        _desktopWindowXamlSource?.Dispose();
        _xamlManager?.Dispose();
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