using System.Collections.Generic;
using Rebound.Helpers.Modding;

#nullable enable

namespace Rebound.Apps;

public partial class DefragInstructions : ReboundAppInstructions
{
    public override InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Recommended;

    public override List<ReboundAppShortcut>? Shortcuts { get; set; } =
    [
        new ReboundAppShortcut()
        {
            OriginalIconLocation = "C:\\Windows\\System32\\shell32.dll #456",
            ModernIconLocation = "C:\\Windows\\System32\\shell32.dll #20",
            ReplaceExisting = true,
            Path = "C:\\Windows\\someshortcut.lnk",
            RunAsAdmin = true, // Just bind this to the RunAsAdmin toggle switch in the installer
            TargetPath = null // Replace existing ignores target path
        }
    ];

    public override List<AppPackage>? AppPackages { get; set; } =
    [
        new AppPackage()
        {

        }
    ];

    public override List<IFEOEntry>? IFEOEntries { get; set; } =
    [
        new IFEOEntry()
        {

        }
    ];
}