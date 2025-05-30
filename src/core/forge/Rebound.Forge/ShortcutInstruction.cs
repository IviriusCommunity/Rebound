using System;
using System.IO;
using System.Runtime.InteropServices;
using Rebound.Helpers;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Rebound.Forge;

public class ShortcutInstruction : IReboundAppInstruction
{
    public required string ExePath { get; set; }
    public required string ShortcutName { get; set; }
    public string? IconLocation { get; set; }

    public ShortcutInstruction()
    {
    }

    public unsafe void Apply()
    {
        try
        {
            if (!SettingsHelper.GetValue<bool>("InstallShortcuts", "rebound", true))
            {
                return;
            }

            // Ensure Rebound is properly installed
            ReboundWorkingEnvironment.EnsureFolderIntegrity();

            // Get the start menu folder of Rebound
            var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");

            var shortcutPath = Path.Combine(startMenuFolder, $"{ShortcutName}.lnk");

            // Create ShellLink object
            var clsidShellLink = new Guid("00021401-0000-0000-C000-000000000046"); // CLSID_ShellLink
            var iidShellLink = new Guid("000214F9-0000-0000-C000-000000000046");  // IID_IShellLinkW

            PInvoke.CoCreateInstance(in clsidShellLink, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER, in iidShellLink, out var shellLinkObj);
            var targetPathPtr = Marshal.StringToHGlobalUni(Path.GetDirectoryName(ExePath));
            var iconLocationPtr = Marshal.StringToHGlobalUni(IconLocation);

            var shellLink = (IShellLinkW)shellLinkObj;
            shellLink.SetPath(ExePath);
            shellLink.SetWorkingDirectory(new Windows.Win32.Foundation.PCWSTR((char*)targetPathPtr));
            if (!string.IsNullOrEmpty(IconLocation))
            {
                shellLink.SetIconLocation(new Windows.Win32.Foundation.PCWSTR((char*)iconLocationPtr), 0);
            }

            // Save it to file using IPersistFile
            var persistFile = (Windows.Win32.System.Com.IPersistFile)shellLink;
            persistFile.Save(shortcutPath, true);
        }
        catch
        {

        }
    }

    public void Remove()
    {
        try
        {
            if (!SettingsHelper.GetValue<bool>("InstallShortcuts", "rebound", true))
            {
                return;
            }

            var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");
            var shortcutPath = Path.Combine(startMenuFolder, $"{ShortcutName}.lnk");

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
        catch
        {

        }
    }

    public bool IsApplied()
    {
        try
        {
            if (!SettingsHelper.GetValue<bool>("InstallShortcuts", "rebound", true))
            {
                return true;
            }
            var startMenuFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu), "Programs", "Rebound");
            var shortcutPath = Path.Combine(startMenuFolder, $"{ShortcutName}.lnk");
            return File.Exists(shortcutPath);
        }
        catch
        {
            return false;
        }
    }
}