// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Shell.ExperienceHost;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Win32;

namespace Rebound.Shell.ShutdownDialog;

[ObservableObject]
public sealed partial class ShutdownDialog : Page
{
    private ShutdownViewModel ViewModel { get; set; }

    bool isServer;

    [ObservableProperty]
    public partial BitmapImage UserPicture { get; set; }

    private static async Task<BitmapImage?> GetUserPictureAsync()
    {
        var picturePath = UserInformation.GetUserPicturePath();
        if (!string.IsNullOrEmpty(picturePath)) return new BitmapImage(new Uri(picturePath));
        else return null;
    }

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
        UIThreadQueue.QueueAction(async () =>
        {
            UserPicture = await GetUserPictureAsync();
        });
    }

    [RelayCommand]
    public void Cancel()
    {
        App.ShutdownWindow?.Close();
    }

    [RelayCommand]
    public void SignOut()
    {
        TerraFX.Interop.Windows.Windows.ExitWindowsEx(0x00000000, 0);
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
        App.ReboundPipeClient.SendAsync(isServer ? $"Shell::ShutdownServer#{ViewModel.OperationReason}{ViewModel.OperationMode}" : "Shell::Shutdown");
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
}