using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

namespace Rebound.Core.Helpers;

public class XamlInitializedEventArgs : EventArgs
{

}

public class AppWindowInitializedEventArgs : EventArgs
{

}

public partial class IslandsWindow : ObservableObject
{
    private delegate LRESULT WndProcDelegate(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam);

    private HWND _xamlHwnd;
    private bool _xamlInitialized; 
    private HWND _coreHwnd;

    private DesktopWindowXamlSource? _desktopWindowXamlSource;
    private WindowsXamlManager? _xamlManager;
    private CoreWindow? _coreWindow;

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

    [UnmanagedCallersOnly]
    private static unsafe LRESULT WndProcStatic(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        // Retrieve the IslandsWindow instance from GWLP_USERDATA
        var ptr = GetWindowLongPtr(hwnd, GWLP.GWLP_USERDATA);
        if (ptr == IntPtr.Zero)
            return DefWindowProc(hwnd, msg, wParam, lParam);

        var handle = GCHandle.FromIntPtr(ptr);
        var window = (IslandsWindow)handle.Target;

        return window.WndProc(hwnd, msg, wParam, lParam); // call instance method
    }

    public IslandsWindow()
    {

    }

    public unsafe void Activate()
    {
        // Pin this instance so it can be retrieved from static WndProc
        _thisHandle = GCHandle.Alloc(this);

        fixed (char* className = "XamlIslandsClass")
        fixed (char* windowName = Title)
        {
            WNDCLASSW wc = new();
            wc.lpfnWndProc = &WndProcStatic; // use static unmanaged WndProc
            wc.lpszClassName = className;
            wc.hInstance = GetModuleHandleW(null);
            RegisterClassW(&wc);

            Handle = CreateWindowExW(
                WS_EX_NOREDIRECTIONBITMAP,
                className,
                windowName,
                WS_OVERLAPPEDWINDOW | WS_VISIBLE,
                CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
                HWND.NULL, HMENU.NULL, wc.hInstance, null);

            // store GCHandle pointer in window
            SetWindowLongPtr(Handle, GWLP.GWLP_USERDATA, GCHandle.ToIntPtr(_thisHandle));

            InitializeXaml();

            // Run message loop
            MSG msg;
            while (GetMessageW(&msg, HWND.NULL, 0, 0))
            {
                bool xamlProcessed = PreTranslateMessage(&msg);
                if (!xamlProcessed)
                {
                    TranslateMessage(&msg);
                    DispatchMessageW(&msg);
                }
            }
        }
    }

    private GCHandle _thisHandle; // add this field to store pinned instance

    ~IslandsWindow()
    {
        if (_thisHandle.IsAllocated)
            _thisHandle.Free();
    }

    private unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_CREATE:
                InitializeXaml();
                break;
            case WM_SIZE:
                OnResize(LOWORD(lParam), HIWORD(lParam));
                break;
            case WM_SETTINGCHANGE:
            case WM_THEMECHANGED:
                ProcessCoreWindowMessage(msg, wParam.Value, lParam.Value);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void InternalLoadLibrary(string lib)
    {
        fixed (char* libName = lib)
            LoadLibraryW(libName);
    }

    public unsafe void InitializeXaml()
    {
        AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(Handle));

        AppWindowInitialized?.Invoke(this, new AppWindowInitializedEventArgs());

        // Load old WinRT libs (optional, for legacy compatibility)
        InternalLoadLibrary("twinapi.appcore.dll");
        InternalLoadLibrary("threadpoolwinrt.dll");

        // Initialize XAML manager for this thread
        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();

        // Create DesktopWindowXamlSource
        _desktopWindowXamlSource = new DesktopWindowXamlSource();

        // Get native interface
        ComPtr<IDesktopWindowXamlSourceNative2> nativeSource = default;
        ThrowIfFailed(((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr)
            ->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)nativeSource.GetAddressOf()));

        nativeSource.Get()->AttachToWindow(Handle);
        nativeSource.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd));

        // Resize to client
        RECT wRect;
        GetClientRect(Handle, &wRect);
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, wRect.right - wRect.left, wRect.bottom - wRect.top,
            SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        _coreWindow = CoreWindow.GetForCurrentThread();

        using ComPtr<ICoreWindowInterop> interop = default;
        ThrowIfFailed(((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr)->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)interop.GetAddressOf()));
        interop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd));

        _xamlInitialized = true;

        XamlInitialized?.Invoke(this, new XamlInitializedEventArgs());

        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));

        using ThemeListener themeListener = new();
        themeListener.ThemeChanged += (args) =>
        {
            unsafe
            {
                if (args.CurrentTheme == ApplicationTheme.Dark)
                {
                    int darkMode = 1;
                    DwmSetWindowAttribute(Handle,
                        (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        &darkMode, sizeof(int));
                }
                else
                {
                    int darkMode = 0;
                    DwmSetWindowAttribute(Handle,
                        (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        &darkMode, sizeof(int));
                }
            }
        };

        unsafe
        {
            var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(Handle,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                &backdrop, sizeof(int));

            int darkMode = themeListener.CurrentTheme == ApplicationTheme.Light ? 0 : 1;
            DwmSetWindowAttribute(Handle,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &darkMode, sizeof(int));
        }
    }

    partial void OnContentChanged(UIElement value)
    {
        if (_desktopWindowXamlSource != null && value != null)
        {
            _desktopWindowXamlSource.Content = value;
        }
    }

    internal void OnResize(int x, int y)
    {
        if (_xamlHwnd != default)
            SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, x, y, SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        if (_coreHwnd != default)
            SendMessageW(_coreHwnd, WM_SIZE, (WPARAM)x, y);
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

    internal unsafe bool PreTranslateMessage(MSG* msg)
    {
        if (!_xamlInitialized) return false;

        BOOL result = false;
        TerraFX.Interop.Windows.ComPtr<IDesktopWindowXamlSourceNative2> nativeSource = default;
        ThrowIfFailed(((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr)
            ->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)nativeSource.GetAddressOf()));

        nativeSource.Get()->PreTranslateMessage(msg, &result);
        return result;
    }
}