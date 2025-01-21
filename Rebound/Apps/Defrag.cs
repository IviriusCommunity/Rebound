using System.Collections.Generic;
using Rebound.Helpers.Modding;

#nullable enable

namespace Rebound.Apps;

public partial class Defrag : StandardReboundApp
{
    public override List<ReboundAppShortcut>? Shortcuts { get; set; } =
    [
        new ReboundAppShortcut()
        {

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