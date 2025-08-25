using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Content;
using Rebound.Generators;
using Rebound.Views;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using WinRT;
using static TerraFX.Interop.Windows.SW;
using static TerraFX.Interop.Windows.SWP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using Colors = Windows.UI.Colors;

namespace Rebound;

//[ReboundApp("Rebound.Hub", "")]
public partial class App : Application
{
    public static HWND _hwnd = default;
    private HWND _xamlHwnd = default;
    private bool _xamlInitialized = false; private HWND _coreHwnd = default;

    private DesktopWindowXamlSource _desktopWindowXamlSource = null;
    private WindowsXamlManager _xamlManager = null;
    internal Frame Frame = null;
    private CoreWindow _coreWindow = null;
    private ContentIsland contentIsland = null;

    private ComPtr<IDesktopWindowXamlSourceNative2> _nativeSource = default;

    public App(HWND hwnd)
    {
        _hwnd = hwnd;
        InitializeXaml();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void InternalLoadLibrary(string lib)
    {
        fixed (char* libName = lib)
            LoadLibraryW(libName);
    }

    private unsafe void InitializeXaml()
    {
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

        nativeSource.Get()->AttachToWindow(_hwnd);
        nativeSource.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _xamlHwnd));

        // Resize to client
        RECT wRect;
        GetClientRect(_hwnd, &wRect);
        SetWindowPos(_xamlHwnd, HWND.NULL, 0, 0, wRect.right - wRect.left, wRect.bottom - wRect.top,
            SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER);

        _coreWindow = CoreWindow.GetForCurrentThread();

        using ComPtr<ICoreWindowInterop> interop = default;
        ThrowIfFailed(((IUnknown*)((IWinRTObject)_coreWindow).NativeObject.ThisPtr)->QueryInterface(__uuidof<ICoreWindowInterop>(), (void**)interop.GetAddressOf()));
        interop.Get()->get_WindowHandle((HWND*)Unsafe.AsPointer(ref _coreHwnd));

        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        Frame = new Frame();
        _desktopWindowXamlSource.Content = Frame;

        _xamlInitialized = true;
        OnXamlInitialized();

        ThemeListener themeListener = new ThemeListener();
        themeListener.ThemeChanged += (args) =>
        {
            unsafe
            {
                if (args.CurrentTheme == ApplicationTheme.Dark)
                {
                    int darkMode = 1;
                    DwmSetWindowAttribute(_hwnd,
                        (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        &darkMode, sizeof(int));
                }
                else
                {
                    int darkMode = 0;
                    DwmSetWindowAttribute(_hwnd,
                        (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        &darkMode, sizeof(int));
                }
            }
        };

        unsafe
        {
            var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(_hwnd,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                &backdrop, sizeof(int));

            int darkMode = themeListener.CurrentTheme == ApplicationTheme.Light ? 0 : 1;
            DwmSetWindowAttribute(_hwnd,
                (uint)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &darkMode, sizeof(int));
        }
    }

    private void OnXamlInitialized()
    {
        Frame.Navigate(typeof(ShellPage));
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
        ComPtr<IDesktopWindowXamlSourceNative2> nativeSource = default;
        ThrowIfFailed(((IUnknown*)((IWinRTObject)_desktopWindowXamlSource).NativeObject.ThisPtr)
            ->QueryInterface(__uuidof<IDesktopWindowXamlSourceNative2>(), (void**)nativeSource.GetAddressOf()));

        nativeSource.Get()->PreTranslateMessage(msg, &result);
        return result;
    }

    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        /*if (e.IsFirstLaunch)
        {
            App.Current.UnhandledException += Current_UnhandledException;
            CreateMainWindow();
        }
        else
        {
            if (MainAppWindow != null)
            {
                _ = ((MainWindow)MainAppWindow).BringToFront();
            }
            else
            {
                CreateMainWindow();
            }
            return;
        }*/
    }

    /*private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
    }*/

    public static void CreateMainWindow()
    {
        /*MainAppWindow = new MainWindow();
        MainAppWindow.Activate();*/
    }
}