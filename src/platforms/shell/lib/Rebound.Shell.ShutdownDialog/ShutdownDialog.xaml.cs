// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Rebound.Shell.ExperienceHost;
using Windows.UI.Xaml.Controls;
using Windows.Win32;

namespace Rebound.Shell.ShutdownDialog;

public sealed partial class ShutdownDialog : Page
{
    private ShutdownViewModel ViewModel { get; set; }

    bool isServer;

    [DllImport("ntdll.dll")]
    private static extern unsafe int RtlGetVersion(OSVERSIONINFOEX* lpVersionInformation);

    [StructLayout(LayoutKind.Sequential)]
    public struct OSVERSIONINFOEX
    {
        public uint dwOSVersionInfoSize;
        public uint dwMajorVersion;
        public uint dwMinorVersion;
        public uint dwBuildNumber;
        public uint dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public ushort wServicePackMajor;
        public ushort wServicePackMinor;
        public ushort wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    public ShutdownDialog()
    {
        ViewModel = new();
        OSVERSIONINFOEX ver = new();
        unsafe
        {
            _ = RtlGetVersion(&ver);
        }
        isServer = ver.wProductType == 3;
        InitializeComponent();
    }

    [RelayCommand]
    public void Cancel()
    {
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void RestartToUEFI()
    {
        App.ReboundPipeClient.SendAsync("Shell::RestartToUEFI");
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void RestartToRecovery()
    {
        App.ReboundPipeClient.SendAsync("Shell::RestartToRecovery");
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void Shutdown()
    {
        App.ReboundPipeClient.SendAsync(isServer ? $"Shell::ShutdownServer#{OperationReason.SelectedIndex}{OperationMode.SelectedIndex}" : "Shell::Shutdown");
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void Restart()
    {
        App.ReboundPipeClient.SendAsync("Shell::Restart");
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void Sleep()
    {
        try
        {
            PInvoke.SetSuspendState(false, false, false);
            App.ShutdownWindow?.Close();
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
            App.ShutdownWindow?.Close();
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
            App.ShutdownWindow?.Close();
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
            App.ShutdownWindow?.Close();
        }
        catch
        {

        }
    }

    private void MenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "control.exe",
            ArgumentList = { "/name Rebound.Settings" },
        });
    }
}