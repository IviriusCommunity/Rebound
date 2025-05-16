// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Shell.ShutdownDialog;

public sealed partial class ShutdownDialog : WindowEx
{
    private readonly Action? onClosedCallback;

    private WindowManager? windowManager;

    private const int WM_ACTIVATE = 0x0006;
    private const int WA_INACTIVE = 0;

    public ShutdownDialog(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }

    [RelayCommand]
    public void RestartToUEFI()
    {
        // Requires elevated privileges
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "/r /fw /t 0",
            UseShellExecute = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(psi);
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void RestartToRecovery()
    {
        // Requires elevated privileges
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "/r /o /t 0",
            UseShellExecute = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(psi);
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void Shutdown()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "/s /t 0",
            UseShellExecute = true,
            CreateNoWindow = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(psi);
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void Restart()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "/r /t 0",
            UseShellExecute = true,
            CreateNoWindow = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(psi);
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void Sleep()
    {
        try
        {
            PInvoke.SetSuspendState(false, false, false);
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void SwitchUsers()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "tsdiscon.exe",
                UseShellExecute = true
            });
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void Lock()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "user32.dll,LockWorkStation",
                UseShellExecute = true
            });
            Close();
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void Hibernate()
    {
        try
        {
            PInvoke.SetSuspendState(true, false, false);
            Close();
        }
        catch
        {

        }
    }

    private void Manager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == WM_ACTIVATE && e.Message.WParam == WA_INACTIVE && !Debugger.IsAttached) 
            Close();
    }

    private unsafe void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        windowManager = null;
        onClosedCallback?.Invoke();
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (!Debugger.IsAttached)
        {
            this.CenterOnScreen();
            if (windowManager == null)
            {
                windowManager = WindowManager.Get(this);
                windowManager.WindowMessageReceived += Manager_WindowMessageReceived;
            }
        }
    }
}