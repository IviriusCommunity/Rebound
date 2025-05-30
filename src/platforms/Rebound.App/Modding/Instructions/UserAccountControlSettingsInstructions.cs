using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class UserAccountControlSettingsInstructions : UserInterfaceReboundAppInstructions
{
    public override string ProcessName { get; set; } = "ruseraccountcontrolsettings";

    public override string Name { get; set; } = "UAC Settings";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/Admin.ico";

    public override string Description { get; set; } = "Replacement for the useraccountcontrolsettings applet.";

    public override string InstallationSteps { get; set; } = "- Install Rebound User Account Control Settings\n- Redirect app launch\n- Create a start menu shortcut";

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
        },
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}