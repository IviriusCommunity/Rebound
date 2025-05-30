using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class ShellInstructions : UserInterfaceReboundAppInstructions
{
    public override string Name { get; set; } = "Rebound Shell";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/ReboundIcon.ico";

    public override string Description { get; set; } = "Replacement for the shell and its components such as the run box, shutdown dialog, desktop, etc.";

    public override string InstallationSteps { get; set; } = "- Install Rebound Shell\n- Register a startup task\n\nYou can choose which components are enabled from the Options menu at the top of the page.";

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
        },
        new ShortcutInstruction()
        {
            ShortcutName = "Rebound Shell",
            ExePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rshell.exe"
        },
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
}