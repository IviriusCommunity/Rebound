// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Rebound.ControlPanel.Views;

internal partial class Tool
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ShellIcon { get; set; } = string.Empty;

    [RelayCommand]
    private async Task LaunchAppAsync()
    {
        if (Name.StartsWith("uri:", StringComparison.InvariantCultureIgnoreCase))
        {
            var uri = new Uri(Name.Substring(4));
            await Launcher.LaunchUriAsync(uri);
            return;
        }

        try
        {
            var info = new ProcessStartInfo()
            {
                FileName = Name,
                UseShellExecute = true,
            };
            if (!string.IsNullOrEmpty(Arguments))
                info.ArgumentList.Add(Arguments);
            Process.Start(info);
        }
        catch
        {

        }
    }

    [RelayCommand]
    private unsafe void CreateShortcut()
    {
        var path = Name.StartsWith("uri:", StringComparison.InvariantCultureIgnoreCase) ? Name.Substring(4) : Name;

        using StringPtr desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        using StringPtr shortcutPath = Path.Combine(desktopPath.ToManagedString()!, $"{DisplayName}.lnk");

        // Create an instance of the ShellLink component
        HRESULT hr = PInvoke.CoCreateInstance(
            *CLSID.CLSID_ShellLink,
            null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            out IShellLinkW* shellLink);

        if (hr.Failed)
            throw new Win32Exception($"Failed to create IShellLink instance. HRESULT: {hr}");

        // Set the path to the application/executable the shortcut launches
        using StringPtr target = $"{path}";
        shellLink->SetPath(target.GetChars());

        // Set arguments
        if (!string.IsNullOrWhiteSpace(Arguments))
        {
            using StringPtr args = $"{Arguments}";
            shellLink->SetArguments(args.GetChars());
        }

        // Set the shortcut's description
        using StringPtr description = $"{Description}";
        shellLink->SetDescription(description.GetChars());

        // Set the custom icon
        using StringPtr icon = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, Icon["ms-appx:///".Length..]);
        shellLink->SetIconLocation(icon.GetChars(), 0);

        // Query for IPersistFile to save the shortcut to disk
        shellLink->QueryInterface(out IPersistFile* persistFile);

        // Save the shortcut (.lnk) file
        persistFile->Save(shortcutPath.GetChars(), true);
    }
}

internal sealed partial class WindowsToolsPage : Page
{
    readonly List<Tool> GeneralTools =
    [
        new() { Name = "winver.exe", DisplayName = "About Windows", Description = "View details about your Windows version.", Icon = "ms-appx:///Assets/Apps/AboutWindows.ico" },
        new() { Name = "calc.exe", DisplayName = "Calculator", Description = "Make calculations and graph functions.", Icon = "ms-appx:///Assets/Apps/Calculator.ico" },
        new() { Name = "uri:microsoft.windows.camera:", DisplayName = "Camera", Description = "Browse and copy special characters from installed fonts.", Icon = "ms-appx:///Assets/Apps/Camera.ico" },
        new() { Name = "charmap.exe", DisplayName = "Character Map", Description = "Browse and copy special characters from installed fonts.", Icon = "ms-appx:///Assets/Apps/CharacterMap.ico" },
        new() { Name = "uri:ms-clock:", DisplayName = "Clock", Description = "Create alarms and set timers.", Icon = "ms-appx:///Assets/Apps/Clock.ico" },
        new() { Name = "control", DisplayName = "Control Panel", Description = "Open the Control Panel home page.", Icon = "ms-appx:///Assets/ControlPanel.ico" },
        new() { Name = "explorer.exe", DisplayName = "File Explorer", Description = "Manage files on this computer and connected cloud services.", Icon = "ms-appx:///Assets/Apps/FileExplorer.ico" },
        new() { Name = "uri:ms-media-player:", DisplayName = "Media Player", Description = "Play audio and video files.", Icon = "ms-appx:///Assets/Apps/MediaPlayer.ico" },
        new() { Name = "uri:zune:", DisplayName = "Microsoft Store", Description = "Browse and purchase apps, games, and other content.", Icon = "ms-appx:///Assets/Apps/MicrosoftStore.ico" },
        new() { Name = "notepad.exe", DisplayName = "Notepad", Description = "Create and edit text files.", Icon = "ms-appx:///Assets/Apps/Notepad.ico" },
        new() { Name = "mspaint.exe", DisplayName = "Paint", Description = "Create and edit images.", Icon = "ms-appx:///Assets/Apps/Paint.ico" },
        new() { Name = "uri:ms-photos:", DisplayName = "Photos", Description = "View and edit photos.", Icon = "ms-appx:///Assets/Apps/Photos.ico" },
        new() { Name = "uri:ms-quick-assist:", DisplayName = "Quick Assist", Description = "Connect to remote computers.", Icon = "ms-appx:///Assets/Apps/QuickAssist.ico" },
        new() { Name = "regedit", DisplayName = "Registry Editor", Description = "View and edit the Windows registry.", Icon = "ms-appx:///Assets/Apps/RegistryEditor.ico" },
        new() { Name = "explorer", Arguments = "shell:::{2559A1F3-21D7-11D4-BDAF-00C04F60B9F0}", DisplayName = "Run", Description = "Run programs and commands.", Icon = "ms-appx:///Assets/Apps/Run.ico" },
        new() { Name = "explorer", Arguments = "shell:AppsFolder\\9390SimonKnuth.ScannerforWindows10_69n05hp4v3s90!App", DisplayName = "Scanner", Description = "Scan photos and documents.", Icon = "ms-appx:///Assets/Apps/Scanner.ico" },
        new() { Name = "uri:ms-settings:", DisplayName = "Settings", Description = "Modify Windows settings.", Icon = "ms-appx:///Assets/Apps/Settings.ico" },
        new() { Name = "uri:ms-screensketch:", DisplayName = "Snipping Tool", Description = "Capture screenshots of parts of your screen.", Icon = "ms-appx:///Assets/Apps/SnippingTool.ico" },
        new() { Name = "explorer", Arguments = "shell:AppsFolder\\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe!App", DisplayName = "Sound Recorder", Description = "Record audio from your microphone.", Icon = "ms-appx:///Assets/Apps/SoundRecorder.ico" },
        new() { Name = "msinfo32.exe", DisplayName = "System Information", Description = "View system information and hardware details.", Icon = "ms-appx:///Assets/Apps/SystemInformation.ico" },
        new() { Name = "taskmgr.exe", DisplayName = "Task Manager", Description = "View and manage running applications and processes.", Icon = "ms-appx:///Assets/Apps/TaskManager.ico" },
        new() { Name = "uri:windowsdefender:", DisplayName = "Windows Security", Description = "Antivirus and threat protection.", Icon = "ms-appx:///Assets/Apps/WindowsSecurity.ico" },
    ];

    readonly List<Tool> ManagementTools =
    [
        new() { Name = "compmgmt.msc", DisplayName = "Computer Management", Description = "Manage computer settings and configurations.", Icon = "ms-appx:///Assets/Apps/ComputerManagement.ico" },
        new() { Name = "devmgmt.msc", DisplayName = "Device Manager", Description = "Manage hardware devices and drivers.", Icon = "ms-appx:///Assets/Apps/DeviceManager.ico" },
        new() { Name = "diskmgmt.msc", DisplayName = "Disk Management", Description = "Manage disks, volumes, and partitions.", Icon = "ms-appx:///Assets/Apps/DiskManagement.ico" },
        new() { Name = "eventvwr.msc", DisplayName = "Event Viewer", Description = "View system logs, errors, and warnings.", Icon = "ms-appx:///Assets/Apps/EventViewer.ico" },
        new() { Name = "virtmgmt.msc", DisplayName = "Hyper-V Manager", Description = "Manage virtual machines and virtual switches.", Icon = "ms-appx:///Assets/Apps/HyperVManager.ico" },
        new() { Name = "secpol.msc", DisplayName = "Local Security Policy", Description = "Configure local security settings and policies.", Icon = "ms-appx:///Assets/Apps/LocalSecurityPolicy.ico" },
        new() { Name = "mmc.exe", DisplayName = "Management Console", Description = "Create and run custom administrative snap-ins.", Icon = "ms-appx:///Assets/Apps/ManagementConsole.ico" },
        new() { Name = "perfmon.msc", DisplayName = "Performance Monitor", Description = "Monitor system performance and resource usage.", Icon = "ms-appx:///Assets/Apps/PerformanceMonitor.ico" },
        new() { Name = "printmanagement.msc", DisplayName = "Print Management", Description = "Manage printers, drivers, and print servers.", Icon = "ms-appx:///Assets/Apps/PrintManagement.ico" },
        new() { Name = "resmon.exe", DisplayName = "Resource Monitor", Description = "Monitor real-time CPU, disk, network, and memory usage.", Icon = "ms-appx:///Assets/Apps/ResourceMonitor.ico" },
        new() { Name = "services.msc", DisplayName = "Services", Description = "Manage background Windows services.", Icon = "ms-appx:///Assets/Apps/Services.ico" },
        new() { Name = "taskschd.msc", DisplayName = "Task Scheduler", Description = "Schedule automated tasks and scripts.", Icon = "ms-appx:///Assets/Apps/TaskScheduler.ico" },
    ];

    readonly List<Tool> CommandTools =
    [
        new() { Name = "cmd.exe", DisplayName = "Command Prompt", Description = "Run command-line programs and scripts.", Icon = "ms-appx:///Assets/Apps/CommandPrompt.ico" },
        new() { Name = "powershell.exe", DisplayName = "Windows PowerShell", Description = "Automate tasks and manage system configurations.", Icon = "ms-appx:///Assets/Apps/WindowsPowerShell.ico" },
        new() { Name = "explorer", Arguments = "shell:AppsFolder\\Microsoft.WindowsTerminal_8wekyb3d8bbwe!App", DisplayName = "Windows Terminal", Description = "Access multiple command-line environments in tabs.", Icon = "ms-appx:///Assets/Apps/WindowsTerminal.ico" },
    ];

    readonly List<Tool> MiscellaneousTools =
    [
        new() { Name = "cleanmgr.exe", DisplayName = "Disk Cleanup", Description = "Free up space on your hard disk by deleting unnecessary files.", Icon = "ms-appx:///Assets/Apps/DiskCleanup.ico" },
        new() { Name = "recoverydrive.exe", DisplayName = "Recovery Drive", Description = "Create a recovery drive to troubleshoot or reset your PC.", Icon = "ms-appx:///Assets/Apps/RecoveryDrive.ico" },
        new() { Name = "mdsched.exe", DisplayName = "Windows Memory Diagnostic", Description = "Check your computer's memory for hardware errors.", Icon = "ms-appx:///Assets/Apps/MemoryDiagnostic.ico" },
    ];

    readonly List<Tool> LegacyTools =
    [
        new() { Name = "dcomcnfg.exe", DisplayName = "Component Services", Description = "Configure COM components and COM+ applications.", Icon = "ms-appx:///Assets/Apps/ComponentServices.ico" },
        new() { Name = "dfrgui.exe", DisplayName = "Defragment and Optimize Drives", Description = "Optimize drives to help your computer run more efficiently.", Icon = "ms-appx:///Assets/Apps/Defragment.ico" },
        new() { Name = "iscsicpl.exe", DisplayName = "iSCSI Initiator", Description = "Configure connections to iSCSI storage arrays.", Icon = "ms-appx:///Assets/Apps/IscsiInitiator.ico" },
        new() { Name = "odbcad32.exe", DisplayName = "ODBC Data Sources", Description = "Manage database drivers and data sources (ODBC).", Icon = "ms-appx:///Assets/Apps/OdbcDataSources.ico" },
        new() { Name = "wf.msc", DisplayName = "Windows Defender Firewall with Advanced Security", Description = "Configure advanced firewall settings and security rules.", Icon = "ms-appx:///Assets/Apps/WindowsFirewall.ico" },
    ];

    public WindowsToolsPage()
    {
        InitializeComponent();
    }

    private void Grid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.Tag is Tool tool)
            tool.LaunchAppCommand.Execute(null);
    }
}
