// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace Rebound.ControlPanel;

internal static partial class CplItemPairs
{
    public static Collection<CplItem> CplItems { get; } =
    [
        new() 
        {
            Name = "Home", 
            Tag = "home",
            Icon = "img:ms-appx:///Assets/Glyphs/Home.png",
            Page = typeof(HomePage), 
            Args = ["control", ""] 
        },
        new()
        {
            Name = "System",
            Icon = "img:ms-appx:///Assets/Glyphs/System.png",
            Children =
            [
                new() { Name = "Configuration", Tag = "configuration", Icon = "glyph:\uE9E9", Page = typeof(SystemConfigurationPage), Args = [CplArgs.SystemPropertiesComputerNameExePath] },
                new() { Name = "Display", IsEnabled = false, Tag = "display", Page = typeof(DisplaySettingsPage), Icon = "glyph:\uE7F4" },
                //new() { Name = "Power and Battery", Tag = "powerandbattery", Icon = "glyph:\uEBA5" },
                new() { Name = "DirectX", Tag = "directx", Page = typeof(DirectXPage), Icon = "glyph:\uE8B2", Args = [CplArgs.DirectXControlPanelExePath] },
                new() { Name = "Backup and Restore", IsEnabled = false, Tag = "backupandrestore", Page = typeof(BackupAndRestorePage), Icon = "glyph:\uE896" },
                new() { Name = "Environment Variables", Tag = "environmentvariables", Page = typeof(EnvironmentVariablesPage), Icon = "path:F1 M 1.88 5.719999 C 1.88 5.16 2.08 4.68 2.48 4.279999 C 2.88 3.880001 3.36 3.68 3.92 3.68 L 12.36 3.68 C 12.919999 3.68 13.393332 3.880001 13.78 4.279999 C 14.166666 4.68 14.36 5.16 14.36 5.719999 L 14.36 13.679999 L 18.119999 13.679999 L 18.119999 17.119999 C 18.119999 17.893333 17.846664 18.553333 17.299999 19.099998 C 16.753332 19.646666 16.093332 19.92 15.32 19.92 L 4.68 19.92 C 3.906667 19.92 3.246667 19.646666 2.7 19.099998 C 2.153333 18.553333 1.88 17.893333 1.88 17.119999 Z M 14.36 18.68 L 15.32 18.68 C 15.746666 18.68 16.113333 18.526667 16.42 18.219999 C 16.726665 17.913332 16.879999 17.546665 16.879999 17.119999 L 16.879999 14.92 L 14.36 14.92 Z M 3.92 4.92 C 3.706666 4.92 3.52 5 3.36 5.16 C 3.2 5.32 3.12 5.506666 3.12 5.719999 L 3.12 17.119999 C 3.12 17.546665 3.273333 17.913332 3.58 18.219999 C 3.886667 18.526667 4.253333 18.68 4.68 18.68 L 13.12 18.68 L 13.12 5.719999 C 13.119999 5.506666 13.046666 5.32 12.9 5.16 C 12.753332 5 12.573332 4.92 12.36 4.92 Z M 5.64 7.44 C 5.453333 7.44 5.299999 7.5 5.18 7.62 C 5.059999 7.74 5 7.886666 5 8.059999 C 5 8.233334 5.059999 8.38 5.18 8.5 C 5.299999 8.62 5.453333 8.68 5.64 8.679999 L 10.639999 8.679999 C 10.799999 8.68 10.94 8.62 11.059999 8.5 C 11.179999 8.38 11.24 8.233334 11.24 8.059999 C 11.24 7.886666 11.179999 7.74 11.059999 7.62 C 10.94 7.5 10.799999 7.44 10.639999 7.44 Z M 5 11.799999 C 5 11.639999 5.059999 11.493334 5.18 11.36 C 5.299999 11.226667 5.453333 11.16 5.64 11.16 L 10.639999 11.16 C 10.799999 11.16 10.94 11.226667 11.059999 11.36 C 11.179999 11.493334 11.24 11.639999 11.24 11.799999 C 11.24 11.959999 11.179999 12.106667 11.059999 12.24 C 10.94 12.373333 10.799999 12.440001 10.639999 12.44 L 5.64 12.44 C 5.453333 12.440001 5.299999 12.373333 5.18 12.24 C 5.059999 12.106667 5 11.959999 5 11.799999 Z M 5.64 14.92 C 5.453333 14.92 5.299999 14.98 5.18 15.099999 C 5.059999 15.219999 5 15.366667 5 15.539999 C 5 15.713333 5.059999 15.86 5.18 15.98 C 5.299999 16.099998 5.453333 16.16 5.64 16.16 L 8.12 16.16 C 8.306666 16.16 8.459999 16.099998 8.58 15.98 C 8.7 15.86 8.76 15.713333 8.76 15.539999 C 8.76 15.366667 8.7 15.219999 8.58 15.099999 C 8.459999 14.98 8.306666 14.92 8.12 14.92 Z "/*"glyph:\uE90F"*/, Args = [CplArgs.ENVIRONMENT_VARIABLES] },
                new() { Name = "Reliability Monitor", Tag = "reliabilitymonitor", Page = typeof(ReliabilityMonitorPage), Icon = "glyph:\uEBE8" },
                new() { Name = "Advanced", Tag = "bootandbsodconfiguration", Page = typeof(BootAndBsodConfigurationPage), Icon = "glyph:\uEC7A", Args = [CplArgs.BOOT_AND_BSOD_CONFIGURATION] },
                new() { Name = "About", Tag = "about", Icon = "glyph:\uE946", Page = typeof(AboutWindowsPage) },
            ]
        },
        new()
        {
            Name = "Bluetooth & Devices",
            Icon = "img:ms-appx:///Assets/Glyphs/BluetoothAndDevices.png"
        },
        new()
        {
            Name = "Network & Internet",
            Icon = "img:ms-appx:///Assets/Glyphs/Internet.ico",
            Uri = "ms-settings:network"
        },
        new()
        {
            Name = "Printers",
            Icon = "img:ms-appx:///Assets/Glyphs/Printers.ico"
        },
        new()
        {
            Name = "Customization",
            Tag = "appearance",
            Icon = "img:ms-appx:///Assets/Glyphs/Customization.ico"
        },
        new()
        {
            Name = "Privacy & Security",
            Icon = "img:ms-appx:///Assets/Glyphs/PrivacyAndSecurity.png",
            Children = [
                new() { Name = "Privacy and User Choice", Tag = "privacyanduserchoice", Icon = "glyph:\uEF58", Page = typeof(PrivacyAndUserChoicePage) },
                new() { Name = "Credentials Manager", Tag = "credentialsmanager", Icon = "glyph:\uF540", Page = typeof(CredentialManagerPage) },
                ]
        },
        new()
        {
            Name = "Apps & Programs",
            Icon = "img:ms-appx:///Assets/Glyphs/AppsAndPrograms.png"
        },
        new()
        {
            Name = "User Accounts",
            Icon = "img:ms-appx:///Assets/Glyphs/UserAccounts.png"
        },
        new()
        {
            Name = "Time & Language",
            Icon = "img:ms-appx:///Assets/Glyphs/TimeAndLanguage.png"
        },
        new()
        {
            Name = "Gaming",
            Icon = "img:ms-appx:///Assets/Glyphs/Gaming.png"
        },
        new()
        {
            Name = "Enterprise Administration",
            Icon = "img:ms-appx:///Assets/Glyphs/EnterpriseAdministration.png"
        },
        new()
        {
            Name = "Windows Update",
            Icon = "img:ms-appx:///Assets/Glyphs/WindowsUpdate.png"
        },
        new() { Name = "Windows Tools", Icon = "img:ms-appx:///Assets/Glyphs/WindowsTools.png", Tag = "windowstools", Page = typeof(WindowsToolsPage),
            Args = [CplArgs.appWizCplPath, CplArgs.ADMINISTRATIVE_TOOLS_UTIL] },
    ];

    // Searches top level only - for items that have pages and can be selected
    public static CplItem? GetFromTag(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
            return null;
        return SearchByTag(CplItems, tag);
    }

    public static CplItem? GetFromPage(Type? pageType)
    {
        if (pageType == null)
            return null;
        return SearchByPage(CplItems, pageType);
    }

    private static CplItem? SearchByTag(IEnumerable<CplItem> items, string tag)
    {
        foreach (var item in items)
        {
            if (item.Tag == tag)
                return item;
            var result = SearchByTag(item.Children, tag);
            if (result != null)
                return result;
        }
        return null;
    }

    private static CplItem? SearchByPage(IEnumerable<CplItem> items, Type pageType)
    {
        foreach (var item in items)
        {
            if (item.Page == pageType)
                return item;
            var result = SearchByPage(item.Children, pageType);
            if (result != null)
                return result;
        }
        return null;
    }

    public static async Task InvokeAsync(CplItem item)
    {
        try
        {
            if (item.Page != null)
            {
                var frame = (App.MainWindow as MainWindow)?.RootFrame.Content as RootPage;
                if (frame?.RootFrame?.Content?.GetType() != item.Page)
                    frame?.RootFrame?.Navigate(item.Page);
            }
            else if (!string.IsNullOrEmpty(item.Uri))
            {
                await Launcher.LaunchUriAsync(new Uri(item.Uri));
            }
            else if (!string.IsNullOrEmpty(item.Process))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.Process,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }
}

internal partial class CplItem
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string? Icon { get; set; }

    // Launch behaviors - only one should be set
    public Type? Page { get; set; }
    public string? Uri { get; set; }
    public string? Process { get; set; }

    public string[] Args { get; set; } = [];

    public bool IsEnabled { get; set; } = true;

    // Computed - NavigationViewItem.SelectsOnInvoked should only be true if it navigates within the app
    public bool SelectsOnInvoked => Page != null;

    public Collection<CplItem> Children { get; set; } = [];
}