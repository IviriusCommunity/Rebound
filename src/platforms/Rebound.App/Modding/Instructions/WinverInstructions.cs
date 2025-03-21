using System;
using System.Collections.ObjectModel;
using Rebound.Helpers.Modding;

namespace Rebound.Modding.Instructions;

public partial class WinverInstructions : ReboundAppInstructions
{
    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } = new()
    {
        new IFEOInstruction()
        {
            OriginalExecutableName = "winver.exe",
            LauncherPath = $"%PROGRAMFILES%\\Rebound\\rwinver.exe"
        },
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rwinver.exe",
            TargetPath = $"%PROGRAMFILES%\\Rebound\\rwinver.exe"
        }
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}