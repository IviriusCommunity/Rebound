// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.IPC;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Generators;
using Rebound.Shell.Run;
using Rebound.Shell.ShutdownDialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.SWP;
using TerraFX.Interop.DirectX;
using System.Collections.ObjectModel;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

public enum SystemPanelPosition
{
    Left,
    Right,
    Top,
    Bottom
}

public partial class SystemPanel : ObservableObject
{
    [ObservableProperty] public partial SystemPanelPosition Position { get; set; }
    [ObservableProperty] public partial bool AllowFloat { get; set; }
    [ObservableProperty] public partial bool AvoidWindows { get; set; }
    [ObservableProperty] public partial int Size { get; set; }
    [ObservableProperty] public partial int FillSize { get; set; }
}

public sealed class SystemPanelController
{
    public SystemPanel Panel { get; }
    public IslandsWindow Window { get; private set; }

    private DispatcherTimer _proximityTimer;
    private bool _isHidden;
    private const int PROXIMITY_CHECK_INTERVAL = 100; // ms
    private const int HIDE_THRESHOLD = 50; // pixels from panel edge
    private const int ANIMATION_DURATION = 200; // ms

    private RECT _logicalPanelRect;

    public SystemPanelController(SystemPanel panel)
    {
        Panel = panel;
    }

    unsafe void ConfigureWindow()
    {
        var hwnd = Window.Handle;
        var exStyle = GetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE);
        exStyle |= WS.WS_EX_TOOLWINDOW;
        exStyle |= WS.WS_EX_NOACTIVATE;
        exStyle &= ~WS.WS_EX_APPWINDOW;
        SetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE, exStyle);
        var style = GetWindowLongPtrW(hwnd, GWL.GWL_STYLE);
        style &= ~WS.WS_BORDER;
        style &= ~WS.WS_THICKFRAME;
        style &= ~WS.WS_DLGFRAME;
        SetWindowLongPtrW(hwnd, GWL.GWL_STYLE, style);
        SetWindowPos(
            hwnd,
            HWND.HWND_TOPMOST,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
        );
    }

    unsafe void RegisterAppBar()
    {
        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = Window.Handle
        };
        SHAppBarMessage(ABM.ABM_NEW, &abd);
    }

    unsafe void ApplyLayout()
    {
        if (!Panel.AvoidWindows)
            RegisterAppBar(); // ideally once, but this is fine for now
        var monitorSize = Display.GetAvailableRectForWindow(Window.Handle);
        var scale = Display.GetScale(Window.Handle);
        int thickness = (int)(Panel.Size * scale);
        Debug.WriteLine("Thickness: " + thickness);
        APPBARDATA abd = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            hWnd = Window.Handle
        };
        switch (Panel.Position)
        {
            case SystemPanelPosition.Top:
                abd.uEdge = ABE_TOP;
                abd.rc.left = monitorSize.left;
                abd.rc.right = monitorSize.right;
                abd.rc.top = monitorSize.top;
                abd.rc.bottom = thickness;
                break;
            case SystemPanelPosition.Bottom:
                abd.uEdge = ABE_BOTTOM;
                abd.rc.left = monitorSize.left;
                abd.rc.right = monitorSize.right;
                abd.rc.top = monitorSize.bottom - thickness;
                abd.rc.bottom = thickness;
                break;
            case SystemPanelPosition.Left:
                abd.uEdge = ABE_LEFT;
                abd.rc.left = monitorSize.left;
                abd.rc.right = (int)(thickness * scale);
                abd.rc.top = monitorSize.top;
                abd.rc.bottom = monitorSize.bottom;
                break;
            case SystemPanelPosition.Right:
                abd.uEdge = ABE_RIGHT;
                abd.rc.left = (int)(monitorSize.right - thickness * scale);
                abd.rc.right = monitorSize.right;
                abd.rc.top = monitorSize.top;
                abd.rc.bottom = monitorSize.bottom;
                break;
        }
        if (!Panel.AvoidWindows)
        {
            SHAppBarMessage(ABM.ABM_QUERYPOS, &abd);
            // Commit the negotiated rectangle
            SHAppBarMessage(ABM.ABM_SETPOS, &abd);
        }
        _logicalPanelRect = abd.rc;

        Window.MoveAndResize(
            (int)((abd.rc.left) / scale),
            (int)((abd.rc.top) / scale),
            (int)((abd.rc.right - abd.rc.left) / scale),
            (int)((abd.rc.bottom - abd.rc.top) / scale)
        );
        Debug.WriteLine("Window height: " + (abd.rc.bottom - abd.rc.top));
    }

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
            ConfigureWindow();
            ApplyLayout();

            if (Panel.AvoidWindows)
            {
                StartProximityMonitoring();
            }
        };
        Window.XamlInitialized += (_, _) =>
        {
            Window.Content = new Grid()
            {
                Background = new CommunityToolkit.WinUI.Media.AcrylicBrush()
                {
                    BlurAmount = 32,
                    TintOpacity = 0.4,
                    TintColor = Colors.Black,
                    BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop
                }
            };
            //Window.Content = CreatePanelRoot();
        };
        Window.Create();
        Window.MakeWindowTransparent();
    }

    private void StartProximityMonitoring()
    {
        UIThreadQueue.QueueAction(() =>
        {
            _proximityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PROXIMITY_CHECK_INTERVAL)
            };
            _proximityTimer.Tick += async (_, _) => await UIThreadQueue.QueueActionAsync(async () => await CheckWindowProximity());
            _proximityTimer.Start();
        });
    }

    private unsafe struct EnumWindowsState
    {
        public HWND PanelHwnd;
        public HMONITOR MonitorHandle;
        public RECT PanelRect;
        public SystemPanelPosition Position;
        public bool FoundNearbyWindow;
    }

    private unsafe bool IsAnyWindowNearby()
    {
        RECT panelRect = _logicalPanelRect;
        var monitorHandle = MonitorFromWindow(Window.Handle, MONITOR.MONITOR_DEFAULTTONEAREST);

        EnumWindowsState state = new()
        {
            PanelHwnd = Window.Handle,
            MonitorHandle = monitorHandle,
            PanelRect = panelRect,
            Position = Panel.Position,
            FoundNearbyWindow = false
        };

        EnumWindows(&EnumWindowCallback, (LPARAM)(&state));

        return state.FoundNearbyWindow;
    }

    [UnmanagedCallersOnly]
    private static unsafe BOOL EnumWindowCallback(HWND hwnd, LPARAM lParam)
    {
        var state = (EnumWindowsState*)lParam;

        // Skip our own window
        if (hwnd == state->PanelHwnd)
            return true;

        // Only check visible windows
        if (!IsWindowVisible(hwnd))
            return true;

        // Skip minimized windows
        if (IsIconic(hwnd))
            return true;

        // Check extended styles
        var exStyle = GetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE);
        var style = GetWindowLongPtrW(hwnd, GWL.GWL_EXSTYLE);
        bool isToolWindow = (exStyle & WS.WS_EX_TOOLWINDOW) != 0;
        bool isAppWindow = (exStyle & WS.WS_EX_APPWINDOW) != 0;

        // Skip tool windows that aren't explicitly app windows
        if (isToolWindow && !isAppWindow)
            return true;

        // Get window owner - top-level windows have no owner
        HWND owner = GetWindow(hwnd, GW_OWNER);

        // Skip owned windows (like dialogs) unless they have WS_EX_APPWINDOW
        if (owner != HWND.NULL && !isAppWindow)
            return true;

        // Skip windows with no title and no app window style (usually helper windows)
        const int titleBufferSize = 256;
        char* titleBuffer = stackalloc char[titleBufferSize];
        int titleLength = GetWindowTextW(hwnd, titleBuffer, titleBufferSize);
        string title = new string(titleBuffer, 0, titleLength);

        if (titleLength == 0 && !isAppWindow)
            return true;

        if (title == "Windows Input Experience")
            return true;

        RECT windowRect;
        GetWindowRect(hwnd, &windowRect);

        if (IsWindowNearPanel(windowRect, state->PanelRect, state->Position))
        {
            // Get window class name
            const int classBufferSize = 256;
            char* classBuffer = stackalloc char[classBufferSize];
            int classLength = GetClassNameW(hwnd, classBuffer, classBufferSize);
            string windowClass = new string(classBuffer, 0, classLength);

            Debug.WriteLine($"Nearby window - Class: {windowClass}");

            state->FoundNearbyWindow = true;
            return false; // Stop enumeration
        }

        return true;
    }

    private static bool IsWindowNearPanel(RECT windowRect, RECT panelRect, SystemPanelPosition position)
    {
        const int HIDE_THRESHOLD = 50;

        switch (position)
        {
            case SystemPanelPosition.Top:
                return windowRect.top <= panelRect.bottom + HIDE_THRESHOLD;
            case SystemPanelPosition.Bottom:
                return windowRect.bottom >= panelRect.top - HIDE_THRESHOLD;
            case SystemPanelPosition.Left:
                return windowRect.left <= panelRect.right + HIDE_THRESHOLD;
            case SystemPanelPosition.Right:
                return windowRect.right >= panelRect.left - HIDE_THRESHOLD;
            default:
                return false;
        }
    }

    private async Task AnimateShowAsync()
    {
        _isHidden = false;
        await AnimatePositionAsync(0);
    }

    private int GetCurrentOffset(RECT rect)
    {
        var monitorSize = Display.GetAvailableRectForWindow(Window.Handle);

        switch (Panel.Position)
        {
            case SystemPanelPosition.Top:
                return monitorSize.top - rect.top;
            case SystemPanelPosition.Bottom:
                return rect.bottom - monitorSize.bottom;
            case SystemPanelPosition.Left:
                return monitorSize.left - rect.left;
            case SystemPanelPosition.Right:
                return rect.right - monitorSize.right;
            default:
                return 0;
        }
    }

    private async Task CheckWindowProximity()
    {
        bool shouldHide = IsAnyWindowNearby();

        if (shouldHide && !_isHidden)
        {
            await AnimateHideAsync();
            Debug.WriteLine("Hiding");
        }
        else if (!shouldHide && _isHidden)
        {
            await AnimateShowAsync();
            Debug.WriteLine("Showing");
        }
    }

    private async Task AnimateHideAsync()
    {
        _isHidden = true;
        var scale = Display.GetScale(Window.Handle);
        int thickness = (int)(Panel.Size * scale);
        await AnimatePositionAsync(thickness);
    }

    private async Task AnimatePositionAsync(int targetOffset)
    {
        var startTime = DateTime.Now;
        var scale = Display.GetScale(Window.Handle);

        RECT currentRect;
        unsafe
        {
            GetWindowRect(Window.Handle, &currentRect);
        }

        var startOffset = GetCurrentOffset(currentRect);

        var delta = targetOffset - startOffset;

        while (true)
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / ANIMATION_DURATION, 1.0);

            var easedProgress = 1 - Math.Pow(1 - progress, 3);

            var currentOffset = startOffset + delta * easedProgress;

            UIThreadQueue.QueueAction(() =>
            {
                ApplyOffset((int)Math.Round(currentOffset), scale);
            });

            if (progress >= 1.0)
                return;

            await Task.Delay(16);
        }
    }

    private unsafe void ApplyOffset(int offset, double scale)
    {
        var monitorSize = Display.GetAvailableRectForWindow(Window.Handle);
        int thickness = (int)(Panel.Size * scale);

        int x = 0, y = 0, width = 0, height = 0;

        switch (Panel.Position)
        {
            case SystemPanelPosition.Top:
                x = monitorSize.left;
                y = monitorSize.top - offset;
                width = monitorSize.right - monitorSize.left;
                height = thickness;
                break;

            case SystemPanelPosition.Bottom:
                x = monitorSize.left;
                y = monitorSize.bottom - thickness + offset;
                width = monitorSize.right - monitorSize.left;
                height = thickness;
                break;

            case SystemPanelPosition.Left:
                x = (int)(monitorSize.left - offset * scale);
                y = monitorSize.top;
                width = (int)(thickness * scale);
                height = monitorSize.bottom - monitorSize.top;
                break;

            case SystemPanelPosition.Right:
                x = (int)(monitorSize.right + (- thickness + offset) * scale);
                y = monitorSize.top;
                width = (int)(thickness * scale);
                height = monitorSize.bottom - monitorSize.top;
                break;
        }

        Task.Run(() =>
            SetWindowPos(
                Window.Handle,
                HWND.HWND_TOPMOST,
                (int)(x),
                (int)(y),
                (int)(width),
                (int)(height),
                SWP_NOACTIVATE
        ));
    }

    public void Dispose()
    {
        _proximityTimer?.Stop();
        _proximityTimer = null;
    }
}

public class SystemPanelsService
{
    public ObservableCollection<SystemPanel> PanelItems { get; } = new();
    private readonly List<SystemPanelController> _controllers = new();

    public void Initialize()
    {
        foreach (var panel in PanelItems)
            SpawnPanel(panel);

        //PanelItems.CollectionChanged += OnPanelsChanged;
    }

    void SpawnPanel(SystemPanel panel)
    {
        UIThreadQueue.QueueAction(() =>
        {
            var controller = new SystemPanelController(panel);
            controller.Create();
            _controllers.Add(controller);
        });
    }
}

public partial class StartMenuService : ObservableObject
{
    [ObservableProperty]
    public partial bool IsStartMenuOpen { get; set; }
}

[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    private static HWND? _previousFocusedWindow = null;

    public static StartMenuService StartMenuService { get; } = new();

    public static PipeClient? ReboundPipeClient { get; private set; }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            WindowList.KeepAlive = true;

            // Initialize pipe client if not already
            ReboundPipeClient ??= new();

            // Start listening (optional, for future messages)
            ReboundPipeClient.MessageReceived += OnPipeMessageReceived;

            // Pipe server thread
            var pipeThread = new Thread(async () =>
            {
                try
                {
                    await Task.Delay(1000);
                    await ReboundPipeClient.ConnectAsync().ConfigureAwait(false);
                    UIThreadQueue.QueueAction(async () =>
                    {
#if DEBUG
                        // Create the window
                        TestShellWindow = new IslandsWindow()
                        {
                            IsPersistenceEnabled = false,
                        };

                        // AppWindow init
                        TestShellWindow.AppWindowInitialized += (s, e) =>
                        {
                            TestShellWindow.Title = "Rebound Shell";
                            TestShellWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                            TestShellWindow.AppWindow?.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
                            TestShellWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                            TestShellWindow.AppWindow?.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, 120, 120, 120);
                            TestShellWindow.AppWindow?.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 120, 120, 120);
                            TestShellWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                        };

                        // Load main page
                        TestShellWindow.XamlInitialized += (s, e) =>
                        {
                            var frame = new FullShellTestPage();
                            TestShellWindow.Content = frame;
                        };

                        // Spawn the window
                        TestShellWindow.Create();
                        TestShellWindow.MakeWindowTransparent();
                        TestShellWindow.Maximize();
                        TestShellWindow.SetAlwaysOnTop(true);
                        unsafe
                        {
                            var exStyle = GetWindowLongPtrW(TestShellWindow.Handle, GWL.GWL_EXSTYLE);
                            exStyle |= WS.WS_EX_TOOLWINDOW;
                            exStyle &= ~WS.WS_EX_APPWINDOW;
                            SetWindowLongPtrW(TestShellWindow.Handle, GWL.GWL_EXSTYLE, exStyle);
                            SetWindowPos(
                                App.TestShellWindow!.Handle,
                                HWND.NULL,
                                0, 0, 0, 0,
                                SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW | SWP_FRAMECHANGED);
                            const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
                            int trueValue = 1;
                            DwmSetWindowAttribute(App.TestShellWindow!.Handle, DWMWA_TRANSITIONS_FORCEDISABLED, &trueValue, sizeof(int));

                        }
#endif
                    });
                }
                catch
                {
                    UIThreadQueue.QueueAction(async () =>
                    {
                        await ReboundDialog.ShowAsync(
                            "Rebound Service Host not found.",
                            "Could not find Rebound Service Host.\nPlease ensure it is running in the background.",
                            DialogIcon.Warning
                        ).ConfigureAwait(false);
                    });
                }
            })
            {
                IsBackground = true,
                Name = "Pipe Server Thread"
            };
            pipeThread.SetApartmentState(ApartmentState.STA);
            pipeThread.Start();

            // Run pipe server in a dedicated background thread
            Thread pipeServerThread = new(async () =>
            {
                using var pipeServer = new PipeHost("REBOUND_SHELL", AccessLevel.Everyone);
                pipeServer.MessageReceived += PipeServer_MessageReceived;

                await pipeServer.StartAsync();
            })
            {
                IsBackground = true,
                Name = "ShellPipeServerThread"
            };

            pipeServerThread.Start();
#if RELEASE
            // Create the window
            using var MainWindow = new IslandsWindow()
            {
                IsPersistenceEnabled = false,
                PersistenceKey = "Rebound.Shell.GhostWindow",
                Width = 0,
                Height = 0,
                X = -9999,
                Y = -9999
            };

            // AppWindow init
            MainWindow.AppWindowInitialized += (s, e) =>
            {
                // Window metrics
                MainWindow.MinWidth = 0;
                MainWindow.MinHeight = 0;
                MainWindow.MaxWidth = 0;
                MainWindow.MaxHeight = 0;

                // Window properties
                MainWindow.IsMaximizable = false;
                MainWindow.IsMinimizable = false;
                MainWindow.SetWindowOpacity(0);
            };

            // Load main page
            MainWindow.XamlInitialized += (s, e) =>
            {
                var frame = new Button();
                MainWindow.Content = frame;
            };

            // Spawn the window
            MainWindow.Create();
#endif

            var panels = new SystemPanelsService();
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Top,
                Size = 40,
                AvoidWindows = false,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Bottom,
                Size = 40,
                AvoidWindows = false,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Left,
                Size = 40,
                AvoidWindows = false,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Right,
                Size = 40,
                AvoidWindows = false,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Top,
                Size = 60,
                AvoidWindows = true,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Bottom,
                Size = 60,
                AvoidWindows = true,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Left,
                Size = 60,
                AvoidWindows = true,
                AllowFloat = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Right,
                Size = 60,
                AvoidWindows = true,
                AllowFloat = false
            });

            panels.Initialize();
        }
        else Process.GetCurrentProcess().Kill();
    }

    private static void OnPipeMessageReceived(string message)
    {

    }

    private void PipeServer_MessageReceived(PipeConnection connection, string arg)
    {
        var parts = arg.Split("##");
        if (parts[0] == "Shell::SpawnRunWindow")
        {
            if (SettingsManager.GetValue("RunBoxUseCommandPalette", "rshell", false))
            {
                UIThreadQueue.QueueAction(async () =>
                {
                    await Launcher.LaunchUriAsync(new("x-cmdpal:///"));
                    await Task.Delay(50);
                    HWND hwnd;
                    nint rawHwnd;
                    unsafe
                    {
                        hwnd = TerraFX.Interop.Windows.Windows.FindWindowExW(HWND.NULL, HWND.NULL, "WinUIDesktopWin32WindowClass".ToPointer(), "Command Palette".ToPointer());
                        rawHwnd = (nint)hwnd;
                    }
                    await ReboundPipeClient.SendAsync("Shell::BringWindowToFront#" + rawHwnd);
                });
            }
            else
            {
                var windowTitle = parts.Length > 1 ? parts[1].Trim() : "Run";
                if (string.IsNullOrWhiteSpace(windowTitle)) windowTitle = "Run";
                if (RunWindow is null)
                {
                    UIThreadQueue.QueueAction(async () =>
                    {
                        ShowRunWindow(windowTitle);
                        await ReboundPipeClient.SendAsync("Shell::BringWindowToFront#" + RunWindow!.Handle);
                    });
                }
                else
                {
                    RunWindow.BringToFront();
                }
            }
        }
        if (parts[0] == "Shell::SpawnShutdownWindow")
        {
            if (ShutdownWindow is null)
            {
                UIThreadQueue.QueueAction(() =>
                {
                    ShowShutdownWindow();
                    return Task.CompletedTask;
                });
            }
            else
            {
                ShutdownWindow.BringToFront();
            }
        }
        if (parts[0] == "Shell::ShowStartMenu")
        {
            ToggleStartMenu();
        }

        return;
    }

    public static void ToggleStartMenu()
    {
        UIThreadQueue.QueueAction(async () =>
        {
            bool opening = !StartMenuService.IsStartMenuOpen;
            if (opening)
            {
                // Save previously focused window before opening the start menu
                _previousFocusedWindow = GetForegroundWindow();
                StartMenuService.IsStartMenuOpen = true;

                TestShellWindow?.ForceBringToFront();
                TestShellWindow?.SetAlwaysOnTop(true);

                unsafe
                {
                    SetWindowPos(
                        App.TestShellWindow!.Handle,
                        HWND.NULL,
                        0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            }
            else
            {
                // Closing start menu
                StartMenuService.IsStartMenuOpen = false;

                await Task.Delay(250); // wait before hiding

                unsafe
                {
                    SetWindowPos(
                        App.TestShellWindow!.Handle,
                        HWND.NULL,
                        0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW);
                }

                // Refocus previous window if available
                if (_previousFocusedWindow.HasValue)
                {
                    await ReboundPipeClient.SendAsync($"Shell::BringWindowToFront#{(nint)_previousFocusedWindow.Value}");
                    _previousFocusedWindow = null;
                }
            }
        });
    }

    public static void ShowRunWindow(string title = "Run")
    {
        RunWindow = new();
        RunWindow.AppWindowInitialized += (s, e) =>
        {
            RunWindow.IsPersistenceEnabled = false;
            RunWindow.MoveAndResize(
                25,
                (int)(Display.GetAvailableRectForWindow(RunWindow.Handle).bottom / Display.GetScale(RunWindow.Handle)) - 265,
                450,
                240);
            RunWindow.Title = title;
            RunWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico");
            RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
            RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            RunWindow.IsMaximizable = false;
            RunWindow.IsMinimizable = false;
            RunWindow.IsResizable = false;
            RunWindow.OnClosing += (sender, args) =>
            {
                RunWindow = null;
            };
        };
        RunWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(RunWindow));
            RunWindow.Content = frame;
            (frame.Content as RunWindow).WindowTitle.Text = title;
        };
        RunWindow.Create();
    }

    public static void ShowShutdownWindow()
    {
        ShutdownWindow = new()
        {
            IsPersistenceEnabled = false
        };
        ShutdownWindow.AppWindowInitialized += (s, e) =>
        {
            if (SettingsManager.GetValue("UseShutdownScreen", "rshutdown", false))
            {
                ShutdownWindow.Title = "Power options";
                ShutdownWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Shutdown.ico");
                ShutdownWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                ShutdownWindow.MaxWidth = 9999999;
                ShutdownWindow.MaxHeight = 9999999;
                ShutdownWindow.OnClosing += (sender, args) =>
                {
                    ShutdownWindow = null;
                };
            }
            else
            {
                ShutdownWindow.Resize(480, WindowsInformation.IsServerShutdownUIEnabled() ? 552 : 400);
                ShutdownWindow.IsPersistenceEnabled = false;
                ShutdownWindow.Title = "Power options";
                ShutdownWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\Shutdown.ico");
                ShutdownWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                ShutdownWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                ShutdownWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                ShutdownWindow.IsMaximizable = false;
                ShutdownWindow.IsMinimizable = false;
                ShutdownWindow.IsResizable = false;
                ShutdownWindow.CenterWindow();
                ShutdownWindow.OnClosing += (sender, args) =>
                {
                    ShutdownWindow = null;
                };
            }
        };
        ShutdownWindow.XamlInitialized += (s, e) =>
        {
            var frame = new Frame();
            frame.Navigate(typeof(ShutdownDialog.ShutdownDialog));
            ShutdownWindow.Content = frame;
        };
        ShutdownWindow.Create();
        if (SettingsManager.GetValue("UseShutdownScreen", "rshutdown", false))
        {
            ShutdownWindow.MakeWindowTransparent();
            ShutdownWindow.AppWindow?.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
        else
        {
            ShutdownWindow.CenterWindow();
        }
    }

    public static void CloseRunWindow()
    {
        UIThreadQueue.QueueAction(() =>
        {
            RunWindow?.Close();
            return Task.CompletedTask;
        });
    }

    public static IslandsWindow? RunWindow { get; set; }
    public static IslandsWindow? ContextMenuWindow { get; set; }
    public static IslandsWindow? DesktopWindow { get; set; }
    public static IslandsWindow? ShutdownWindow { get; set; }
    public static IslandsWindow? BackgroundWindow { get; set; }
    public static IslandsWindow? CantRunDialog { get; set; }
    public static IslandsWindow? TestShellWindow { get; set; }
}