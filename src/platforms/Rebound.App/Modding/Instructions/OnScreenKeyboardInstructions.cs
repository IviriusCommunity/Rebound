using System;
using System.Collections.ObjectModel;
using Rebound.Forge;

namespace Rebound.Modding.Instructions;

public partial class OnScreenKeyboardInstructions : UserInterfaceReboundAppInstructions
{
    public override string Name { get; set; } = "On-Screen Keyboard";

    public override string Icon { get; set; } = "ms-appx:///Assets/AppIcons/OSK.ico";

    public override string Description { get; set; } = "Replacement for the old On-Screen Keyboard using the existing UWP keyboard.";

    public override string InstallationSteps { get; set; } = "Redirect app launch";

    public override ObservableCollection<IReboundAppInstruction>? Instructions { get; set; } = new()
    {
        new IFEOInstruction()
        {
            OriginalExecutableName = "osk.exe",
            LauncherPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
        },
        new LauncherInstruction()
        {
            Path = $"{AppContext.BaseDirectory}\\Modding\\Launchers\\rosk.exe",
            TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\\Rebound\\rosk.exe"
        }
    };

    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
}