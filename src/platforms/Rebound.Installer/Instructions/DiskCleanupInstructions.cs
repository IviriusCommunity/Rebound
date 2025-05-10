using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class DiskCleanupInstructions : ReboundAppInstructions
{
    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } =
    [
        new IFEOInstruction()
        {
            OriginalExecutableName = "cleanmgr.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr.exe"
        },
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rcleanmgr.exe",
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr.exe"
        },
        new ShortcutInstruction()
        {
            ShortcutName = "Disk Cleanup",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcleanmgr.exe"
        }
    ];

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}