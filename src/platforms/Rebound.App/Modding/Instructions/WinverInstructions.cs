using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class WinverInstructions : UserInterfaceReboundAppInstructions
{
    public override string ProcessName { get; set; } = "Rebound About";

    public override string Name { get; set; } = "About Windows";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/AboutWindows.ico";

    public override string Description { get; set; } = "Replacement for the winver applet.";

    public override string InstallationSteps { get; set; } = "- Redirect app launch\n- Create a start menu shortcut";

    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } = new()
    {
        new IFEOInstruction()
        {
            OriginalExecutableName = "winver.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
        },
        new ShortcutInstruction()
        {
            ShortcutName = "About Windows",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rwinver\\Rebound About.exe"
        },
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Basic;
}