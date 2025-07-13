// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Rebound.Helpers.Windowing;
using Rebound.Shell.ExperienceHost;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Shell.ShutdownDialog;

public sealed partial class ShutdownDialog : WindowEx
{
    private readonly Action? onClosedCallback;

    private WindowManager? windowManager;

    public ShutdownDialog(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        this.TurnOffDoubleClick();
        this.CenterOnScreen();
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
        App.ReboundPipeClient.SendMessageAsync("Shell::RestartToUEFI");
        Close();
    }

    [RelayCommand]
    public void RestartToRecovery()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::RestartToRecovery");
        Close();
    }

    [RelayCommand]
    public void Shutdown()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::Shutdown");
        Close();
    }

    [RelayCommand]
    public void Restart()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::Restart");
        Close();
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

    private unsafe void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        windowManager = null;
        onClosedCallback?.Invoke();
    }

    private void MenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "control.exe",
            ArgumentList = { "/name Rebound.Settings" },
        });
    }
}