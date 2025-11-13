// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Forge.Cogs;
using Rebound.Forge.Launchers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rebound.Forge;

public static class Catalog
{
    public static ObservableCollection<Mod> Mods { get; } =
    [
        /*// Control Panel
        new Mod(
            name: "Control Panel",
            description: "Replacement for the legacy Control Panel. Most pages and sections redirect to the Settings app and Wintoys. Some settings for Rebound can also be found here.",
            icon: "ms-appx:///Assets/ReboundApps/ControlPanel.ico",
            installationSteps: "•   Redirect app launch\n•   Create a start menu shortcut",
            instructions: new ObservableCollection<ICog>
            {
                new IFEOCog()
                {
                    OriginalExecutableName = "control.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe"
                },
                new ShortcutCog()
                {
                    ShortcutName = "Control Panel",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe"
                },
            },
            processName: "Rebound Control Panel"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe",
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },

        // Disk Cleanup
        new Mod(
            name: "Disk Cleanup",
            description: "Replacement for the Disk Cleanup utility. Comes with more cleanup options, including Rebound temporary files and logs.",
            icon: "ms-appx:///Assets/ReboundApps/DiskCleanup.ico",
            installationSteps: "•   Redirect app launch\n•   Create a start menu shortcut",
            instructions: new ObservableCollection<ICog>
            {
                new IFEOCog()
                {
                    OriginalExecutableName = "cleanmgr.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe"
                },
                new ShortcutCog()
                {
                    ShortcutName = "Disk Cleanup",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe"
                },
            },
            processName: "Rebound Disk Cleanup"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe",
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
        
        // On-Screen Keyboard
        new Mod(
            name: "On-Screen Keyboard",

            description: "Launches the TabTip panel (temporarily). Will be replaced by a standalone app.",
            icon: "ms-appx:///Assets/ReboundApps/OSK.ico",
            installationSteps: "Redirect app launch",
            instructions: new ObservableCollection<ICog>
            {
                new IFEOCog()
                {
                    OriginalExecutableName = "osk.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
                },
                new LauncherCog()
                {
                    Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rosk.exe",
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
                },
            },
            processName: "rosk"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe",
            PreferredInstallationTemplate = InstallationTemplate.Extras
        },*/
        
        // Rebound Shell
        new Mod()
        { 
            Name = "Rebound Shell",
            Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
            Icon = "ms-appx:///Assets/ReboundApps/Shell.ico",
            InstallationSteps = "•   Register a startup task\n•   Hijack selected applets\n\nYou can choose which components are enabled from the Options menu at the top of the page.\n\nNote: Rebound Shell alone doesn't have a predefined UI. To check if it's running, try opening one of the applets it replaces.",
            Cogs = new ObservableCollection<ICog>
            {
                new ProcessKillCog()
                {
                    ProcessName = "Rebound Shell"
                },
                new PackageCog()
                {
                    PackageURI = $"{AppContext.BaseDirectory}\\Modding\\Packages\\Rebound.Shell.msixbundle",
                    PackageFamilyName = "Rebound.Shell_rcz2tbwv5qzb8"
                },
                new StartupPackageCog()
                {
                    TargetPackageFamilyName = "Rebound.Shell_rcz2tbwv5qzb8",
                    Description = "Rebound Shell task for overlapping the Windows shell.",
                    Name = "Shell",
                    RequireAdmin = false
                },
                new PackageLaunchCog()
                {
                    PackageFamilyName = "Rebound.Shell_rcz2tbwv5qzb8"
                }
            },
            Category = ModCategory.Customization,
            Settings =
            [
                new ModLabel()
                {
                    Text = "Content"
                },
                new ModBoolSetting(true)
                {
                    Name = "Run",
                    IconGlyph = "\uE946",
                    Description = "Enable Rebound Run.",
                    Identifier = "InstallRun",
                    AppName = "rshell"
                },
            ],
            Launchers =
            [
                new PackageLauncher()
                {
                    PackageFamilyName = "Rebound.Shell_rcz2tbwv5qzb8"
                }
            ],
            PreferredInstallationTemplate = InstallationTemplate.Extras
        },
        
        /*// Rebound User Account Control Settings
        new Mod(
            name: "UAC Settings",
            description: "Replacement for the useraccountcontrolsettings applet.",
            icon: "ms-appx:///Assets/ReboundApps/UACSettings.ico",
            installationSteps: "•   Redirect app launch\n•   Create a start menu shortcut",
            instructions: new ObservableCollection<ICog>
            {
                new IFEOCog()
                {
                    OriginalExecutableName = "useraccountcontrolsettings.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe"
                },
                new ShortcutCog()
                {
                    ShortcutName = "Change User Account Control Settings",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe"
                },
            },
            processName: "Rebound User Account Control Settings"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe",
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },*/
        
        // Character Map
        new Mod()
        {
            Name = "Character Map",
            Description = "Utility for searching through characters in any installed font. (3rd party)",
            Icon = "ms-appx:///Assets/AppIcons/PartnerApps/Character Map UWP.png",
            InstallationSteps = "Install Character Map UWP from the Microsoft Store",
            Cogs =
            [
                new StorePackageCog()
                {
                    StoreProductId = "9WZDNCRDXF41",
                    PackageFamilyName = "58027.265370AB8DB33_fjemmk5ta3a5g"
                },
                new IFEOCog()
                {
                    OriginalExecutableName = "charmap.exe",
                    LauncherPath = Variables.ReboundLauncherPath
                }
            ],
            Category = ModCategory.Productivity,
            Settings =
            [
                new ModInfoBar()
                {
                    IsClosable = false,
                    Title = "Settings for this app can be found inside the app itself."
                }
            ]
        },

        // Rebound About Windows
        new Mod()
        {
            Name = "About Windows",
            Description = "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
            Icon = "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
            InstallationSteps = "•   Redirect app launch\n•   Create a start menu shortcut",
            Cogs = new ObservableCollection<ICog>
            {
                new PackageCog()
                {
                    PackageURI = $"{AppContext.BaseDirectory}\\Modding\\Packages\\Rebound.About.msixbundle",
                    PackageFamilyName = "Rebound.About_rcz2tbwv5qzb8"
                },
                new IFEOCog()
                {
                    OriginalExecutableName = "winver.exe",
                    LauncherPath = Variables.ReboundLauncherPath
                }
            },
            Category = ModCategory.Customization,
            Settings =
            [
                new ModLabel()
                {
                    Text = "Layout"
                },
                new ModBoolSetting(true)
                {
                    Name = "Show Rebound version",
                    IconGlyph = "\uE946",
                    Description = "Show the installed Rebound version above the bottom bar.",
                    Identifier = "IsReboundOn",
                    AppName = "winver"
                },
                new ModInfoBar()
                {
                    IsClosable = false,
                    Title = "You will still be able to see the Rebound version inside Rebound Hub.",
                    Severity = ModInfoBarSeverity.Informational
                },
                new ModBoolSetting(true)
                {
                    Name = "Show specs",
                    IconGlyph = "\uE950",
                    Description = "Show a section with specs (CPU, GPU, RAM).",
                    Identifier = "IsSidebarOn",
                    AppName = "winver"
                },
                new ModBoolSetting(true)
                {
                    Name = "Show \"Hello User\"",
                    IconGlyph = "\uE77B",
                    Description = "Show the greeting text alongside the user's profile picture in the bottom bar.",
                    Identifier = "ShowHelloUser",
                    AppName = "winver"
                },
                new ModBoolSetting(true)
                {
                    Name = "Show activation state",
                    IconGlyph = "\uEB95",
                    Description = "Show the Windows activation state for the current installation.",
                    Identifier = "ShowActivationInfo",
                    AppName = "winver"
                },
                new ModBoolSetting(true)
                {
                    Name = "Tabs",
                    IconGlyph = "\uEC6C",
                    Description = "Display tabs below the Windows logo.",
                    Identifier = "ShowTabs",
                    AppName = "winver"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        }
    ];

    public static ObservableCollection<Mod> MandatoryMods { get; } =
    [
        new Mod()
        {
            Name = "Environment",
            Description = "Mandatory mod required for Rebound to run properly.",
            Icon = "ms-appx:///Assets/ReboundApps/Environment.ico",
            InstallationSteps = "Coming soon",
            Cogs =
            [
                new FolderCog()
                {
                    Path = Variables.ReboundDataFolder,
                    AllowPersistence = true
                },
                new FolderCog()
                {
                    Path = Variables.ReboundProgramDataFolder,
                    AllowPersistence = true
                },
                new FolderCog()
                {
                    Path = Variables.ReboundProgramDataModsFolder,
                    AllowPersistence = true
                },
                new FolderCog()
                {
                    Path = Variables.ReboundStartMenuFolder,
                    AllowPersistence = false
                },
                new TaskFolderCog()
                {
                    Name = "Rebound"
                }
            ],
            Category = ModCategory.General,
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
        // Service Host
        new Mod()
        {
            Name = "Service Host",
            Description = "Mandatory background service required for Rebound apps to run properly.",
            Icon = "ms-appx:///Assets/ReboundApps/ServiceHost.ico",
            InstallationSteps = "Register a startup task as admin",
            Cogs =
            [
                new ProcessKillCog()
                {
                    ProcessName = "Rebound Service Host"
                },
                new PackageCog()
                {
                    PackageURI = $"{AppContext.BaseDirectory}\\Modding\\Packages\\Rebound.ServiceHost.msixbundle",
                    PackageFamilyName = "Rebound.ServiceHost_rcz2tbwv5qzb8"
                },
                new StartupPackageCog()
                {
                    TargetPackageFamilyName = "Rebound.ServiceHost_rcz2tbwv5qzb8",
                    Description = "Rebound Service Host task for managing Rebound actions as admin.",
                    Name = "Service Host",
                    RequireAdmin = true
                },
                new PackageLaunchCog()
                {
                    PackageFamilyName = "Rebound.ServiceHost_rcz2tbwv5qzb8"
                }
            ],
            Category = ModCategory.General,
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
        new Mod()
        {            
            Name = "Launcher",
            Description = "Mandatory executable for Rebound apps to run properly.",
            Icon = "ms-appx:///Assets/ReboundApps/Launcher.ico",
            InstallationSteps = "Register a startup task as admin",
            Cogs =
            [
                new FileCopyCog
                {
                    Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\Rebound.Launcher.exe",
                    TargetPath = Variables.ReboundLauncherPath
                }
            ],
            Category = ModCategory.General,
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
    ];

    public static ObservableCollection<Mod> SideloadedMods => new(ModParser.ParseMods());

    public static Mod GetMod(string name)
    {
        foreach (var instruction in Mods)
        {
            if (instruction.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return instruction;
            }
        }
        throw new KeyNotFoundException($"App instructions with name '{name}' not found.");
    }

    public static Mod GetMandatoryMod(string name)
    {
        foreach (var instruction in MandatoryMods)
        {
            if (instruction.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return instruction;
            }
        }
        throw new KeyNotFoundException($"Mandatory instructions with name '{name}' not found.");
    }
}