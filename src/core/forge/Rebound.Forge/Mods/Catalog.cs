// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Forge.Cogs;
using Rebound.Forge.Launchers;
using System.Collections.ObjectModel;

#pragma warning disable CA1724

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
            // Control Panel
            new()
            { 
                Name = "Control Panel",
                Id = new Guid("d889389f-ec76-4ca6-93c8-318abe1e853c"),
                Description = "Replacement for the legacy Control Panel. Some pages and sections redirect to the Settings app. Additional privacy and user choice options can be found here.",
                Icon = "ms-appx:///Assets/ReboundApps/ControlPanel.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = 
                [
                    new ModVariant()
                    {
                        Name = "Default",
                        Id = new("32cae467-aeb4-4700-84d7-53265c0c942e"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59")], // Launcher
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Kill Rebound Control Panel",
                                CogId = new("4fa8b125-f076-4285-84e1-09ae11d13a07"),
                                ProcessName = "Rebound.ControlPanel",
                                KillOn = ProcessKillOn.Both,
                                RequiresElevation = true
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound Control Panel Package Manager",
                                CogId = new("be5586c6-4ac0-4567-9dfe-43df94fb706b"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.ControlPanel.msixbundle"),
                                    PackageFamilyName: "Rebound.ControlPanel_rcz2tbwv5qzb8")
                            },
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound Control Panel Program Association",
                                CogId = new("3c1b8eaa-a4e4-4fbc-b993-dd0544814909"),
                                OriginalExecutable = "control.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.AUMID,
                                    "Rebound.ControlPanel_rcz2tbwv5qzb8!App")
                            }
                        ]
                    }
                ]
            },

            // Rebound Shell
            new()
            {
                Name = "Rebound Shell",
                Id = new("5545cc21-f12c-4ce2-b36d-b4b0127a462b"),
                Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
                Icon = "ms-appx:///Assets/ReboundApps/Shell.ico",
                Category = ModCategory.Customization,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = 
                [
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
                                CogId = new("4f6e025e-e237-4f36-85b4-2eeac9a06bdd"),
                                StartupTarget = new StartupTaskTarget(
                                    "Rebound.Shell_rcz2tbwv5qzb8",
                                    StartupTaskTargetType.PackageFamilyName),
                                TaskRequiresElevation = true,
                                TaskDescription = "Rebound Shell application.",
                                TaskName = "Rebound Shell"
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
                Variants = 
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("8F76268C-BAE4-4753-829A-53801C856062"),
                        Cogs =
                        [
                            new DLLInjectionCog()
                            {
                                CogName = "RunFileDlg Redirection DLL",
                                CogId = new("199e9acb-fda0-4c19-bb08-c796053ba2c3"),
                                DLLPathARM64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.Run.ARM64.dll"),
                                DLLPathx64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.Run.x64.dll"),
                                TargetProcesses = [ "*" ],
                                ExcludedProcesses = [ "Rebound.Shell.exe" ]
                            }
                        ],
                        Dependencies = [ new("5545cc21-f12c-4ce2-b36d-b4b0127a462b") ] // Rebound Shell
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
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound UAC Settings Program Association",
                                CogId = new("e239881d-edf8-4910-abaa-361fd6af6ccb"),
                                OriginalExecutable = "useraccountcontrolsettings.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.AUMID,
                                    "Rebound.ControlPanel_rcz2tbwv5qzb8!App")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:useraccountcontrolsettings")
                            }
                        ],
                    }
                ],
            },
            
            // Advanced System Properties
            new()
            {
                Name = "Advanced System Properties",
                Id = new("a71e8c32-5f94-4b61-8d27-3c9e5a12f806"),
                Description = "Replacement for the SystemPropertiesAdvanced.exe applet.",
                Icon = "ms-appx:///Assets/ReboundApps/SystemConfig.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("c82f1a49-7e63-4d20-9b51-6a3f8e27d904"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound Advanced System Properties Program Association",
                                CogId = new("f31b7d85-2c49-4e16-a093-8b6f5d21c740"),
                                OriginalExecutable = "SystemPropertiesAdvanced.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                    }
                ],
            },
            
            // System Properties - Computer Name
            new()
            {
                Name = "System Properties - Computer Name",
                Id = new("d49f2b81-6c35-4e97-a120-8f53d7b26409"),
                Description = "Replacement for the SystemPropertiesComputerName.exe applet.",
                Icon = "ms-appx:///Assets/ReboundApps/SystemConfig.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("e72a4c93-1b58-46d0-9f34-5c81a7e263b5"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound System Properties - Computer Name Program Association",
                                CogId = new("b63e1d75-9a42-4f08-8c31-2d7e5a96f140"),
                                OriginalExecutable = "SystemPropertiesComputerName.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:sysconfig")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:sysconfig")
                            }
                        ],
                    }
                ],
            },
            
            // System Properties - Hardware
            new()
            {
                Name = "System Properties - Hardware",
                Id = new("8d3f1a72-5c94-4e60-b281-7f6a9d35c042"),
                Description = "Replacement for the SystemPropertiesHardware.exe applet.",
                Icon = "ms-appx:///Assets/ReboundApps/SystemConfig.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("2a6e9c41-8f53-4d70-a192-5b3e7c64d805"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound System Properties - Hardware Program Association",
                                CogId = new("c5b8e2f1-7d49-4a63-9f20-6e3d1b85a704"),
                                OriginalExecutable = "SystemPropertiesHardware.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:sysconfig")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:sysconfig")
                            }
                        ],
                    }
                ],
            },

            // System Properties - Performance
            new()
            {
                Name = "System Properties - Performance",
                Id = new("f4a7c291-3e65-48b0-9d12-6c8e5f31a704"),
                Description = "Replacement for the SystemPropertiesPerformance.exe applet.",
                Icon = "ms-appx:///Assets/ReboundApps/SystemConfig.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("7b2d5e83-1a49-46c0-8f35-9e6a4c21d708"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound System Properties - Performance Program Association",
                                CogId = new("e9c3b6a1-5d72-4f08-a294-7c1e8b53d640"),
                                OriginalExecutable = "SystemPropertiesPerformance.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                    }
                ],
            },

            // System Configuration
            new()
            {
                Name = "System Configuration",
                Id = new("3e8a1c72-5d94-4b60-a281-7f6c9d35e042"),
                Description = "Replacement for the msconfig.exe application.",
                Icon = "ms-appx:///Assets/ReboundApps/SystemConfig.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("6b2e9c41-8f53-4d70-a192-5b3e7c64d805"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")],
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound System Configuration Program Association",
                                CogId = new("c5b8e2f1-7d49-4a63-9f20-6e3d1b85a704"),
                                OriginalExecutable = "msconfig.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:advancedsystemsettings")
                            }
                        ],
                    }
                ],
            },

            // ClearType Text Tuner
            new()
            {
                Name = "ClearType Text Tuner",
                Id = new("f4a7c291-3e65-48b0-9d12-6c8e5f31a704"),
                Description = "Replacement for the cttune.exe application.",
                Icon = "ms-appx:///Assets/ReboundApps/ClearType.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("7b2d5e83-1a49-46c0-8f35-9e6a4c21d708"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")],
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound ClearType Text Tuner Program Association",
                                CogId = new("e9c3b6a1-5d72-4f08-a294-7c1e8b53d640"),
                                OriginalExecutable = "cttune.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:display")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:display")
                            }
                        ],
                    }
                ],
            },

            // Display Color Calibration
            new()
            {
                Name = "Display Color Calibration",
                Id = new("9c2f6a81-4d35-47e0-b192-8f5a3c71d604"),
                Description = "Replacement for the dccw.exe application.",
                Icon = "ms-appx:///Assets/ReboundApps/DisplayColorCalibration.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("1d7b4e92-6a35-48c0-9f21-5e8d3a72b604"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")],
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound Display Color Calibration Program Association",
                                CogId = new("a6e3c9f2-8b51-4d70-9c24-5f1a7e83b602"),
                                OriginalExecutable = "dccw.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:display")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:display")
                            }
                        ],
                    }
                ],
            },

            // DirectX Control Panel
            new()
            {
                Name = "DirectX Control Panel",
                Id = new("5a8d2c71-9e43-46b0-a195-7f3c6d82e504"),
                Description = "Replacement for the dxcpl.exe application.",
                Icon = "ms-appx:///Assets/ReboundApps/DirectX.ico",
                Category = ModCategory.SystemAdministration,
                PreferredInstallationTemplate = InstallationTemplate.Complete,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("8e1c5b73-2d49-4a60-b837-6f9e1c25d402"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")],
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound DirectX Control Panel Program Association",
                                CogId = new("d2f6a9c1-5b83-4e70-a294-8c1d6f35b702"),
                                OriginalExecutable = "dxcpl.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:directx")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:directx")
                            }
                        ],
                    }
                ],
            },

            // Launcher
            new()
            {
                Name = "Launcher",
                Id = new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"),
                Description = "A small utility that allows Rebound to open one program instead of the other.",
                Icon = "ms-appx:///Assets/ReboundApps/Launcher.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = 
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("2f38d061-3a05-460c-a3a1-57339c45ee7c"),
                        Cogs =
                        [
                            new DLLInjectionCog()
                            {
                                CogName = "Redirection DLL",
                                CogId = new("b1781ee2-0823-454c-bccc-f24baa11523e"),
                                DLLPathARM64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.Launcher.ARM64.dll"),
                                DLLPathx64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.Launcher.x64.dll"),
                                TargetProcesses = [ "*" ],
                                ExcludedProcesses = [ ]
                            }
                        ],
                        Settings =
                        [
                            new ModInfoBar()
                            {
                                IsClosable = false,
                                Title = "There are no settings available for this mod."
                            }
                        ]
                    }
                ]
            },

            // Character Map
            new()
            {
                Name = "Character Map",
                Id = new("3b18b8e7-1a56-45f0-849b-4abccc85db3f"),
                Description = "Utility for searching through characters in any installed font. (3rd party)",
                Icon = "ms-appx:///Assets/PartnerApps/CharacterMapUWP.png",
                Category = ModCategory.Productivity,
                PreferredInstallationTemplate = InstallationTemplate.Extras,
                Variants = 
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("4dc88441-a519-4097-83f6-16c1b7b544dc"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59")], // Launcher
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Kill Character Map UWP",
                                CogId = new("6b4d5759-af00-48a8-a877-e3b3c5725328"),
                                ProcessName = "CharacterMap",
                                KillOn = ProcessKillOn.Both,
                                RequiresElevation = false
                            },
                            new PackageCog()
                            {
                                CogName = "Character Map UWP Package Manager",
                                CogId = new("df390a26-a0cd-4350-b046-2f37666800e3"),
                                Target = new PackageTarget(
                                    PackageTargetType.Store,
                                    StoreProductId: "9WZDNCRDXF41",
                                    PackageFamilyName: "58027.265370AB8DB33_fjemmk5ta3a5g")
                            },
                            new ProgramAssociationCog()
                            {
                                CogName = "Character Map UWP Program Association",
                                CogId = new("a1d0b615-6400-4234-b14a-a803af9a9366"),
                                OriginalExecutable = "charmap.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.AUMID,
                                    "58027.265370AB8DB33_fjemmk5ta3a5g!App")
                            }
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
            
            // Disk Cleanup
            new()
            {
                Name = "Disk Cleanup",
                Id = new Guid("83442088-9521-47ee-86e8-49b64daa2d38"),
                Description = "Replacement for the Disk Cleanup utility. Comes with more cleanup options, including Rebound temporary files and logs.",
                Icon = "ms-appx:///Assets/ReboundApps/DiskCleanup.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = 
                [
                    new()
                    {
                        Name = "Rebound Disk Cleanup",
                        Id = new("a474cf05-1513-4ac3-9019-c3d0a05d6576"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59")], // Launcher
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Kill Rebound Disk Cleanup",
                                CogId = new("3fd3ce71-24a6-466e-98da-875a3cf4c9cf"),
                                ProcessName = "Rebound.Cleanup",
                                KillOn = ProcessKillOn.Both,
                                RequiresElevation = true
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound Disk Cleanup Package Manager",
                                CogId = new("46e0b75a-1085-46fb-8226-8fad0be74a63"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.Cleanup.msixbundle"),
                                    PackageFamilyName: "Rebound.Cleanup_rcz2tbwv5qzb8")
                            },
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound Disk Cleanup Program Association",
                                CogId = new("e37e82aa-4d65-430b-9002-4352a08c18d4"),
                                OriginalExecutable = "cleanmgr.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.AUMID,
                                    "Rebound.Cleanup_rcz2tbwv5qzb8!App")
                            }
                        ],
                        Launchers =
                        [
                            new PackageLauncher()
                            {
                                PackageFamilyName = "Rebound.Cleanup_rcz2tbwv5qzb8"
                            }
                        ],
                    }
                ]
            },
            
            // Rebound About Windows
            new()
            {
                Name = "About Windows",
                Id = new("9fe3d6bd-2d64-45bc-b230-f4a605d67de7"),
                Description = "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
                Icon = "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
                Category = ModCategory.General,
                PreferredInstallationTemplate = InstallationTemplate.Basic,
                Variants = 
                [
                    new()
                    {
                        Name = "Rebound About",
                        Id = new("2917ef5b-c290-4414-ae2e-348e0c38f3c4"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59")], // Launcher
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Kill Rebound About",
                                CogId = new("7c521c7d-bfd1-4c38-bb4e-f791e9281915"),
                                ProcessName = "Rebound.About",
                                KillOn = ProcessKillOn.Both,
                                RequiresElevation = true
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound About Package Manager",
                                CogId = new("554f0b42-8265-4b76-87c3-eb532a2dd213"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.About.msixbundle"),
                                    PackageFamilyName: "Rebound.About_rcz2tbwv5qzb8")
                            },
                            new DLLInjectionCog()
                            {
                                CogName = "ShellAbout Redirection DLL",
                                CogId = new("d34554fc-f202-400a-80a9-cdbdc7a6f91a"),
                                DLLPathARM64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.About.ARM64.dll"),
                                DLLPathx64 = Path.Combine(AppContext.BaseDirectory, "Modding", "DLLs", "Rebound.Forge.Hooks.About.x64.dll"),
                                TargetProcesses = [ "*" ],
                                ExcludedProcesses = [ "winver.exe" ]
                            },
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound About Program Association",
                                CogId = new("90472e74-0bc2-4588-b619-90969bed67c8"),
                                OriginalExecutable = "winver.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.AUMID,
                                    "Rebound.About_rcz2tbwv5qzb8!App")
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
                    },
                    new()
                    {
                        Name = "Windows Settings - About",
                        Id = new("10044773-7031-4008-855e-accc0738899a"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59")], // Launcher
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound About Program Association",
                                CogId = new("134aac09-5627-4eee-9586-2e84fb824c70"),
                                OriginalExecutable = "winver.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "ms-settings:about")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("ms-settings:about")
                            }
                        ],
                    },
                    new()
                    {
                        Name = "Rebound Control Panel - About",
                        Id = new("aa6bbc41-290c-4372-b875-ea51b4da16ee"),
                        Dependencies = [new("80439df9-6cca-4d5e-89f8-96c10f4e6c59"), new("d889389f-ec76-4ca6-93c8-318abe1e853c")], // Launcher, Rebound Control Panel
                        Cogs =
                        [
                            new ProgramAssociationCog()
                            {
                                CogName = "Rebound Control Panel winver Program Association",
                                CogId = new("3b0a271e-bd12-44f7-8038-2bc5dd27ebc6"),
                                OriginalExecutable = "winver.exe",
                                Target = new ProgramAssociationTarget(
                                    ProgramAssociationType.URI,
                                    "rebound-controlpanel:about")
                            }
                        ],
                        Launchers =
                        [
                            new UriLauncher()
                            {
                                Uri = new("rebound-controlpanel:about")
                            }
                        ],
                    }
                ]
            }
        };
        return [with(mods.OrderBy(m => m.Name))];
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
                Id = new Guid("569a252d-86c2-4cfc-aa33-25c4a49623b8"),
                Description = "Mandatory mod required for Rebound to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/Environment.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("36f9393f-b6c1-473e-814a-89041688c86f"),
                        Cogs =
                        [
                            new FolderCog()
                            {
                                CogName = "Data folder",
                                CogId = new("41bc34e9-8be2-4d7a-95a7-15fc5ed5c9f9"),
                                Path = Variables.ReboundDataFolder,
                                PersistAfterRemoving = true,
                                RequiresElevation = false,
                                LockBehindAdmin = false
                            },
                            new FolderCog()
                            {
                                CogName = "Mods folder",
                                CogId = new("266105cd-5581-4b95-afd5-a30ac0d003be"),
                                Path = Variables.ReboundModsFolder,
                                PersistAfterRemoving = true,
                                RequiresElevation = true,
                                LockBehindAdmin = true
                            },
                            new FolderCog()
                            {
                                CogName = "DLLs folder",
                                CogId = new("6e9bd59d-1108-4a2e-b2af-5dfa97530dfc"),
                                Path = Variables.ReboundInjectedDLLsFolder,
                                PersistAfterRemoving = false,
                                RequiresElevation = true,
                                LockBehindAdmin = true
                            },
                            new FolderCog()
                            {
                                CogName = "Program associations folder",
                                CogId = new("6b3729f7-75bf-4de5-ad89-bc2edf5cb7a2"),
                                Path = Variables.ReboundProgramAssociationsFolder,
                                PersistAfterRemoving = false,
                                RequiresElevation = true,
                                LockBehindAdmin = true
                            },
                            new FolderCog()
                            {
                                CogName = "Start menu folder",
                                CogId = new("c7856ce9-587b-4851-a388-ee8446e2bfba"),
                                Path = Variables.ReboundStartMenuFolder,
                                PersistAfterRemoving = false,
                                RequiresElevation = false,
                                LockBehindAdmin = false
                            },
                            new TaskFolderCog()
                            {
                                CogName = "Rebound Task Scheduler folder",
                                CogId = new("53842f38-8781-4fce-8aea-c7fec031502b"),
                                TaskFolderName = "Rebound"
                            }
                        ],
                    }
                ]
            },

            // MinHook-Detours
            new()
            {
                Name = "MinHook-Detours",
                Id = new Guid("4e6c6105-6ae8-4ca0-8247-180f893604b1"),
                Description = "DLL injection runtime required for Rebound to work properly.",
                Icon = "ms-appx:///Assets/ReboundApps/InjectionDLLs.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("c6ce0751-7500-4d8d-9f9a-d564054bec6c"),
                        Cogs =
                        [
                            new FileCopyCog()
                            {
                                CogName = "ARM64 lib",
                                CogId = new("d65d4ff6-2a6a-4f56-9660-b0722cb5d734"),
                                IsDirectory = false,
                                RequiresElevation = true,
                                Path = Path.Combine(AppContext.BaseDirectory, "Libs", "minhook-detours.ARM64.Release.dll"),
                                TargetPath = Path.Combine(Variables.ReboundLibsFolder, "minhook-detours.ARM64.Release.dll")
                            },
                            new FileCopyCog()
                            {
                                CogName = "x64 lib",
                                CogId = new("ac162429-94bd-4a7f-857f-74400e72c391"),
                                IsDirectory = false,
                                RequiresElevation = true,
                                Path = Path.Combine(AppContext.BaseDirectory, "Libs", "minhook-detours.x64.Release.dll"),
                                TargetPath = Path.Combine(Variables.ReboundLibsFolder, "minhook-detours.x64.Release.dll")
                            }
                        ],
                    }
                ]
            },

            // Rebound (Service Host)
            new()
            {
                Name = "Rebound (Service Host)",
                Id = new Guid("c855b4cf-4f26-40dd-a38c-ac7aecb14029"),
                Description = "Mandatory background service required for Rebound apps to run properly.",
                Icon = "ms-appx:///Assets/ReboundApps/ServiceHost.ico",
                Category = ModCategory.Mandatory,
                PreferredInstallationTemplate = InstallationTemplate.Mandatory,
                Variants =
                [
                    new()
                    {
                        Name = "Default",
                        Id = new("e0acb7fb-2681-4c7f-b921-38a8b40100a9"),
                        Cogs =
                        [
                            new ProcessKillCog()
                            {
                                CogName = "Kill Rebound (Service Host)",
                                CogId = new("bbb3d616-c07d-4ff5-84a9-dc1336cb1882"),
                                ProcessName = "Rebound.ServiceHost",
                                KillOn = ProcessKillOn.Both,
                                RequiresElevation = true,
                                RequiresUserConfirmation = false
                            },
                            new PackageCog()
                            {
                                CogName = "Rebound (Service Host) Package Manager",
                                CogId = new("5690655c-4c78-482f-acc9-b9398d9cc03d"),
                                Target = new PackageTarget(
                                    PackageTargetType.Local,
                                    Path.Combine(AppContext.BaseDirectory, "Modding", "Packages", "Rebound.ServiceHost.msixbundle"),
                                    PackageFamilyName: "Rebound.ServiceHost_rcz2tbwv5qzb8")
                            },
                            new StartupTaskCog()
                            {
                                CogName = "Rebound (Service Host) Startup Task",
                                CogId = new("f93090af-c0a2-48ab-932c-e8f98734ec33"),
                                StartupTarget = new StartupTaskTarget(
                                    "Rebound.ServiceHost_rcz2tbwv5qzb8",
                                    StartupTaskTargetType.PackageFamilyName),
                                TaskRequiresElevation = true,
                                TaskDescription = "Rebound (Service Host) elevated helper for Rebound apps.",
                                TaskName = "Rebound (Service Host)"
                            },
                            new ProcessLaunchCog()
                            {
                                CogName = "Start Rebound (Service Host)",
                                CogId = new("69c7c511-e45d-4369-b73f-43f6e87d6551"),
                                LaunchOn = ProcessLaunchOn.Apply,
                                LaunchTarget = new ProcessLaunchTarget("Rebound.ServiceHost_rcz2tbwv5qzb8", ProcessLaunchTargetType.PackageFamilyName),
                                RequiresElevation = true,
                                RequiresUserConfirmation = false
                            }
                        ],
                    }
                ]
            },
        };
        return [with(mods.OrderBy(m => m.Name))];
    }

    /// <summary>
    /// Represents the Rebound Hub declaration mod. Must only be used in the installers.
    /// </summary>
    public readonly static Mod ReboundHub = new()
    {
        Name = "Rebound Hub",
        Id = new Guid("0E885036-4DC8-4AAA-9B12-A97E6C51BB82"),
        Description = "The Rebound Hub.",
        Icon = string.Empty,
        Category = ModCategory.Mandatory,
        PreferredInstallationTemplate = InstallationTemplate.Mandatory,
        Variants =
        [
            new()
            {
                Name = "Default",
                Id = new Guid("2ba8d562-9d00-4a13-b4d2-c949e3e41440"),
                Cogs =
                [
                    new ProcessKillCog()
                    {
                        CogName = "Rebound Hub Process Kill",
                        CogId = new("24a06efd-06e1-45e2-a30c-22f9516add5f"),
                        ProcessName = "Rebound Hub",
                        RequiresElevation = true,
                        KillOn = ProcessKillOn.Remove
                    },
                    new PackageCog()
                    {
                        CogName = "Rebound Hub Package",
                        CogId = new("71951744-33f4-44d1-bd6a-4dad64c47b1f"),
                        Target = new PackageTarget(PackageTargetType.Local, Path.Combine(AppContext.BaseDirectory, "Hub", "Rebound.Hub.msixbundle"), PackageFamilyName: "Rebound.Hub_rcz2tbwv5qzb8"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both
                    },
                ],
            }
        ]
    };

    /// <summary>
    /// Custom mods that are loaded from a folder at runtime.
    /// </summary>
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static ObservableCollection<Mod> SideloadedMods = []; // => [with(ModParser.ParseMods())];
#pragma warning restore CA2211 // Non-constant fields should not be visible
}