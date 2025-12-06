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
                Id = "Rebound.Shell",
                Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
                Icon = "ms-appx:///Assets/ReboundApps/Shell.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.Shell.Default",
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
                        Launchers =
                        [
                            new PackageLauncher()
                            {
                                PackageFamilyName = "Rebound.Shell_rcz2tbwv5qzb8"
                            }
                        ],
                    }
                ]
            },
            
            // Rebound Shell
            new()
            {
                Name = "Run",
                Id = "Rebound.Run",
                Description = "Replacement for the run box.",
                Icon = "ms-appx:///Assets/ReboundApps/RunBox.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.Run.Default",
                        Cogs =
                        [
                            new BoolSettingCog()
                            {
                                AppliedValue = true,
                                AppName = "rebound",
                                Key = "InstallRun"
                            }
                        ],
                        Settings =
                        [
                            new ModLabel()
                            {
                                Text = "Behavior"
                            },
                            new ModBoolSetting()
                            {
                                AppName = "rshell",
                                Description = "Launch PowerToys Command Palette when opening the Run box.",
                                IconGlyph = "\uE945",
                                Identifier = "RunBoxUseCommandPalette",
                                Name = "Use PowerToys Command Palette"
                            },
                            new ModInfoBar()
                            {
                                IsClosable = false,
                                Title = "PowerToys must be installed for this setting to work.",
                                Severity = ModInfoBarSeverity.Warning
                             }
                        ],
                        Dependencies = [ "Rebound.Shell" ]
                    },
                ]
            },

            // Rebound Shell
            new()
            {
                Name = "Shutdown Dialog",
                Id = "Rebound.ShutdownDialog",
                Description = "Replacement for the Alt + F4 dialog.",
                Icon = "ms-appx:///Assets/ReboundApps/ShutdownDialog.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.ShutdownDialog.Default",
                        Cogs =
                        [
                            new BoolSettingCog()
                            {
                                AppliedValue = true,
                                AppName = "rebound",
                                Key = "InstallShutdown"
                            }
                        ],
                        Dependencies = [ "Rebound.Shell" ]
                    },
                ]
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
                Id = "Rebound.CharacterMap",
                Description = "Utility for searching through characters in any installed font. (3rd party)",
                Icon = "ms-appx:///Assets/AppIcons/PartnerApps/Character Map UWP.png",
                Category = ModCategory.Productivity,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.CharacterMap.Default",
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
                    }
                ]
            },

            // Rebound About Windows
            new()
            {
                Name = "About Windows",
                Id = "Rebound.About",
                Description = "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
                Icon = "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.About.Default",
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
                ]
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
                Id = "Rebound.Environment",
                Description = "Mandatory mod required for Rebound to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/Environment.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants = 
                [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.Environment.Default",
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
                    }
                ]
            },

            // Service Host
            new()
            {
                Name = "Service Host",
                Id = "Rebound.ServiceHost",
                Description = "Mandatory background service required for Rebound apps to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/ServiceHost.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants = 
                [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.ServiceHost.Default",
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                ProcessName = "Rebound Service Host"
                            },
                            new ProgramFolderCopyCog()
                            {
                                Path = Path.Combine(AppContext.BaseDirectory, "Modding", "ServiceHost"),
                                DestinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound", "ServiceHost")
                            },
                            new StartupTaskCog()
                            {
                                TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound", "ServiceHost", "Rebound Service Host.exe"),
                                Description = "Rebound Service Host task for managing Rebound actions as admin.",
                                Name = "Service Host",
                                RequireAdmin = true
                            },
                            new ProcessLaunchCog()
                            {
                                ExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound", "ServiceHost", "Rebound Service Host.exe")
                            }
                        ],
                    }
                ]
            },
            
            // Launcher
            new()
            {
                Name = "Launcher",
                Id = "Rebound.Launcher",
                Description = "Mandatory executable for Rebound apps to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/Launcher.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = "Rebound.Launcher.Default",
                        Cogs =
                        [
                            new FileCopyCog
                            {
                                Path = Path.Combine(AppContext.BaseDirectory, "Modding", "Launchers", "Rebound.Launcher.exe"),
                                TargetPath = Variables.ReboundLauncherPath
                            }
                        ],
                    }
                ]
            },
        };
        return new(mods.OrderBy(m => m.Name));
    }

    /// <summary>
    /// Represents the Rebound Hub declaration mod. Must only be used in the installers.
    /// </summary>
    public readonly static Mod ReboundHub = new()
    {
        Name = "Rebound Hub",
        Id = "Rebound.Hub",
        Description = "The Rebound Hub.",
        Icon = string.Empty,
        Category = ModCategory.Mandatory,
        PreferredInstallationTemplate = InstallationTemplate.Mandatory,
        Variants =
        [
            new()
            {
                Name = "Default",
                Id = "Rebound.Hub.Default",
                Cogs =
                [
                    new ProcessKillCog()
                    {
                        ProcessName = "Rebound Hub"
                    },
                    new PackageCog()
                    {
                        PackageURI = Path.Combine(AppContext.BaseDirectory, "Rebound.Hub.msixbundle"),
                        PackageFamilyName = "Rebound.Hub_rcz2tbwv5qzb8"
                    },
                ],
            }
        ]
    };

    /// <summary>
    /// Custom mods that are loaded from a folder at runtime.
    /// </summary>
    public static ObservableCollection<Mod> SideloadedMods => new(ModParser.ParseMods());
}