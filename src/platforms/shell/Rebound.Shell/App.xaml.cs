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
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRT;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.SWP;
using Rebound.Shell.ExperiencePack;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

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

            TaskbarManager.HideTaskbar();

            await Task.Delay(500);

            var panels = new SystemPanelsService();
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Bottom,
                Size = 48,
                VisibilityMode = SystemPanelVisibilityMode.AlwaysVisible,
                Floating = false
            });
            panels.PanelItems.Add(new SystemPanel
            {
                Position = SystemPanelPosition.Right,
                Size = 128,
                VisibilityMode = SystemPanelVisibilityMode.Hidden,
                Floating = false
            });

            panels.Initialize();

            UIThreadQueue.QueueAction(() =>
            {
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
                    //exStyle &= ~WS.WS_EX_APPWINDOW;
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
            });

            TaskbarManager.HideTaskbar();
            /*await UIThreadQueue.QueueActionAsync(async () =>
            {
                // Create the window
                var bkgWin = new IslandsWindow()
                {
                    IsPersistenceEnabled = false,
                };

                // AppWindow init
                bkgWin.AppWindowInitialized += (s, e) =>
                {
                    bkgWin.AppWindow?.Closing += (_, _) =>
                    {
                        TaskbarManager.ShowTaskbar();
                    };
                };
                bkgWin.XamlInitialized += (_, _) =>
                {
                    bkgWin.Content = new TextBlock()
                    {
                        Text = "Close this window to bring back the taskbar"
                    };
                };

                bkgWin.Create();
            }).ConfigureAwait(false);*/
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