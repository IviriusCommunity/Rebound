// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.Threading;
using Rebound.Forge;
using Rebound.Forge.Cogs;
using Rebound.Forge.Engines;
using Rebound.Forge.Mods;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rebound.ControlPanel.Views;

internal sealed partial class HomePage : Page
{
    // The \\\\ is a workaround for this thing: https://github.com/CommunityToolkit/Labs-Windows/issues/788
    // Remove once fixed
    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string UserPicturePath { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "C:\\\\")] public partial string WallpaperPath { get; set; }

    internal HomeViewModel ViewModel { get; set; } = new();

    public HomePage()
    {
        InitializeComponent();
        Loaded += HomePage_Loaded;
    }

    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= HomePage_Loaded;

        // This needs a STA thread for some reason (Shell COM moment)
        var (wallpaper, userPicture) = await STAThread.RunOnSTAThread(() =>
        {
            string w = string.Empty, u = string.Empty;
            try { w = UserInformation.GetWallpaperPath()!; } catch { }
            try { u = UserInformation.GetUserPicturePath(); } catch { }
            return (w, u);
        }).ConfigureAwait(true);

        WallpaperPath = wallpaper;
        UserPicturePath = userPicture;

        // ViewModel init
        await ViewModel.InitializeAsync().ConfigureAwait(false);

        // Rebound Hub query
        var (hubInstalled, _) = await Catalog.ReboundHub.UpdateIntegrityAsync().ConfigureAwait(true);

        // Rebound Shell query
        var (shellInstalled, _) = await Catalog.Mods
            .FirstOrDefault(m => m.Id!.Value.ToString() == "5545cc21-f12c-4ce2-b36d-b4b0127a462b")! // Rebound Shell ID
            .UpdateIntegrityAsync().ConfigureAwait(true);

        // Wintoys query
        var (wintoysInstalled, _) = await Wintoys.UpdateIntegrityAsync().ConfigureAwait(true);

        // PowerToys query
        var machinePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PowerToys",
            "WinUI3Apps",
            "PowerToys.Settings.exe");
        var userPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PowerToys",
            "WinUI3Apps",
            "PowerToys.Settings.exe");

        // Marshal to the UI thread
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            ViewModel.IsReboundHubInstalled = hubInstalled;
            ViewModel.IsReboundShellInstalled = shellInstalled;
            ViewModel.IsWintoysInstalled = wintoysInstalled;
            ViewModel.IsPowerToysInstalled = File.Exists(userPath) || File.Exists(machinePath);
            ViewModel.HasReboundApps = hubInstalled && shellInstalled;
            ViewModel.Has3rdPartyApps = wintoysInstalled && (File.Exists(userPath) || File.Exists(machinePath));
        }).ConfigureAwait(true);
    }

    private static readonly Mod Wintoys = new()
    {
        Name = "Wintoys",
        Id = new Guid("cb55f64a-4ad2-4a62-95b4-17c33cbfe9cf"),
        Variants =
        [
            new ModVariant
            {
                Name = "Wintoys",
                Id = new Guid("7da8aa35-e57d-4929-bc05-a9c3fafb111b"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Wintoys",
                        CogId = new Guid("0c71ab68-efd4-4974-a3c9-89332159654f"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9p8ltpgcbzxd",
                            PackageFamilyName: "11413PtruceanBogdan.Wintoys_ankwhmsh70gj6")
                    }
                ]
            }
        ]
    };

    [RelayCommand]
    public static void LaunchPowerToysSettings()
    {
        try
        {
            var machinePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PowerToys",
                "WinUI3Apps",
                "PowerToys.Settings.exe");
            var userPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PowerToys",
                "WinUI3Apps",
                "PowerToys.Settings.exe");

            if (File.Exists(userPath))
            {
                Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = userPath
                });
            }
            else if (File.Exists(machinePath))
            {
                Process.Start(new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = machinePath
                });
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("Launch Wintoys", "Couldn't launch Wintoys.", LogMessageSeverity.Error, ex);
        }
    }

    [RelayCommand]
    public static void LaunchWintoys()
    {
        try
        {
            ApplicationLaunchEngine.LaunchApp("11413PtruceanBogdan.Wintoys_ankwhmsh70gj6");
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("Launch Wintoys", "Couldn't launch Wintoys.", LogMessageSeverity.Error, ex);
        }
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
    public static void LaunchLegacyControlPanel()
    {
        try
        {
            if (Application.Current is App app)
                app.LaunchLegacy(string.Empty);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("Launch Rebound Hub", "Couldn't launch Rebound Hub.", LogMessageSeverity.Error, ex);
        }
    }

    [RelayCommand]
    public static void LaunchWindowsTools()
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = "control.exe",
                Arguments = "/name Microsoft.AdministrativeTools",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog($"Launch Windows Tools", $"Couldn't launch Windows Tools.", LogMessageSeverity.Error, ex);
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

    private void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        LaunchPath("winver.exe");
    }
}