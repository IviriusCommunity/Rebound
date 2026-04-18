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
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

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
    public unsafe async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog("IFEOCog Apply", "Apply started.");

            // Bunch of PInvoke
            HKEY phkResult;
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            // Open the key if it exists, otherwise create it
            var result = TerraFX.Interop.Windows.Windows.RegCreateKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                null,
                REG.REG_OPTION_NON_VOLATILE,
                KEY.KEY_WRITE | KEY.KEY_WOW64_64KEY,
                null,
#pragma warning disable CS9123 // Shush I don't wanna hear it
                &phkResult,
#pragma warning restore CS9123
                null);

            // Success
            if (result is 0)
            {
                using ManagedArrayPtr<byte> bytesPtr = Encoding.Unicode.GetBytes(LauncherPath + "\0");
                using ManagedPtr<char> debugger = "Debugger";

                ReboundLogger.WriteToLog("IFEOCog Apply", "Writing to registry.");

                _ = TerraFX.Interop.Windows.Windows.RegSetValueExW(
                    phkResult,
                    debugger,
                    0,
                    REG.REG_SZ,
                    bytesPtr,
                    (uint)bytesPtr.ByteLength);

                ReboundLogger.WriteToLog("IFEOCog Apply", $"Set Debugger value for {subKey} -> {LauncherPath}");
                return new(true, null, true);
            }

            // Failed to create/open key (most likely because of missing privileges)
            else
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog Apply", 
                    $"Failed to create registry key {subKey}. Error code: {result}", 
                    LogMessageSeverity.Error);
                return new(false, $"Failed to create registry key. Error code: {result}", false);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "IFEOCog Apply", 
                "Apply failed with exception.", 
                LogMessageSeverity.Error, 
                ex);
            return new(false, "Apply failed with exception: " + ex.Message, false);
        }
    }

    /// <inheritdoc/>
    public unsafe async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog("IFEOCog Remove", "Remove started.");

            // More PInvoke stuff
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var result = TerraFX.Interop.Windows.Windows.RegDeleteKeyW(HKEY.HKEY_LOCAL_MACHINE, subKey);

            // Success
            if (result is 0)
            {
                ReboundLogger.WriteToLog("IFEOCog Remove", $"Deleted registry key {subKey}");
                return new(true, null, true);
            }

            // Failure
            else
            {
                ReboundLogger.WriteToLog("IFEOCog Remove", $"Failed to delete registry key {subKey}. Error code: {result}", LogMessageSeverity.Error);
                return new(false, $"Failed to delete registry key. Error code: {result}", false);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("IFEOCog Remove", "Remove failed with exception.", LogMessageSeverity.Error, ex);
            return new(false, "Remove failed with exception: " + ex.Message, false);
        }
    }

    /// <inheritdoc/>
    public unsafe async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog("IFEOCog Get Status", "Get Status check started.");

            // More PInvoke
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            HKEY hKey;

            var result = TerraFX.Interop.Windows.Windows.RegOpenKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                KEY.KEY_READ | KEY.KEY_WOW64_64KEY,
#pragma warning disable CS9123 // Shhhh
                &hKey);
#pragma warning restore CS9123

            // Error opening key (most likely doesn't exist)
            if (result is not 0)
            {
                ReboundLogger.WriteToLog("IFEOCog Get Status", $"Registry key {subKey} not found. Error code: {result}", LogMessageSeverity.Warning);
                return new(CogState.NotInstalled, null);
            }

            // Variables
            using ManagedArrayPtr<byte> buffer = new byte[1024];
            uint bufferSize = (uint)buffer.ByteLength;
            using ManagedPtr<char> debugger = "Debugger";
            uint type;

            var queryResult = TerraFX.Interop.Windows.Windows.RegQueryValueExW(
                hKey,
                debugger,
                null,
#pragma warning disable CS9123 // No
                &type,
                buffer,
                &bufferSize);
#pragma warning restore CS9123

            _ = TerraFX.Interop.Windows.Windows.RegCloseKey(hKey);

            // Failure
            if (queryResult is not 0)
            {
                ReboundLogger.WriteToLog("IFEOCog Get Status", $"Failed to query Debugger value for {subKey}. Error code: {queryResult}", LogMessageSeverity.Error);
                return new(CogState.PartiallyInstalled, $"Failed to query Debugger value. Error code: {queryResult}");
            }

            // Get the string value and trim null terminators
            int bytesWritten = (int)bufferSize;
            string value = Encoding.Unicode.GetString((byte*)buffer.ObjectPointer, bytesWritten / sizeof(char)).TrimEnd('\0');
            bool applied = value == LauncherPath;

            ReboundLogger.WriteToLog("IFEOCog Get Status", $"Queried Debugger value for {subKey}: {value}. Expected: {LauncherPath}. Applied: {applied}");
            return new(applied ? CogState.Installed : CogState.PartiallyInstalled, null);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("IFEOCog Get Status", "Get Status check failed with exception.", LogMessageSeverity.Error, ex);
            return new(CogState.Unknown, "Get Status check failed with exception: " + ex.Message);
        }
    }
}