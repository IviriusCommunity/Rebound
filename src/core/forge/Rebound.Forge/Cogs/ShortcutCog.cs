using System;
using System.IO;
using Rebound.Core.Helpers;
using Rebound.Helpers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Rebound.Forge;

internal class ShortcutCog : ICog
{
    public required string ExePath { get; set; }

    public required string ShortcutName { get; set; }

    public string? IconLocation { get; set; }

    private static string GetShortcutPath(string shortcutName) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            "Programs", "Rebound", $"{shortcutName}.lnk");

    public unsafe void Apply()
    {
        try
        {
            // Begin logging
            ReboundLogger.Log($"[ShortcutCog] Apply started for executable {ExePath}.");

            HRESULT hr;
            var shortcutPath = GetShortcutPath(ShortcutName);
            using ComPtr<IShellLinkW> shellLink = default;

            // Check if shortcut installation is enabled in settings
            if (!SettingsHelper.GetValue("InstallShortcuts", "rebound", true))
            {
                ReboundLogger.Log("[ShortcutCog] Shortcut installation disabled via settings. Exiting Apply.");
                return;
            }

            // Ensure the Rebound start menu folder exists
            WorkingEnvironment.EnsureFolderIntegrity();
            ReboundLogger.Log("[ShortcutCog] Ensured folder integrity.");

            // Create IShellLink instance
            fixed (Guid* iidIShellLink = &IShellLinkW.IID_Guid)
            {
                hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_ShellLink, 
                    null, 
                    CLSCTX.CLSCTX_INPROC_SERVER, 
                    iidIShellLink, 
                    (void**)shellLink.GetAddressOf());
            }
            ReboundLogger.Log("[ShortcutCog] Created IShellLink instance.");

            // Check for errors
            if (hr.Failed || shellLink.Get() is null)
            {
                ReboundLogger.Log($"[ShortcutCog] CoCreateInstance failed with HRESULT=0x{hr.Value:X}, pointer null? {shellLink.Get() is null}");
                return;
            }

            // Set executable path
            shellLink.Get()->SetPath(ExePath.ToPCWSTR());
            ReboundLogger.Log($"[ShortcutCog] Set executable path: {ExePath}");

            // Set working directory
            string workingDir = Path.GetDirectoryName(ExePath)!;
            shellLink.Get()->SetWorkingDirectory(workingDir.ToPCWSTR());
            ReboundLogger.Log($"[ShortcutCog] Set working directory: {workingDir}");

            // Set icon if provided
            if (!string.IsNullOrEmpty(IconLocation))
            {
                shellLink.Get()->SetIconLocation(IconLocation.ToPCWSTR(), 0);
                ReboundLogger.Log($"[ShortcutCog] Set icon location: {IconLocation}");
            }

            // Query IPersistFile
            using ComPtr<IPersistFile> pPersistFile = default;
            hr = ((IUnknown*)shellLink.Get())
                ->QueryInterface(IID.IID_IPersistFile, (void**)&pPersistFile);
            ReboundLogger.Log($"[ShortcutCog] QueryInterface(IPersistFile) returned HRESULT: 0x{hr.Value:X}");

            // Save the shortcut
            if (hr.Succeeded && pPersistFile.Get() is not null)
            {
                hr = pPersistFile.Get()->Save(shortcutPath.ToPCWSTR(), true);
                ReboundLogger.Log($"[ShortcutCog] Saved shortcut to {shortcutPath} with HRESULT: 0x{hr.Value:X}");
            }
            else
            {
                ReboundLogger.Log("[ShortcutCog] Failed to get IPersistFile pointer or QueryInterface failed.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ShortcutCog] Apply failed with exception.", ex);
        }

        // Finish logging
        ReboundLogger.Log($"[ShortcutCog] Apply finished for: {ShortcutName}");
    }

    public void Remove()
    {
        try
        {
            ReboundLogger.Log($"[ShortcutCog] Remove started for: {ShortcutName}");

            var shortcutPath = GetShortcutPath(ShortcutName);

            if (!SettingsHelper.GetValue("InstallShortcuts", "rebound", true))
            {
                ReboundLogger.Log("[ShortcutCog] Shortcut removal disabled via settings. Exiting Remove.");
                return;
            }

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
                ReboundLogger.Log("[ShortcutCog] Shortcut successfully deleted.");
            }
            else
            {
                ReboundLogger.Log("[ShortcutCog] No shortcut found to delete.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ShortcutCog] Remove failed with exception.", ex);
        }
    }

    public bool IsApplied()
    {
        try
        {
            ReboundLogger.Log($"[ShortcutCog] IsApplied check started for: {ShortcutName}");

            var shortcutPath = GetShortcutPath(ShortcutName);

            if (!SettingsHelper.GetValue("InstallShortcuts", "rebound", true))
            {
                ReboundLogger.Log("[ShortcutCog] Shortcut installation disabled via settings. Returning true.");
                return true;
            }

            bool exists = File.Exists(shortcutPath);
            ReboundLogger.Log($"[ShortcutCog] Shortcut path checked: {shortcutPath} → Exists = {exists}");

            return exists;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ShortcutCog] IsApplied failed with exception. Returning false.", ex);
            return false;
        }
    }
}