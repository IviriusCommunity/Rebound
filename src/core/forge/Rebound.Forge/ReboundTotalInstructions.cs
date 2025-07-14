using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rebound.Forge;

public static class ReboundTotalInstructions
{
    public static ObservableCollection<ReboundAppInstructions> AppInstructions { get; } =
    [
        // Control Panel
        new()
        {
            ProcessName = "Rebound Control Panel",
            Name = "Control Panel",
            Icon = "ms-appx:///Assets/AppIcons/ControlPanel.ico",
            Description = "Replacement for the Control Panel.",
            InstallationSteps = "\n- Redirect app launch\n- Create a start menu shortcut",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe",
            Instructions =
            [
                new IFEOInstruction()
                {
                    OriginalExecutableName = "control.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe"
                },
                new ShortcutInstruction()
                {
                    ShortcutName = "Control Panel",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol\\Rebound Control Panel.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },

        // Disk Cleanup
        new()
        {
            ProcessName = "Rebound Disk Cleanup",
            Name = "Disk Cleanup",
            Icon = "ms-appx:///Assets/AppIcons/cleanmgr.ico",
            Description = "Replacement for the Disk Cleanup utility.",
            InstallationSteps = "- Redirect app launch\n- Create a start menu shortcut",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe",
            Instructions =
            [
                new IFEOInstruction()
                {
                    OriginalExecutableName = "cleanmgr.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe"
                },
                new ShortcutInstruction()
                {
                    ShortcutName = "Disk Cleanup",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr\\Rebound Disk Cleanup.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
        
        // On-Screen Keyboard
        new ()
        {
            ProcessName = "rosk",
            Name = "On-Screen Keyboard",
            Icon = "ms-appx:///Assets/AppIcons/OSK.ico",
            Description = "Temporarily deprecated. Turning this on will not do anything.",
            InstallationSteps = "Redirect app launch",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe",
            Instructions =
            [
                new IFEOInstruction()
                {
                    OriginalExecutableName = "osk.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
                },
                new LauncherInstruction()
                {
                    Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rosk.exe",
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Extras
        },
        
        // Rebound Shell
        new()
        {
            ProcessName = "Rebound Shell",
            Name = "Rebound Shell",
            Icon = "ms-appx:///Assets/AppIcons/ReboundIcon.ico",
            Description = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.",
            InstallationSteps = "- Register a startup task\n\nYou can choose which components are enabled from the Options menu at the top of the page. Note: Rebound Shell alone doesn't have a predefined UI. To check if it's running, try opening one of the applets it replaces.",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe",
            Instructions =
            [
                new StartupTaskInstruction()
                {
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe",
                    Description = "Rebound Shell task for overlapping the Windows shell.",
                    Name = "Shell",
                    RequireAdmin = false,
                },
                new ShortcutInstruction()
                {
                    ShortcutName = "Rebound Shell",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell\\Rebound Shell.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Extras
        },
        
        // Rebound User Account Control Settings
        new()
        {
            ProcessName = "Rebound User Account Control Settings",
            Name = "UAC Settings",
            Icon = "ms-appx:///Assets/AppIcons/Admin.ico",
            Description = "Replacement for the useraccountcontrolsettings applet.",
            InstallationSteps = "- Redirect app launch\n- Create a start menu shortcut",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe",
            Instructions =
            [
                new IFEOInstruction()
                {
                    OriginalExecutableName = "useraccountcontrolsettings.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe"
                },
                new ShortcutInstruction()
                {
                    ShortcutName = "Change User Account Control Settings",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings\\Rebound User Account Control Settings.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        },
        
        // Rebound About Windows
        new()
        {
            ProcessName = "Rebound About",
            Name = "About Windows",
            Icon = "ms-appx:///Assets/AppIcons/AboutWindows.ico",
            Description = "Replacement for the winver applet.",
            InstallationSteps = "- Redirect app launch\n- Create a start menu shortcut",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe",
            Instructions =
            [
                new IFEOInstruction()
                {
                    OriginalExecutableName = "winver.exe",
                    LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
                },
                new ShortcutInstruction()
                {
                    ShortcutName = "About Windows",
                    ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
                },
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        }
    ];

    public static ObservableCollection<ReboundAppInstructions> MandatoryInstructions { get; } =
    [
        // Service Host
        new()
        {
            ProcessName = "Rebound Service Host",
            Name = "Service Host",
            Icon = "ms-appx:///Assets/AppIcons/ServiceHost.ico",
            Description = "Mandatory background service required for Rebound apps to run properly.",
            InstallationSteps = "- Register a startup task as admin",
            EntryExecutable = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rsvchost\\Rebound Service Host.exe",
            Instructions =
            [
                new StartupTaskInstruction()
                {
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rsvchost\\Rebound Service Host.exe",
                    Description = "Rebound Service Host task for managing Rebound actions as admin.",
                    Name = "Service Host",
                    RequireAdmin = true,
                }
            ],
            PreferredInstallationTemplate = InstallationTemplate.Basic
        }
    ];

    public static ReboundAppInstructions GetAppInstructions(string name)
    {
        foreach (var instruction in AppInstructions)
        {
            if (instruction.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return instruction;
            }
        }
        throw new KeyNotFoundException($"App instructions with name '{name}' not found.");
    }

    public static ReboundAppInstructions GetMandatoryInstructions(string name)
    {
        foreach (var instruction in MandatoryInstructions)
        {
            if (instruction.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return instruction;
            }
        }
        throw new KeyNotFoundException($"Mandatory instructions with name '{name}' not found.");
    }
}