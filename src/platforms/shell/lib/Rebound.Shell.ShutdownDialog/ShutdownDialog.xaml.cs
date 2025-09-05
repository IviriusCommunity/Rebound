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
    private readonly Action? onClosedCallback;

    private ShutdownViewModel ViewModel { get; set; }

    bool isServer;

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int RtlGetVersion(ref OSVERSIONINFOEX lpVersionInformation);

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

    public ShutdownDialog(Action? onClosed = null)
    {
        ViewModel = new();
        onClosedCallback = onClosed;
        OSVERSIONINFOEX ver = new();
        _ = RtlGetVersion(ref ver);
        //this.SetWindowSize(512, ver.wProductType == 3 ? 512 : 360);
        isServer = ver.wProductType == 3;
        InitializeComponent();
        /*this.SetWindowIcon($"{AppContext.BaseDirectory}\\Assets\\Shutdown.ico");
        this.TurnOffDoubleClick();
        this.CenterOnScreen();
        ExtendsContentIntoTitleBar = true;*/
    }

    [RelayCommand]
    public void Cancel()
    {
        //Close();
    }

    [RelayCommand]
    public void RestartToUEFI()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::RestartToUEFI");
        //Close();
    }

    [RelayCommand]
    public void RestartToRecovery()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::RestartToRecovery");
        //Close();
    }

    [RelayCommand]
    public void Shutdown()
    {
        App.ReboundPipeClient.SendMessageAsync(isServer ? $"Shell::ShutdownServer#{OperationReason.SelectedIndex}{OperationMode.SelectedIndex}" : "Shell::Shutdown");
        //Close();
    }

    [RelayCommand]
    public void Restart()
    {
        App.ReboundPipeClient.SendMessageAsync("Shell::Restart");
        //Close();
    }

    [RelayCommand]
    public void Sleep()
    {
        try
        {
            PInvoke.SetSuspendState(false, false, false);
            //Close();
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
            //Close();
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
            //Close();
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
            //Close();
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