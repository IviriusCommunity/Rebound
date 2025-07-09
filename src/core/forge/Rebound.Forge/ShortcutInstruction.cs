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
                return;

            ReboundWorkingEnvironment.EnsureFolderIntegrity();

            var startMenuFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs", "Rebound");

            var shortcutPath = Path.Combine(startMenuFolder, $"{ShortcutName}.lnk");

            var clsidShellLink = CLSID.CLSID_ShellLink;
            var iidIShellLinkW = IShellLinkW.IID_Guid;
            var iidIPersistFile = IID.IID_IPersistFile;

            IShellLinkW* pShellLink; 

            Windows.Win32.Foundation.HRESULT hr = PInvoke.CoCreateInstance(
                clsidShellLink,
                null,
                Windows.Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
                &iidIShellLinkW,
                (void**)&pShellLink);

            if (hr.Failed || pShellLink is null)
                return;

            fixed (char* exePath = ExePath)
            {
                pShellLink->SetPath(new Windows.Win32.Foundation.PCWSTR(exePath));
            }

            string workingDir = Path.GetDirectoryName(ExePath)!;
            fixed (char* workingDirPtr = workingDir)
            {
                pShellLink->SetWorkingDirectory(new Windows.Win32.Foundation.PCWSTR(workingDirPtr));
            }

            if (!string.IsNullOrEmpty(IconLocation))
            {
                fixed (char* iconPath = IconLocation)
                {
                    pShellLink->SetIconLocation(new Windows.Win32.Foundation.PCWSTR(iconPath), 0);
                }
            }

            Windows.Win32.System.Com.IPersistFile* pPersistFile;
            hr = ((Windows.Win32.System.Com.IUnknown*)pShellLink)->QueryInterface(iidIPersistFile, (void**)&pPersistFile);

            if (hr.Succeeded && pPersistFile is not null)
            {
                fixed (char* shortcutPathPtr = shortcutPath)
                {
                    pPersistFile->Save(new Windows.Win32.Foundation.PCWSTR(shortcutPathPtr), true);
                }

                pPersistFile->Release();
            }

            pShellLink->Release();
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