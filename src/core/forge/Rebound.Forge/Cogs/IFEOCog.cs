// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System.Text;
using TerraFX.Interop.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

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
    public string CogDescription { get => $"Register IFE entry for {OriginalExecutableName} to launch {LauncherPath}."; }

    /// <summary>
    /// Image File Execution Options can only be written to with admin privileges.
    /// </summary>
    public bool RequiresElevation { get; } = true;

    /// <summary>
    /// The name, with extension, of the executable to be redirected
    /// </summary>
    public required string OriginalExecutableName { get; set; }

    /// <summary>
    /// The full path of the executable to redirect to
    /// </summary>
    public required string LauncherPath { get; set; }

    private const string BaseRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken)
    {
        HKEY phkResult = default;

        try
        {
            ReboundLogger.WriteToLog(
                "IFEOCog Apply", 
                "Apply started.");

            // Bunch of PInvoke
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            // Open the key if it exists, otherwise create it
            var hr = TerraFX.Interop.Windows.Windows.RegCreateKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                null,
                REG.REG_OPTION_NON_VOLATILE,
                KEY.KEY_WRITE | KEY.KEY_WOW64_64KEY,
                null,
                &phkResult,
                null);

            if (TerraFX.Interop.Windows.Windows.SUCCEEDED(hr))
            {
                using ManagedArrayPtr<byte> bytesPtr = Encoding.Unicode.GetBytes(LauncherPath + "\0");
                using ManagedPtr<char> debugger = "Debugger";

                ReboundLogger.WriteToLog(
                    "IFEOCog Apply", 
                    "Writing to registry.");

                hr = TerraFX.Interop.Windows.Windows.RegSetValueExW(
                    phkResult,
                    debugger,
                    0,
                    REG.REG_SZ,
                    bytesPtr,
                    (uint)bytesPtr.ByteLength);

                if (TerraFX.Interop.Windows.Windows.FAILED(hr))
                {
                    ReboundLogger.WriteToLog(
                        "IFEOCog Apply", 
                        $"Failed to set Debugger value for {subKey}. Error code: {hr}", 
                        LogMessageSeverity.Error);
                    return Task.FromResult(new CogOperationResult(false, $"Failed to set Debugger value. Error code: {hr}", false));
                }

                ReboundLogger.WriteToLog(
                    "IFEOCog Apply",
                    $"Set Debugger value for {subKey} -> {LauncherPath}");
                return Task.FromResult(new CogOperationResult(true, null, true));
            }

            // Failed to create/open key (most likely because of missing privileges)
            else
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog Apply", 
                    $"Failed to create registry key {subKey}. Error code: {hr}", 
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, $"Failed to create registry key. Error code: {hr}", false));
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "IFEOCog Apply", 
                "Apply failed with exception.", 
                LogMessageSeverity.Error, 
                ex);
            return Task.FromResult(new CogOperationResult(false, "Apply failed with exception: " + ex.Message, false));
        }
        finally
        {
            if (phkResult != HKEY.NULL)
                _ = TerraFX.Interop.Windows.Windows.RegCloseKey(phkResult);
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog("IFEOCog Remove", "Remove started.");

            // More PInvoke stuff
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";
            var hr = TerraFX.Interop.Windows.Windows.RegDeleteTreeW(HKEY.HKEY_LOCAL_MACHINE, subKey);

            if (TerraFX.Interop.Windows.Windows.SUCCEEDED(hr))
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog Remove",
                    $"Deleted registry key {subKey}");
                return Task.FromResult(new CogOperationResult(true, null, true));
            }
            else
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog Remove", 
                    $"Failed to delete registry key {subKey}. Error code: {hr}", LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, $"Failed to delete registry key. Error code: {hr}", false));
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "IFEOCog Remove", 
                "Remove failed with exception.", 
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "Remove failed with exception: " + ex.Message, false));
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogStatus> GetStatusAsync()
    {
        HKEY hKey = default;

        try
        {
            ReboundLogger.WriteToLog(
                "IFEOCog GetStatus",
                "Get Status check started.");

            // More PInvoke
            using ManagedPtr<char> subKey = $@"{BaseRegistryPath}\{OriginalExecutableName}";

            var hr = TerraFX.Interop.Windows.Windows.RegOpenKeyExW(
                HKEY.HKEY_LOCAL_MACHINE,
                subKey,
                0,
                KEY.KEY_READ | KEY.KEY_WOW64_64KEY,
                &hKey);

            // Error opening key (most likely doesn't exist)
            if (TerraFX.Interop.Windows.Windows.FAILED(hr))
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog GetStatus",
                    $"Registry key {subKey} not found. Error code: {hr}", LogMessageSeverity.Warning);
                return Task.FromResult(new CogStatus(CogState.NotInstalled, null));
            }

            // Variables
            uint bufferSize = 0;
            using ManagedPtr<char> debugger = "Debugger";
            uint type;

            // First obtain the buffer size
            hr = TerraFX.Interop.Windows.Windows.RegQueryValueExW(
                hKey,
                debugger,
                null,
                &type,
                null,
                &bufferSize);

            if (TerraFX.Interop.Windows.Windows.FAILED(hr) 
                || type != REG.REG_SZ && type != REG.REG_EXPAND_SZ
                || bufferSize.Equals(0)) // Roslyn won't shut up if I use `==` or `is`
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog GetStatus",
                    $"Registry validation failed. hr={hr}, type={type}, size={bufferSize}",
                    LogMessageSeverity.Warning);

                return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "Debugger value invalid."));
            }

            using ManagedArrayPtr<byte> buffer = new byte[bufferSize];

            // Now read the key
            hr = TerraFX.Interop.Windows.Windows.RegQueryValueExW(
                hKey,
                debugger,
                null,
                &type,
                buffer,
                &bufferSize);

            if (TerraFX.Interop.Windows.Windows.FAILED(hr))
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog GetStatus",
                    $"Failed to query Debugger value for {subKey}. Error code: {hr}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, $"Failed to query Debugger value. Error code: {hr}"));
            }

            if (type != REG.REG_SZ)
            {
                ReboundLogger.WriteToLog(
                    "IFEOCog GetStatus",
                    $"Debugger value for {subKey} is not REG_SZ.",
                    LogMessageSeverity.Warning);

                return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "Debugger value has unexpected type."));
            }

            // Get the string value and trim null terminators
            int bytesWritten = (int)bufferSize;
            string value = Encoding.Unicode.GetString((byte*)buffer.ObjectPointer, bytesWritten / sizeof(char)).TrimEnd('\0');
            bool applied = string.Equals(value, LauncherPath, StringComparison.OrdinalIgnoreCase);

            ReboundLogger.WriteToLog(
                "IFEOCog GetStatus",
                $"Queried Debugger value for {subKey}: {value}. Expected: {LauncherPath}. Applied: {applied}");
            return Task.FromResult(new CogStatus(applied ? CogState.Installed : CogState.PartiallyInstalled, null));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "IFEOCog GetStatus",
                "GetStatus check failed with exception.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, "GetStatus check failed with exception: " + ex.Message));
        }
        finally
        {
            if (hKey != HKEY.NULL)
                _ = TerraFX.Interop.Windows.Windows.RegCloseKey(hKey);
        }
    }
}