using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class ControlPanelInstructions : UserInterfaceReboundAppInstructions
{
    public override string ProcessName { get; set; } = "Rebound Control Panel";

    public override string Name { get; set; } = "Control Panel";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/ControlPanel.ico";

    public override string Description { get; set; } = "Replacement for the Control Panel.";

    public override string InstallationSteps { get; set; } = "\n- Redirect app launch\n- Create a start menu shortcut";

    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } =
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
    ];

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}