// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Forge.Cogs;
using Rebound.Forge.Launchers;
using System.Collections.ObjectModel;

namespace Rebound.Forge.Mods;

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
            
            /*// Rebound Shell
            new()
            {
                Name = "Rebound Shell",
                Id = new("5545cc21-f12c-4ce2-b36d-b4b0127a462b"),
                Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
                Icon = "ms-appx:///Assets/ReboundApps/Shell.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = new("c2e9cd0d-4ef4-48d8-a62c-b5417c6ebf9e"),
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                ProcessName = "Rebound Shell",
                                RequiresElevation = false,
                                CogId = new("743b5100-09db-4e57-94ff-d8f04b79c0fd"),
                                CogName = "Rebound Shell Process Killer",
                                RequiresUserConfirmation = false
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound Shell Package Manager",
                                CogId = new("cc531a17-4e12-441c-9320-247effe94e22"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.Shell.msixbundle"), 
                                    PackageFamilyName: "Rebound.Shell_rcz2tbwv5qzb8")
                            },
                            new StartupTaskCog()
                            {
                                CogName = "Rebound Shell Startup Task",
                                CogId = new("b43d1a69-671a-4eb6-bce4-d18b18d54342"),
                                TaskDescription = "Rebound Shell task for overlapping the Windows shell.",
                                TaskName = "Rebound Shell",
                                TaskRequiresElevation = false,
                                StartupTarget = new StartupTaskTarget("Rebound.Shell_rcz2tbwv5qzb8", StartupTaskTargetType.PackageFamilyName)
                            },
                            new ProcessLaunchCog()
                            {
                                CogName = "Rebound Shell Launcher",
                                CogId = new("15433605-3609-4658-bab0-fc33beb37812"),
                                RequiresElevation = false,
                                LaunchTarget = new ProcessLaunchTarget("Rebound.Shell_rcz2tbwv5qzb8", ProcessLaunchTargetType.PackageFamilyName)
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
            
            // Run
            new()
            {
                Name = "Run",
                Id = new("8F76268C-BAE4-4753-829A-53801C856061"),
                Description = "Replacement for the run box.",
                Icon = "ms-appx:///Assets/ReboundApps/RunBox.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = new("8F76268C-BAE4-4753-829A-53801C856062"),
                        Cogs =
                        [
                            new BoolSettingCog()
                            {
                                CogName = "Run Box Installation Setting",
                                CogId = new("15433605-3609-4658-bab0-fc33beb37812"),
                                AppliedValue = true,
                                SettingsFileName = "rebound",
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
                        Dependencies = [ new("5545cc21-f12c-4ce2-b36d-b4b0127a462b") ] // Rebound Shell
                    },
                ]
            },

            // Shutdown Dialog
            new()
            {
                Name = "Shutdown Dialog",
                Id = new("00c146cc-eae2-402a-980d-7cae86447e9f"),
                Description = "Replacement for the Alt + F4 dialog.",
                Icon = "ms-appx:///Assets/ReboundApps/ShutdownDialog.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = new("ea1abde1-f0a6-4916-9dc8-186e133e7deb"),
                        Cogs =
                        [
                            new BoolSettingCog()
                            {
                                CogName = "Shutdown Dialog Installation Setting",
                                CogId = new("8956c69f-68b2-4a18-bcbf-551ff556c305"),
                                AppliedValue = true,
                                SettingsFileName = "rebound",
                                Key = "InstallShutdown"
                            }
                        ],
                        Dependencies = [ new("5545cc21-f12c-4ce2-b36d-b4b0127a462b") ] // Rebound Shell
                        Settings = [
                            new ModLabel()
                            {
                                Text = "Layout"
                            },
                            new ModBoolSetting(true)
                            {
                                Name = "Show user",
                                IconGlyph = "\uE77B",
                                Description = "Show the username alongside the user's profile picture in the bottom bar.",
                                Identifier = "ShowUserInfo",
                                AppName = "rshutdown"
                            },
                            new ModBoolSetting(false)
                            {
                                Name = "Use the shutdown screen",
                                IconGlyph = "\uE740",
                                Description = "Show the shutdown options in fullscreen instead of a dialog window.",
                                Identifier = "UseShutdownScreen",
                                AppName = "rshutdown"
                            },
                        ]
                    },
                ]
            },

            // User Account Control Settings
            new()
            {
                Name = "UAC Settings",
                Id = new("69325954-aa50-4e7e-9e53-7c528abdc985"),
                Description = "Replacement for the useraccountcontrolsettings applet.",
                Icon = "ms-appx:///Assets/ReboundApps/UACSettings.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Recommended,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("96654df3-6bd8-4a79-8f28-abab4d81674e"),
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Rebound UAC Settings Process Killer",
                                CogId = new("7ee06bb4-234d-4ea7-8e6c-a574c2af4737"),
                                RequiresElevation = true,
                                ProcessName = "Rebound User Account Control Settings"
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound UAC Settings Package Manager",
                                CogId = new("1a0efec3-bbd4-4588-9257-f86a2df1733b"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.UserAccountControlSettings.msixbundle"),
                                    "Rebound.UserAccountControlSettings_rcz2tbwv5qzb8")
                            },
                            new IFEOCog()
                            {
                                CogName = "Rebound UAC Settings IFEOCog",
                                CogId = new("9e7c1ae3-8741-4e12-b967-4d9202fce1e2"),
                                OriginalExecutableName = "useraccountcontrolsettings.exe",
                                LauncherPath = Variables.ReboundLauncherPath
                            },
                            new LauncherAssociationCog()
                            {
                                CogName = "Rebound UAC Settings Launcher Association",
                                CogId = new("9d1dd6a8-d1c7-4b92-a97c-0ea320d84041"),
                                ConfigurationFileName = "uacsettings",
                                OriginalExecutable = "useraccountcontrolsettings.exe",
                                Targets =
                                [
                                    new LauncherTarget("default", 
                                        new LauncherTargetPackage()
                                        {
                                            FamilyName = "Rebound.UserAccountControlSettings_rcz2tbwv5qzb8"
                                        })
                                ]
                            }
                        ],
                        Launchers =
                        [
                            new PackageLauncher()
                            {
                                PackageFamilyName = "Rebound.UserAccountControlSettings_rcz2tbwv5qzb8"
                            }
                        ],
                    }
                ],
            },

            // Character Map
            new()
            {
                Name = "Character Map",
                Id = new("3b18b8e7-1a56-45f0-849b-4abccc85db3f"),
                Description = "Utility for searching through characters in any installed font. (3rd party)",
                Icon = "ms-appx:///Assets/AppIcons/PartnerApps/Character Map UWP.png",
                Category = ModCategory.Productivity,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = new("4dc88441-a519-4097-83f6-16c1b7b544dc"),
                        Cogs =
                        [
                            new IFEOCog()
                            {
                                CogName = "Rebound Character Map IFEOCog",
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
            },*/

            // Rebound About Windows
            new()
            {
                Name = "About Windows",
                Id = new("9fe3d6bd-2d64-45bc-b230-f4a605d67de7"),
                Description = "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
                Icon = "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = [
                    new()
                    {
                        Name = "Default",
                        Id = new("2917ef5b-c290-4414-ae2e-348e0c38f3c4"),
                        Cogs =
                        [
                            new PackageCog()
                            {
                                CogName = "Rebound About Package Manager",
                                CogId = new("554f0b42-8265-4b76-87c3-eb532a2dd213"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.About.msixbundle"),
                                    "Rebound.About_rcz2tbwv5qzb8")
                            },
                            new IFEOCog()
                            {
                                CogName = "Rebound About IFEOCog",
                                CogId = new("20a581d1-19dc-4c9e-9123-4d3f49dede97"),
                                OriginalExecutableName = "winver.exe",
                                LauncherPath = Variables.ReboundLauncherPath
                            },
                            new LauncherAssociationCog()
                            {
                                CogName = "Rebound About Launcher Association",
                                CogId = new("d34554fc-f202-400a-80a9-cdbdc7a6f91a"),
                                ConfigurationFileName = "winver",
                                OriginalExecutable = "winver.exe",
                                Targets =
                                [
                                    new LauncherTarget("default",
                                        new LauncherTargetPackage()
                                        {
                                            FamilyName = "Rebound.About_rcz2tbwv5qzb8"
                                        })
                                ]
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
        /*// Environment
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
                                Path = Variables.ReboundModsFolder,
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
                            new ZipExtractCog()
                            {
                                ZipFilePath = Path.Combine(AppContext.BaseDirectory, "Modding", "ServiceHost", "ServiceHost.zip"),
                                DestinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rebound", "ServiceHost")
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
            },*/
        };
        return new(mods.OrderBy(m => m.Name));
    }

    /// <summary>
    /// Represents the Rebound Hub declaration mod. Must only be used in the installers.
    /// </summary>
    /*public readonly static Mod ReboundHub = new()
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
    /// Represents the built-in mod definition for the Rebound uninstaller executable.
    /// </summary>
    /// <remarks>This mod is categorized as mandatory and is used to install the Rebound uninstaller,
    /// including its executable and a desktop shortcut. It is intended to be present in all installations to allow
    /// users to uninstall Rebound through the provided shortcut.</remarks>
    public readonly static Mod Uninstaller = new()
    {
        Name = "Rebound Uninstaller",
        Id = "Rebound.Uninstaller",
        Description = "The Rebound uninstaller executable.",
        Icon = string.Empty,
        Category = ModCategory.Mandatory,
        PreferredInstallationTemplate = InstallationTemplate.Mandatory,
        Variants =
        [
            new()
            {
                Name = "Default",
                Id = "Rebound.Uninstaller.Default",
                Cogs =
                [
                    new FileCopyCog
                    {
                        Path = Path.Combine(AppContext.BaseDirectory, "Rebound Uninstaller.exe"),
                        TargetPath = Variables.ReboundUninstallerPath
                    },
                    new ShortcutCog()
                    {
                        ExePath = Variables.ReboundUninstallerPath,
                        ShortcutName = "Uninstall Rebound"
                    }
                ],
            }
        ]
    };*/

    /// <summary>
    /// Custom mods that are loaded from a folder at runtime.
    /// </summary>
    public static ObservableCollection<Mod> SideloadedMods => new(ModParser.ParseMods());
}