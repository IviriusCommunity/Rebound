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
            Icon = "img:ms-appx:///Assets/Glyphs/Home.ico",
            Page = typeof(HomePage), 
            Args = ["control", ""],
            PageOpenUri = "home",
            PageOpenIconPath = "Assets/Glyphs/Home.ico",
            LegacyLaunchItems =
            [
                new() 
                {
                    Name = "Control Panel - Home", 
                    Path = "control.exe"
                }
            ],
        },
        new()
        {
            Name = "System",
            Icon = "img:ms-appx:///Assets/Glyphs/System.png",
            Children =
            [
                new() 
                {
                    Name = "Configuration", 
                    Tag = "configuration", 
                    Icon = "glyph:\uE9E9",
                    Page = typeof(SystemConfigurationPage),
                    Args = [CplArgs.SystemPropertiesComputerNameExePath],
                    PageOpenUri = "sysconfig",
                    PageOpenIconPath = "Assets/Glyphs/Configuration.ico",
                    LegacyLaunchItems =
                    [
                        new()
                        {
                            Name = "System Properties",
                            Path = CplArgs.SystemPropertiesComputerNameExePath
                        }
                    ],
                },

                new() 
                { 
                    Name = "Display",
                    Tag = "display",
                    Icon = "glyph:\uE7F4",
                    Page = typeof(DisplaySettingsPage),
                    Args = [CplArgs.DisplayColorCalibrationExePath, CplArgs.ClearTypeTunerExePath],
                    PageOpenUri = "display",
                    PageOpenIconPath = "Assets/Glyphs/Display.ico",
                    LegacyLaunchItems =
                    [
                        new()
                        {
                            Name = "Display Color Calibration",
                            Path = CplArgs.DisplayColorCalibrationExePath
                        },
                        new()
                        {
                            Name = "ClearType Text Tuner",
                            Path = CplArgs.ClearTypeTunerExePath
                        }
                    ],
                },
                new() 
                {
                    Name = "Power and Battery",
                    Icon = "glyph:\uEBA5",
                    Uri = "ms-settings:powersleep"
                },
                new() 
                { 
                    Name = "DirectX",
                    Tag = "directx",
                    Icon = "glyph:\uF211",
                    Page = typeof(DirectXPage),
                    Args = [CplArgs.DirectXControlPanelExePath, CplArgs.DirectXDiagExePath],
                    PageOpenUri = "directx",
                    PageOpenIconPath = "Assets/Glyphs/DirectX.ico",
                    LegacyLaunchItems =
                    [
                        new()
                        {
                            Name = "DirectX Properties",
                            Path = CplArgs.DirectXControlPanelExePath
                        },
                        new()
                        {
                            Name = "DirectX Diagnostic Tool",
                            Path = CplArgs.DirectXDiagExePath
                        }
                    ],
                },
                //new() { Name = "Backup and Restore", IsEnabled = false, Tag = "backupandrestore", Page = typeof(BackupAndRestorePage), Icon = "glyph:\uE896" },
                new() 
                {
                    Name = "Environment Variables",
                    Tag = "environmentvariables",
                    Icon = "path:F1 M 2 0 C 1 0 0 1 0 2 L 0 14 C 0 15 1 16 2 16 L 14 16 C 15 16 16 15 16 14 L 16 10 L 12 10 L 12 2 C 12 1 11 0 10 0 M 10 1 C 10.5 1 11 1.5 11 2 L 11 15 L 2 15 C 1.5 15 1 14.5 1 14 L 1 2 C 1 1.5 1.5 1 2 1 M 12 11 L 15 11 L 15 14 C 15 14.5 14.5 15 14 15 L 12 15 M 3 4 L 9 4 L 9 5 L 3 5 M 3 7 L 9 7 L 9 8 L 3 8 M 3 10 L 6 10 L 6 11 L 3 11",
                    Page = typeof(EnvironmentVariablesPage), 
                    Args = [CplArgs.ENVIRONMENT_VARIABLES],
                    PageOpenUri = "environmentvariables",
                    PageOpenIconPath = "Assets/Glyphs/EnvironmentVariables.ico",
                    LegacyLaunchItems =
                    [
                        new()
                        {
                            Name = "Environment Variables",
                            Path = CplArgs.EnvironmentVariablesExePath
                        }
                    ],
                },
                new() 
                { 
                    Name = "Reliability Monitor",
                    Tag = "reliabilitymonitor",
                    Icon = "glyph:\uEBE8",
                    Page = typeof(ReliabilityMonitorPage),
                    Args = [ /* TODO */ ],
                    PageOpenUri = "reliabilitymonitor",
                    PageOpenIconPath = "Assets/Glyphs/ReliabilityMonitor.ico",
                    LegacyLaunchItems =
                    [
                        // TODO
                    ], 
                },
                new() 
                {
                    Name = "Advanced",
                    Tag = "advancedsystemsettings",
                    Icon = "glyph:\uEC7A",
                    Page = typeof(BootAndBsodConfigurationPage), 
                    Args = [CplArgs.BOOT_AND_BSOD_CONFIGURATION],
                    PageOpenUri = "advancedsystemsettings",
                    PageOpenIconPath = "Assets/Glyphs/Advanced.ico",
                    LegacyLaunchItems =
                    [
                        // TODO
                    ],
                },
                new() 
                {
                    Name = "About", 
                    Tag = "about", 
                    Icon = "glyph:\uE946",
                    Page = typeof(AboutWindowsPage),
                    Args = [ /* TODO */],
                    PageOpenUri = "about",
                    PageOpenIconPath = "Assets/Glyphs/About.ico",
                    LegacyLaunchItems =
                    [
                        // TODO
                    ],
                },
            ]
        },
        new()
        {
            Name = "Bluetooth & Devices",
            Icon = "img:ms-appx:///Assets/Glyphs/BluetoothAndDevices.png",
            Uri = "ms-settings:bluetooth"
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
            Icon = "img:ms-appx:///Assets/Glyphs/Printers.ico",
            Uri = "ms-settings:printers"
        },
        new()
        {
            Name = "Customization",
            Icon = "img:ms-appx:///Assets/Glyphs/Customization.ico",
            Uri = "ms-settings:personalization"
        },
        new()
        {
            Name = "Privacy & Security",
            Icon = "img:ms-appx:///Assets/Glyphs/PrivacyAndSecurity.png",
            Children = [
                new() { Name = "Privacy and User Choice", Tag = "privacyanduserchoice", Args = [CplArgs.PRIVACY_USER_CHOICE], Icon = "glyph:\uEF58", Page = typeof(PrivacyAndUserChoicePage) },
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

    public static async Task InvokeAsync(Frame rootFrame, CplItem item)
    {
        try
        {
            if (item.Page != null)
            {
                if (rootFrame.Content?.GetType() != item.Page)
                    rootFrame.Navigate(item.Page);
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

    public Collection<CplLegacyLaunchItem> LegacyLaunchItems { get; set; } = [];
    public Collection<CplDocsItem> DocsItems { get; set; } = [];

    // Launch behaviors - only one should be set
    public Type? Page { get; set; }
    public string? Uri { get; set; }
    public string? Process { get; set; }

    public string? PageOpenUri { get; set; }
    public string? PageOpenIconPath { get; set; }

    public string[] Args { get; set; } = [];

    public bool IsEnabled { get; set; } = true;

    // Computed - NavigationViewItem.SelectsOnInvoked should only be true if it navigates within the app
    public bool SelectsOnInvoked => Page != null;

    public Collection<CplItem> Children { get; set; } = [];
}

internal partial class CplLegacyLaunchItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;
}

internal partial class CplDocsItem
{
    public string Name { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}