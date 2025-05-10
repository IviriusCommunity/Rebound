using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class UserAccountControlSettingsInstructions : ReboundAppInstructions
{
    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } = new()
    {
        new IFEOInstruction()
        {
            OriginalExecutableName = "useraccountcontrolsettings.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings.exe"
        },
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\ruseraccountcontrolsettings.exe",
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings.exe"
        },
        new ShortcutInstruction()
        {
            ShortcutName = "Change User Account Control Settings",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\ruseraccountcontrolsettings.exe"
        }
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}