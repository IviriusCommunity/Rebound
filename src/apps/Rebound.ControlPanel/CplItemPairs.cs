// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.ControlPanel.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel;

internal static partial class CplItemPairs
{
    public static Collection<CplItem> CplItems { get; } =
    [
        new() 
        {
            Name = "Home", 
            Tag = "home",
            Icon = "glyph:\uE80F", 
            Page = typeof(HomePage), 
            Args = ["control", ""] 
        },
        new()
        {
            Name = "System",
            Icon = "img:ms-appx:///Assets/Glyphs/System.ico",
            Children =
            [
                new() { Name = "Configuration", Tag = "configuration", Icon = "glyph:\uE90F", Page = typeof(SystemConfigurationPage), Args = [CplArgs.SystemPropertiesComputerNameExePath] },
                new() { Name = "Display", Tag = "display", Page = typeof(DisplaySettingsPage), Icon = "glyph:\uE7F4" },
                //new() { Name = "Power and Battery", Tag = "powerandbattery", Icon = "glyph:\uEBA5" },
                new() { Name = "DirectX", Tag = "directx", Page = typeof(DirectXPage), Icon = "glyph:\uE967", Args = [CplArgs.DirectXControlPanelExePath] },
                new() { Name = "Backup and Restore", Tag = "backupandrestore", Page = typeof(BackupAndRestorePage), Icon = "glyph:\uE896" },
                new() { Name = "Environment Variables", Tag = "environmentvariables", Icon = "glyph:\uE83B" },
                new() { Name = "Reliability Monitor", Tag = "reliabilitymonitor", Icon = "glyph:\uEBE8" },
                new() { Name = "Boot and BSoD Configuration", Tag = "bootandbsodconfiguration", Icon = "glyph:\uEBC8" },
                new() { Name = "About", Tag = "about", Icon = "glyph:\uE946" },
            ]
        },
        new()
        {
            Name = "Network and Internet",
            Icon = "img:ms-appx:///Assets/Glyphs/Internet.ico",
            Uri = "ms-settings:network"
        },
        new()
        {
            Name = "Hardware and Sound",
            Icon = "img:ms-appx:///Assets/HardwareAndSound.ico",
            Children =
            [
                new() { Name = "Bluetooth and Devices", Icon = "glyph:\uEB66", Uri = "ms-settings:devices" ,
                    Children = [
                        new() { Name = "Add hardware manually", Icon = "glyph:\uE710" },
                        ]
                },
                new() { Name = "Sound", Icon = "glyph:\uE767" },
            ]
        },
        new()
        {
            Name = "Privacy and Security",
            Icon = "img:ms-appx:///Assets/Glyphs/PrivacySecurity.ico",
            Children = [
                new() { Name = "Privacy and User Choice", Tag = "privacyanduserchoice", Icon = "glyph:\uEF58" },
                new() { Name = "User Account Control Settings", Tag = "useraccountcontrolsettings", Icon = "glyph:\uEA18" },
                new() { Name = "Credentials Manager", Tag = "credentialsmanager", Icon = "glyph:\uF540" },
                ]
        },
        new()
        {
            Name = "Appearance and Personalization",
            Tag = "appearance",
            Icon = "img:ms-appx:///Assets/Glyphs/Personalization.ico",
            Children = [
                new() { Name = "General", Icon = "glyph:\uE713" },
                new() { Name = "Rebound Shell", Icon = "img:ms-appx:///Assets/Apps/ReboundShell.ico" },
                ]
        },
        new()
        {
            Name = "Enterprise Administration",
            Icon = "img:ms-appx:///Assets/Glyphs/EnterpriseAdministration.ico",
            Children =
            [
                new() { Name = "Utilities", Icon = "glyph:\uE90F" },
                new() { Name = "Network Drives", Icon = "glyph:\uE8CE" },
            ]
        },
        new()
        {
            Name = "Clock and Region",
            Icon = "img:ms-appx:///Assets/ClockAndRegion.ico"
        },
        new() { Name = "Windows Tools", Icon = "img:ms-appx:///Assets/Glyphs/WindowsTools.ico", Tag = "windowstools", Page = typeof(WindowsToolsPage),
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
                var frame = (App.MainWindow?.Content as Frame)?.Content as RootPage;
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