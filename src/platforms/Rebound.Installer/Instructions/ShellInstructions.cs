using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class ShellInstructions : ReboundAppInstructions
{
    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } = new()
    {
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rshell.exe",
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell.exe"
        },
        new StartupTaskInstruction()
        {
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell.exe"
        }
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
}