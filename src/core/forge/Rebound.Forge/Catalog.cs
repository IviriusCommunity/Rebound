// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Forge.Cogs;
using Rebound.Forge.Launchers;
using System.Collections.ObjectModel;

namespace Rebound.Forge;

/// <summary>
/// Class containing lists of every built in Rebound mod.
/// </summary>
public partial class Catalog : ObservableObject
{
    /// <summary>
    /// Optional mods that the user can install individually.
    /// </summary>
    public static ObservableCollection<Mod> Mods { get; } = CreateMods();

    private static ObservableCollection<Mod> CreateMods()
    {
        var mods = new List<Mod>()
        {
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
            new()
            {
                Name = "Rebound Shell",
                Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
                Icon = "ms-appx:///Assets/ReboundApps/Shell.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Cogs =
                [
                    new ProcessKillCog()
                    {
                        ProcessName = "Rebound Shell"
                    },
                    new PackageCog()
                    {
                        PackageURI = Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.Shell.msixbundle"),
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
                ],
                Settings =
                [
                    new ModLabel()
                    {
                        Text = "Content"
                    },
                    new ModBoolSetting(true)
                    {
                        Name = "Run",
                        IconGlyph = "\uF0D2",
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
            new()
            {
                Name = "Character Map",
                Description = "Utility for searching through characters in any installed font. (3rd party)",
                Icon = "ms-appx:///Assets/AppIcons/PartnerApps/Character Map UWP.png",
                Category = ModCategory.Productivity,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Cogs =
                [
                    new IFEOCog()
                    {
                        OriginalExecutableName = "charmap.exe",
                        LauncherPath = Variables.ReboundLauncherPath
                    },
                    new StorePackageCog()
                    {
                        StoreProductId = "9WZDNCRDXF41",
                        PackageFamilyName = "58027.265370AB8DB33_fjemmk5ta3a5g"
                    },
                ],
                Settings =
                [
                    new ModInfoBar()
                    {
                        IsClosable = false,
                        Title = "Settings for this app can be found inside the app itself."
                    }
                ],
                Launchers =
                [
                    new PackageLauncher()
                    {
                        PackageFamilyName = "58027.265370AB8DB33_fjemmk5ta3a5g"
                    }
                ],
            },

            // Rebound About Windows
            new()
            {
                Name = "About Windows",
                Description = "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
                Icon = "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Cogs =
                [
                    new PackageCog()
                    {
                        PackageURI = Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.About.msixbundle"),
                        PackageFamilyName = "Rebound.About_rcz2tbwv5qzb8"
                    },
                    new IFEOCog()
                    {
                        OriginalExecutableName = "winver.exe",
                        LauncherPath = Variables.ReboundLauncherPath
                    }
                ],
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
                Launchers =
                [
                    new PackageLauncher()
                    {
                        PackageFamilyName = "Rebound.About_rcz2tbwv5qzb8"
                    }
                ],
            }
        };
        return new(mods.OrderBy(m => m.Name));
    }

    /// <summary>
    /// Mandatory mods that are required for Rebound to work properly. These are installed once with
    /// Rebound and cannot be uninstalled separately.
    /// </summary>
    public static ObservableCollection<Mod> MandatoryMods { get; } = CreateMandatoryMods();

    private static ObservableCollection<Mod> CreateMandatoryMods()
    {
        var mods = new List<Mod>()
        {
        // Environment
            new()
            {
                Name = "Environment",
                Description = "Mandatory mod required for Rebound to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/Environment.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Cogs =
                [
                    new FolderCog()
                    {
                        Path = Variables.ReboundDataFolder,
                        AllowPersistence = true
                    },
                    new FolderCog()
                    {
                        Path = Variables.ReboundProgramFilesFolder,
                        AllowPersistence = true
                    },
                    new FolderCog()
                    {
                        Path = Variables.ReboundProgramFilesModsFolder,
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
            },

            // Service Host
            new()
            {
                Name = "Service Host",
                Description = "Mandatory background service required for Rebound apps to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/ServiceHost.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Cogs =
                [
                    new ProcessKillCog()
                    {
                        ProcessName = "Rebound Service Host"
                    },
                    new PackageCog()
                    {
                        PackageURI = Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.ServiceHost.msixbundle"),
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
            },
            
            // Launcher
            new()
            {
                Name = "Launcher",
                Description = "Mandatory executable for Rebound apps to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/Launcher.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Cogs =
                [
                    new FileCopyCog
                    {
                        Path = Path.Combine(AppContext.BaseDirectory, "Modding", "Launchers", "Rebound.Launcher.exe"),
                        TargetPath = Variables.ReboundLauncherPath
                    }
                ],
            },
        };
        return new(mods.OrderBy(m => m.Name));
    }

    /// <summary>
    /// Custom mods that are loaded from a folder at runtime.
    /// </summary>
    public static ObservableCollection<Mod> SideloadedMods => new(ModParser.ParseMods());
}