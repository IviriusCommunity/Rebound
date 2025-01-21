using System;
using System.Collections.Generic;

#nullable enable

namespace Rebound.Helpers.Modding;

public partial class StandardReboundApp : IReboundPackagedApp, IReboundShortcutsApp, IReboundIFEOApp
{
    public virtual List<AppPackage>? AppPackages { get; set; }

    public virtual List<ReboundAppShortcut>? Shortcuts { get; set; }

    public virtual List<IFEOEntry>? IFEOEntries { get; set; }

    public void Install()
    {

    }

    public void Uninstall()
    {

    }

    public ReboundAppIntegrity GetIntegrity()
    {
        if (AppPackages == null || Shortcuts == null || IFEOEntries == null)
        {
            throw new InvalidOperationException("App packages and shortcuts must not be null.");
        }

        var isAppPackageIntact = true;
        var isShortcutIntact = true;
        var isIFEOEntryIntact = true;

        foreach (var appPackage in AppPackages)
        {
            if (!appPackage.IsInstalled())
            {
                isAppPackageIntact = false;
            }
        }

        foreach (var shortcut in Shortcuts)
        {
            if (!shortcut.IsShortcutIconModernized())
            {
                isShortcutIntact = false;
            }
        }

        foreach (var entry in IFEOEntries)
        {
            if (!entry.IsIntact())
            {
                isIFEOEntryIntact = false;
            }
        }

        return
            // Check if everything is ok
            isShortcutIntact && isAppPackageIntact && isIFEOEntryIntact ?

            // All good
            ReboundAppIntegrity.Installed :

            // Check if nothing is ok
            !isShortcutIntact && !isAppPackageIntact && !isIFEOEntryIntact ?

            // Not installed
            ReboundAppIntegrity.NotInstalled : 
            
            // Corrupt
            ReboundAppIntegrity.Corrupt;
    }
}