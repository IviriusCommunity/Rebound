// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Forge.Engines;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

internal sealed partial class HomePage : Page
{
    // The \\\\ is a workaround for this thing: https://github.com/CommunityToolkit/Labs-Windows/issues/788
    // Remove once fixed
    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string UserPicturePath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string WallpaperPath { get; set; }

    internal HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
        UIThreadQueue.QueueAction(() =>
        {
            UserPicturePath = UserInformation.GetUserPicturePath() ?? string.Empty;
            WallpaperPath = UserInformation.GetWallpaperPath() ?? string.Empty;
        });
    }

    [RelayCommand]
    public static void LaunchReboundHub()
    {
        try
        {
            ApplicationLaunchEngine.LaunchApp("Rebound.Hub_rcz2tbwv5qzb8");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("Launch Rebound Hub", "Couldn't launch Rebound Hub.", LogMessageSeverity.Error, ex);
        }
    }

    [RelayCommand]
    public static void LaunchPath(string path)
    {
        try
        {
            Process.Start(path);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog($"Launch {path}", $"Couldn't launch {path}.", LogMessageSeverity.Error, ex);
        }
    }

    private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        LaunchPath("winver.exe");
    }
}