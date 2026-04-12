// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System.Text;
using TerraFX.Interop.Windows;

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

    /// <inheritdoc/>
    public string TaskDescription { get => $"Register an IFEO entry for {OriginalExecutableName}"; }

    /// <inheritdoc/>
    public unsafe async Task ApplyAsync()
    {
        try
        {
            ReboundLogger.Log("[IFEOCog] Apply started.");

            HKEY phkResult;

            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var result = TerraFX.Interop.Windows.Windows.RegCreateKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                null,
                (uint)REG.REG_OPTION_NON_VOLATILE,
                (uint)(KEY.KEY_WRITE | KEY.KEY_WOW64_64KEY),
                null,
                &phkResult,
                null);

            if (result == 0) // ERROR_SUCCESS
            {
                byte[] bytes = Encoding.Unicode.GetBytes(LauncherPath + "\0");
                ReboundLogger.Log($"[IFEOCog] Writing {bytes.Length} bytes to registry.");
                fixed (byte* pBytes = bytes)
                {
                    using ManagedPtr<char> debugger = "Debugger";
                    _ = TerraFX.Interop.Windows.Windows.RegSetValueExW(
                        phkResult,
                        debugger,
                        0,
                        (uint)REG.REG_SZ,
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

            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            var result = TerraFX.Interop.Windows.Windows.RegDeleteKeyW(HKEY.HKEY_LOCAL_MACHINE, subKey);
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
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            HKEY hKey;
            var result = TerraFX.Interop.Windows.Windows.RegOpenKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                (uint)(KEY.KEY_READ | KEY.KEY_WOW64_64KEY),
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
                using ManagedPtr<char> debugger = "Debugger";
                var queryResult = TerraFX.Interop.Windows.Windows.RegQueryValueExW(
                    hKey,
                    debugger,
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