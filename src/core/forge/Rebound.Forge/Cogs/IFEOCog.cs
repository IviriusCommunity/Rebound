using Rebound.Core.Helpers;
using System;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.Registry;

namespace Rebound.Forge;

internal class IFEOCog : ICog
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public required string OriginalExecutableName { get; set; }

    public required string LauncherPath { get; set; }

    public IFEOCog()
    {

    }

    public unsafe void Apply()
    {
        try
        {
            ReboundLogger.Log("[IFEOCog] Apply started.");

            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));
            HKEY phkResult;

            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var result = PInvoke.RegCreateKeyEx(
                HKEY_LOCAL_MACHINE,
                subKey.ToPCWSTR(),
                0,
                null,
                REG_OPEN_CREATE_OPTIONS.REG_OPTION_NON_VOLATILE,
                REG_SAM_FLAGS.KEY_WRITE | REG_SAM_FLAGS.KEY_WOW64_64KEY,
                null,
                &phkResult);

            if (result == 0) // ERROR_SUCCESS
            {
                byte[] bytes = Encoding.Unicode.GetBytes(LauncherPath + "\0");
                ReboundLogger.Log($"[IFEOCog] Writing {bytes.Length} bytes to registry.");
                fixed (byte* pBytes = bytes)
                {
                    PInvoke.RegSetValueEx(
                        phkResult,
                        "Debugger".ToPCWSTR(),
                        0,
                        REG_VALUE_TYPE.REG_SZ,
                        pBytes,
                        (uint)bytes.Length);
                }

                ReboundLogger.Log($"[IFEOCog] Set Debugger value for {subKey} → {LauncherPath}");
            }
            else
            {
                ReboundLogger.Log($"[IFEOCog] Failed to create registry key {subKey}. Error code: {result}");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[IFEOCog] Apply failed with exception.", ex);
        }
    }

    public unsafe void Remove()
    {
        try
        {
            ReboundLogger.Log("[IFEOCog] Remove started.");

            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));
            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            var result = PInvoke.RegDeleteKey(HKEY_LOCAL_MACHINE, subKey.ToPCWSTR());
            if (result == 0)
            {
                ReboundLogger.Log($"[IFEOCog] Deleted registry key {subKey}");
            }
            else
            {
                ReboundLogger.Log($"[IFEOCog] Failed to delete registry key {subKey}. Error code: {result}");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[IFEOCog] Remove failed with exception.", ex);
        }
    }

    public unsafe bool IsApplied()
    {
        try
        {
            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));
            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            HKEY hKey;
            var result = PInvoke.RegOpenKeyEx(
                HKEY_LOCAL_MACHINE,
                subKey.ToPCWSTR(),
                0,
                REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WOW64_64KEY,
                &hKey);

            if (result != 0)
            {
                ReboundLogger.Log($"[IFEOCog] Registry key {subKey} not found.");
                return false;
            }

            byte[] buffer = new byte[1024];
            uint size = (uint)buffer.Length;
            fixed (byte* pBuffer = buffer)
            {
                REG_VALUE_TYPE type;
                var queryResult = PInvoke.RegQueryValueEx(
                    hKey,
                    "Debugger".ToPCWSTR(),
                    null,
                    &type,
                    pBuffer,
                    &size);

                PInvoke.RegCloseKey(hKey);

                if (queryResult != 0)
                {
                    ReboundLogger.Log($"[IFEOCog] Debugger value not found in {subKey}.");
                    return false;
                }

                string value = Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
                bool applied = value == LauncherPath;
                ReboundLogger.Log($"[IFEOCog] IsApplied check for {subKey} → {applied}");
                return applied;
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[IFEOCog] IsApplied failed with exception.", ex);
            return false;
        }
    }
}