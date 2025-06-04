using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class DiskCleanupInstructions : UserInterfaceReboundAppInstructions
{
    public override string ProcessName { get; set; } = "Rebound Disk Cleanup";

    public override string Name { get; set; } = "Disk Cleanup";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/cleanmgr.ico";

    public override string Description { get; set; } = "Replacement for the Disk Cleanup utility.";

    public override string InstallationSteps { get; set; } = "- Redirect app launch\n- Create a start menu shortcut";

    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } =
    [
        new IFEOInstruction()
        {
            OriginalExecutableName = "cleanmgr.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\ReboundHub\\Modding\\Apps\\rcleanmgr\\Rebound Disk Cleanup.exe"
        },
        new ShortcutInstruction()
        {
            ShortcutName = "Disk Cleanup",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\ReboundHub\\Modding\\Apps\\rcleanmgr\\Rebound Disk Cleanup.exe"
        },
    ];

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}