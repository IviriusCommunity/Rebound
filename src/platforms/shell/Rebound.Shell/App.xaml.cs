// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Shell.Desktop;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515  // Consider making public types internal

namespace Rebound.Shell.ExperienceHost;

[Guid("E7F6D0A3-1234-4567-89AB-1C2D3E4F5678")]
public unsafe struct IReboundShellServer : Windows.Win32.System.Com.IUnknown
{
    public void** lpVtbl;

    public HRESULT OpenRunBox()
    {
        // vtable[3] → first slot after IUnknown methods
        return ((delegate* unmanaged[Stdcall]<IReboundShellServer*, HRESULT>)(lpVtbl[3]))((IReboundShellServer*)Unsafe.AsPointer(ref this));
    }
}

public class ReboundShellServerImpl : IReboundShellServer
{
    public HRESULT OpenRunBox()
    {
        Program._actions.Add(() => App.ShowRunWindow());
        return HRESULT.S_OK;
    }
}

//[ReboundApp("Rebound.ShellExperienceHost", "")]
public partial class App : Application
{
    private static bool _runWindowQueued = false;

    public App()
    {
        Run();
    }

    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void Run()
    {
        unsafe
        {
            uint cookie;
            Windows.Win32.ComPtr<IReboundShellServer> shellServer = default;

            var hr = PInvoke.CoRegisterClassObject(
                typeof(ReboundShellServerImpl).GUID,
                (Windows.Win32.System.Com.IUnknown*)(shellServer.Get()),
                (Windows.Win32.System.Com.CLSCTX)CLSCTX.CLSCTX_LOCAL_SERVER,
                (Windows.Win32.System.Com.REGCLS)(REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED),
                out cookie);

            if (hr.Failed) throw new COMException("CoRegisterClassObject failed", (int)hr);

            PInvoke.CoResumeClassObjects();
        }

        ReboundPipeClient = new ReboundPipeClient();
        await ReboundPipeClient.ConnectAsync();

        ReboundPipeClient.StartListening(async (msg) =>
        {
            switch (msg)
            {
                case "Shell::SpawnRunWindow":
                    {
                        // Only enqueue if not already queued
                        if (!_runWindowQueued)
                        {
                            _runWindowQueued = true;

                            Program._actions.Add(() =>
                            {
                                ShowRunWindow();
                                // reset flag after action executed
                                _runWindowQueued = false;
                            });
                        }
                        break;
                    }
                /*case "Shell::SpawnShutdownDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowShutdownDialog);
                    break;
                case "Shell::SpawnCantRunDialog":
                    BackgroundWindow?.DispatcherQueue.TryEnqueue(ShowCantRunDialog);
                    break;*/
                default:
                    break;
            }
        });
        /*ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
        {
            ShutdownDialog = null;
        });

        CantRunDialog = new CantRunDialog.CantRunDialog(() =>
        {
            CantRunDialog = null;
        });

        if (SettingsHelper.GetValue("AllowDesktopFeature", "rebound", false))
        {
            // Desktop window
            DesktopWindow = new DesktopWindow(ShowShutdownDialog);
            DesktopWindow.Activate();
            DesktopWindow.AttachToProgMan();
        }*/

        // Start your ReboundPipeClient
    }

    public static void ShowRunWindow()
    {
        if (RunWindow is null)
        {
            RunWindow = new();
            RunWindow.MoveAndResize(25, Display.GetAvailableRectForWindow(RunWindow.Handle).bottom - 265, 450, 240);
            RunWindow.AppWindowInitialized += (s, e) =>
            {
                RunWindow.Title = "Run";
                RunWindow.AppWindow?.SetIcon($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico");
                RunWindow.AppWindow?.TitleBar.ExtendsContentIntoTitleBar = true;
                RunWindow.AppWindow?.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                RunWindow.AppWindow?.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                RunWindow.IsMaximizable = false;
                RunWindow.IsMinimizable = false;
                RunWindow.IsResizable = false;
                RunWindow.Closing += (sender, args) =>
                {
                    RunWindow = null;
                };
            };
            RunWindow.XamlInitialized += (s, e) =>
            {
                var frame = new Frame();
                frame.Navigate(typeof(Run.RunWindow));
                RunWindow.Content = frame;
            };
            RunWindow.Create();
        }
        else
        {
            //RunWindow.ForceBringToFront();
            TerraFX.Interop.Windows.Windows.ShowWindow(RunWindow.Handle, SW.SW_SHOW);
            TerraFX.Interop.Windows.Windows.SetForegroundWindow(RunWindow.Handle);
            TerraFX.Interop.Windows.Windows.SetActiveWindow(RunWindow.Handle);
        }
    }

    public static void CloseRunWindow()
    {
        RunWindow?.Close();
        //RunWindow = null;
    }

    /*public static void ShowShutdownDialog()
    {
        if (ShutdownDialog is null)
        {
            ShutdownDialog = new ShutdownDialog.ShutdownDialog(() =>
            {
                ShutdownDialog = null;
            });
        }
        ShutdownDialog.Activate();
        ShutdownDialog.ForceBringToFront();
    }

    public static void ShowCantRunDialog()
    {
        if (CantRunDialog is null)
        {
            CantRunDialog = new CantRunDialog.CantRunDialog(() =>
            {
                CantRunDialog = null;
            });
        }
        CantRunDialog.Activate();
        CantRunDialog.ForceBringToFront();
    }*/

    private void OnSingleInstanceLaunched(object? sender, Core.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            Run();
        }
    }

    public static IslandsWindow? RunWindow { get; set; }
    public static IslandsWindow? ContextMenuWindow { get; set; }
    public static IslandsWindow? DesktopWindow { get; set; }
    public static IslandsWindow? ShutdownDialog { get; set; }
    public static IslandsWindow? BackgroundWindow { get; set; }
    public static IslandsWindow? CantRunDialog { get; set; }
}