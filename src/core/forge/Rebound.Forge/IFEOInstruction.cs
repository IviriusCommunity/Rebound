using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Rebound.Core.Helpers;
using System;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.Registry;

namespace Rebound.Forge;

public class IFEOInstruction : IReboundAppInstruction
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public required string OriginalExecutableName { get; set; }

    public required string LauncherPath { get; set; }

    public IFEOInstruction()
    {

    }

    public unsafe void Apply()
    {
        try
        {
            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));

            HKEY phkResult;

            var result = PInvoke.RegCreateKeyEx(
                HKEY_LOCAL_MACHINE,
                $@"{BaseRegistryPath}\{OriginalExecutableName}".ToPCWSTR(),
                0,
                null,
                REG_OPEN_CREATE_OPTIONS.REG_OPTION_NON_VOLATILE,
                REG_SAM_FLAGS.KEY_WRITE,
                null,
                &phkResult);

            var bytes = Encoding.Unicode.GetBytes(LauncherPath + "\0");

            if (result == 0) // ERROR_SUCCESS
            {
                unsafe
                {
                    fixed (byte* pBytes = bytes) // pin the array
                    {
                        PInvoke.RegSetValueEx(
                            phkResult,
                            "Debugger".ToPCWSTR(),
                            0,
                            REG_VALUE_TYPE.REG_SZ,
                            pBytes,               // pass pointer to pinned bytes
                            (uint)bytes.Length
                        );
                    }
                }
            }
        }
        catch
        {
        }
    }

    public unsafe void Remove()
    {
        try
        {
            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));
            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            PInvoke.RegDeleteKey(HKEY_LOCAL_MACHINE, subKey.ToPCWSTR());
        }
        catch
        {
        }
    }

    public unsafe bool IsApplied()
    {
        try
        {
            var HKEY_LOCAL_MACHINE = new HKEY(unchecked((int)0x80000002));
            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            // Open the key
            HKEY hKey;
            var result = PInvoke.RegOpenKeyEx(
                HKEY_LOCAL_MACHINE,
                subKey.ToPCWSTR(),
                0,
                REG_SAM_FLAGS.KEY_READ,
                &hKey
            );
            if (result != 0) return false; // failed to open

            // Query the "Debugger" value
            byte[] buffer = new byte[1024];
            uint size = (uint)buffer.Length;

            fixed (byte* pBuffer = buffer)
            {
                var win32Result = PInvoke.RegQueryValueEx(
                    hKey,
                    "Debugger".ToPCWSTR());

                // Close the key
                PInvoke.RegCloseKey(hKey);

                if (win32Result != 0) return false;

                string value = Encoding.Unicode.GetString(buffer, 0, (int)size - 2); // remove null terminator
                return value == LauncherPath;
            }
        }
        catch
        {
            return false;
        }
    }
}