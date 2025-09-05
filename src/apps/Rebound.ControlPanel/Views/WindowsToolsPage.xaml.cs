using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

public partial class Tool
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }

    [RelayCommand]
    private void LaunchApp(string name)
    {
        if (name == "taskmgr")
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "taskmgr",
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch
            {

            }
        }
        else
        {
            try
            {
                Process.Start(name);
            }
            catch
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = name,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
        }
    }
}

public sealed partial class WindowsToolsPage : Page
{
    List<Tool> Tools =
    [
        new() { Name = "winver", DisplayName = "About Windows", Description = "View details about your Windows version.", Icon = "ms-appx:///Assets/winver.ico" },
        new() { Name = "charmap", DisplayName = "Character Map", Description = "Browse and copy special characters from installed fonts.", Icon = "ms-appx:///Assets/CharacterMap.png" },
        new() { Name = "cmd", DisplayName = "Command Prompt", Description = "Open the legacy Windows Command Prompt.", Icon = "ms-appx:///Assets/cmd.ico" },
        new() { Name = "dcomcnfg", DisplayName = "Component Services", Description = "Manage COM+ and DCOM application settings.", Icon = "ms-appx:///Assets/componentservices.ico" },
        new() { Name = "compmgmt.msc", DisplayName = "Computer Management", Description = "Access system tools like disk management and services.", Icon = "ms-appx:///Assets/compmgmt.ico" },
        new() { Name = "control", DisplayName = "Control Panel", Description = "Open the Control Panel home page.", Icon = "ms-appx:///Assets/controlpanel.ico" },
        new() { Name = "dfrgui", DisplayName = "Defragment and Optimize Drives", Description = "Defragment and optimize system drives.", Icon = "ms-appx:///Assets/dfrgui.ico" },
        new() { Name = "cleanmgr", DisplayName = "Disk Cleanup", Description = "Free up space by removing temporary files.", Icon = "ms-appx:///Assets/cleanmgr.ico" },
        new() { Name = "eventvwr", DisplayName = "Event Viewer", Description = "View system and application event logs.", Icon = "ms-appx:///Assets/eventvwr.ico" },
        new() { Name = "virtmgmt.msc", DisplayName = "Hyper‑V Manager", Description = "Manage Hyper-V virtual machines and settings.", Icon = "ms-appx:///Assets/executable.ico" },
        new() { Name = "hypervquickcreate", DisplayName = "Hyper‑V Quick Create", Description = "Quickly create a Hyper‑V virtual machine.", Icon = "ms-appx:///Assets/executable.ico" },
        new() { Name = "iscsicpl", DisplayName = "iSCSI Initiator", Description = "Configure iSCSI storage connections.", Icon = "ms-appx:///Assets/iscsicpl.ico" },
        new() { Name = "secpol.msc", DisplayName = "Local Security Policy", Description = "Define local security policies and permissions.", Icon = "ms-appx:///Assets/secpol.ico" },
        new() { Name = "odbcad32", DisplayName = "ODBC Data Sources", Description = "Manage ODBC drivers and data source names.", Icon = "ms-appx:///Assets/odbc.ico" },
        new() { Name = "perfmon", DisplayName = "Performance Monitor", Description = "Monitor system performance in real time.", Icon = "ms-appx:///Assets/perfmon.ico" },
        new() { Name = "printmanagement.msc", DisplayName = "Print Management", Description = "View and manage printers and print servers.", Icon = "ms-appx:///Assets/printmanagement.ico" },
        new() { Name = "recoverydrive.exe", DisplayName = "Recovery Drive", Description = "Create or back up a USB recovery drive.", Icon = "ms-appx:///Assets/recoverydrive.ico" },
        new() { Name = "regedit", DisplayName = "Registry Editor", Description = "Edit the Windows system registry.", Icon = "ms-appx:///Assets/regedit.ico" },
        new() { Name = "mstsc", DisplayName = "Remote Desktop", Description = "Connect to another computer using Remote Desktop.", Icon = "ms-appx:///Assets/remote_desktop.ico" },
        new() { Name = "resmon", DisplayName = "Resource Monitor", Description = "Track CPU, memory, disk, and network usage.", Icon = "ms-appx:///Assets/perfmon.ico" },
        new() { Name = "services.msc", DisplayName = "Services", Description = "Manage Windows services.", Icon = "ms-appx:///Assets/services.ico" },
        new() { Name = "snippingtool", DisplayName = "Snipping Tool", Description = "Capture screenshots and annotate.", Icon = "ms-appx:///Assets/snippingtool.png" },
        new() { Name = "msconfig", DisplayName = "System Configuration", Description = "Configure startup applications and boot options.", Icon = "ms-appx:///Assets/msconfig.ico" },
        new() { Name = "msinfo32", DisplayName = "System Information", Description = "View detailed system hardware and software info.", Icon = "ms-appx:///Assets/msinfo32.ico" },
        new() { Name = "taskmgr", DisplayName = "Task Manager", Description = "Manage running processes and performance.", Icon = "ms-appx:///Assets/taskmgr.ico" },
        new() { Name = "taskschd.msc", DisplayName = "Task Scheduler", Description = "Schedule automated tasks and scripts.", Icon = "ms-appx:///Assets/taskschd.ico" },
        new() { Name = "wf.msc", DisplayName = "Windows Defender Firewall with Advanced Security", Description = "Configure advanced firewall rules and settings.", Icon = "ms-appx:///Assets/wf.ico" },
        new() { Name = "wfs", DisplayName = "Windows Fax and Scan", Description = "Send and receive faxes and scans.", Icon = "ms-appx:///Assets/wfs.ico" },
        new() { Name = "wmplayer", DisplayName = "Windows Media Player", Description = "Play audio and video files.", Icon = "ms-appx:///Assets/wmplayer.ico" },
        new() { Name = "mdsched", DisplayName = "Windows Memory Diagnostic", Description = "Test your computer’s memory for errors.", Icon = "ms-appx:///Assets/mdsched.ico" },
        new() { Name = "powershell", DisplayName = "Windows PowerShell", Description = "Run command‑line tasks and scripts in PowerShell.", Icon = "ms-appx:///Assets/executable.ico" },
        new() { Name = "powershell_ise", DisplayName = "Windows PowerShell ISE", Description = "Integrated Scripting Environment for PowerShell.", Icon = "ms-appx:///Assets/executable.ico" },
        new() { Name = "wordpad", DisplayName = "WordPad", Description = "Edit RTF documents.", Icon = "ms-appx:///Assets/wordpad.png" }
    ];

    public WindowsToolsPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void LaunchApp(string name)
    {
        if (name == "taskmgr")
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "taskmgr",
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch
            {

            }
        }
        else
        {
            try
            {
                Process.Start(name);
            }
            catch
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = name,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
        }
    }
}
