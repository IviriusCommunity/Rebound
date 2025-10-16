using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rebound.Forge;

internal static class Catalog
{
    internal static ObservableCollection<Mod> Mods { get; } =
    [
        // Control Panel
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
        },
        
        // Rebound Shell
        new Mod(
            name: "Rebound Shell",
            description: "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
            icon: "ms-appx:///Assets/ReboundApps/Shell.ico",
            installationSteps: "•   Register a startup task\n•   Hijack selected applets\n\nYou can choose which components are enabled from the Options menu at the top of the page.\n\nNote: Rebound Shell alone doesn't have a predefined UI. To check if it's running, try opening one of the applets it replaces.",
            instructions: new ObservableCollection<ICog>
            {
                new StartupTaskCog()
                {
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe",
                    Description = "Rebound Shell task for overlapping the Windows shell.",
                    Name = "Shell",
                    RequireAdmin = false
                },
                new ShortcutCog()
                {
                    ShortcutName = "Rebound Shell",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe"
                },
            },
            processName: "Rebound Shell"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe",
            PreferredInstallationTemplate = InstallationTemplate.Extras
        },
        
        // Rebound User Account Control Settings
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
        },
        
        // Rebound About Windows
        new Mod(
            name: "About Windows",
            description: "Replacement for the winver applet. Details about the currently installed Rebound version can be found here.",
            icon: "ms-appx:///Assets/ReboundApps/AboutWindows.ico",
            installationSteps: "•   Redirect app launch\n•   Create a start menu shortcut",
            instructions: new ObservableCollection<ICog>
            {
                new IFEOCog()
                {
                    OriginalExecutableName = "winver.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
                },
                new ShortcutCog()
                {
                    ShortcutName = "About Windows",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
                },
            },
            processName: "Rebound About",
            settings: new ObservableCollection<IModItem>
            {
                new ModLabel()
                {
                    Text = "Layout"
                },
                new ModBoolSetting()
                {
                    Name = "Show Rebound version",
                    Description = "Show the installed Rebound version above the bottom bar.",
                    Identifier = "ShowReboundVersion",
                    AppName = "rwinver"
                },
                new ModInfoBar()
                {
                    IsClosable = false,
                    Title = "You will still be able to see the Rebound version inside Rebound Hub.",
                    Severity = ModInfoBarSeverity.Informational
                }
            }
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe",
            PreferredInstallationTemplate = InstallationTemplate.Basic
        }
    ];

    internal static ObservableCollection<Mod> MandatoryMods { get; } =
    [
        // Service Host
        new Mod(
            name: "Service Host",
            description: "Mandatory background service required for Rebound apps to run properly.",
            icon: "ms-appx:///Assets/ReboundApps/ServiceHost.ico",
            installationSteps: "Register a startup task as admin",
            instructions: new ObservableCollection<ICog>
            {
                new StartupTaskCog()
                {
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rsvchost\\Rebound Service Host.exe",
                    Description = "Rebound Service Host task for managing Rebound actions as admin.",
                    Name = "Service Host",
                    RequireAdmin = true
                }
            },
            processName: "Rebound Service Host"
        )
        {
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rsvchost\\Rebound Service Host.exe",
            PreferredInstallationTemplate = InstallationTemplate.Basic
        }
    ];

    internal static ObservableCollection<Mod> SideloadedMods => new(ModParser.ParseMods());

    internal static Mod GetMod(string name)
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

    internal static Mod GetMandatoryMod(string name)
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