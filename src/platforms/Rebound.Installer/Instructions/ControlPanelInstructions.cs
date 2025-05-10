using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class ControlPanelInstructions : ReboundAppInstructions
{
    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } =
    [
        new IFEOInstruction()
        {
            OriginalExecutableName = "control.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol.exe"
        },
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rcontrol.exe",
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol.exe"
        },
        new ShortcutInstruction()
        {
            ShortcutName = "Control Panel",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rcontrol.exe"
        }
    ];

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}