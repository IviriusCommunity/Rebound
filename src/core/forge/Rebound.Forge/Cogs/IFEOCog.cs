// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using System.Text;
using Windows.Win32.System.Registry;
using HKEY = TerraFX.Interop.Windows.HKEY;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Registers an Image File Execution Options entry to redirect one app
/// to another.
/// </summary>
public class IFEOCog : ICog
{
    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    /// <summary>
    /// The name, with extension, of the executable to be redirected
    /// </summary>
    public required string OriginalExecutableName { get; set; }

    /// <summary>
    /// The full path of the executable to redirect to
    /// </summary>
    public required string LauncherPath { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="IFEOCog"/> class.
    /// </summary>
    public IFEOCog() { }

    /// <inheritdoc/>
    public unsafe async Task ApplyAsync()
    {
        try
        {
            ReboundLogger.Log("[IFEOCog] Apply started.");

            HKEY phkResult;

            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var result = TerraFX.Interop.Windows.Windows.RegCreateKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey.ToPointer(),
                0,
                null,
                (uint)REG_OPEN_CREATE_OPTIONS.REG_OPTION_NON_VOLATILE,
                (uint)(REG_SAM_FLAGS.KEY_WRITE | REG_SAM_FLAGS.KEY_WOW64_64KEY),
                null,
                &phkResult,
                null);

            if (result == 0) // ERROR_SUCCESS
            {
                byte[] bytes = Encoding.Unicode.GetBytes(LauncherPath + "\0");
                ReboundLogger.Log($"[IFEOCog] Writing {bytes.Length} bytes to registry.");
                fixed (byte* pBytes = bytes)
                {
                    _ = TerraFX.Interop.Windows.Windows.RegSetValueExW(
                        phkResult,
                        "Debugger".ToPointer(),
                        0,
                        (uint)REG_VALUE_TYPE.REG_SZ,
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

    /// <inheritdoc/>
    public unsafe async Task RemoveAsync()
    {
        try
        {
            ReboundLogger.Log("[IFEOCog] Remove started.");

            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            var result = TerraFX.Interop.Windows.Windows.RegDeleteKeyW(HKEY.HKEY_LOCAL_MACHINE, subKey.ToPointer());
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

    /// <inheritdoc/>
    public unsafe async Task<bool> IsAppliedAsync()
    {
        try
        {
            string subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            HKEY hKey;
            var result = TerraFX.Interop.Windows.Windows.RegOpenKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey.ToPointer(),
                0,
                (uint)(REG_SAM_FLAGS.KEY_READ | REG_SAM_FLAGS.KEY_WOW64_64KEY),
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
                uint type;
                var queryResult = TerraFX.Interop.Windows.Windows.RegQueryValueExW(
                    hKey,
                    "Debugger".ToPointer(),
                    null,
                    &type,
                    pBuffer,
                    &size);

                _ = TerraFX.Interop.Windows.Windows.RegCloseKey(hKey);

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