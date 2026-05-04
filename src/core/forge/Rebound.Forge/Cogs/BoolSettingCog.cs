// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Settings;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Writes a boolean value to an application setting on apply, and its inverse on remove.
/// </summary>
public class BoolSettingCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Set {Key} to {AppliedValue} in {SettingsFileName}.xml"; }

    /// <summary>
    /// Writing boolean settings to AppData never requires elevation.
    /// </summary>
    public bool RequiresElevation => false;

    /// <summary>
    /// The file name (without extension) of the settings file in AppData\Local. 
    /// For example, "rebound" for the "rebound.xml" settings file.
    /// </summary>
    public required string SettingsFileName { get; set; }

    /// <summary>
    /// The key identifying the setting to write.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The value written when <see cref="ApplyAsync"/> is called.
    /// <see cref="RemoveAsync"/> will write the inverse.
    /// </summary>
    public required bool AppliedValue { get; set; }

    /// <inheritdoc/>
    public Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog Apply", 
                $"Applying setting {Key} for {SettingsFileName}");
            SettingsManager.SetValue(Key, SettingsFileName, AppliedValue);
            ReboundLogger.WriteToLog(
                "BoolSettingCog Apply", 
                $"Applied setting {Key} for {SettingsFileName}");

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog Apply", 
                $"Failed to apply setting {Key} for {SettingsFileName}.", 
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", true));
        }
    }

    /// <inheritdoc/>
    public Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog Remove",
                $"Removing setting {Key} for {SettingsFileName}");
            SettingsManager.SetValue(Key, SettingsFileName, !AppliedValue);
            ReboundLogger.WriteToLog(
                "BoolSettingCog Remove",
                $"Removed setting {Key} for {SettingsFileName}");

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog Remove", 
                $"Failed to remove setting {Key} for {SettingsFileName}.",
                LogMessageSeverity.Error, 
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", true));
        }
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog GetStatus", 
                $"Checking status of setting {Key} for {SettingsFileName}");

            // The default passed to GetValue is the inverse of AppliedValue so that a missing key
            // is treated as not installed rather than accidentally matching.
            var current = SettingsManager.GetValue(Key, SettingsFileName, !AppliedValue);
            var state = current == AppliedValue ? CogState.Installed : CogState.NotInstalled;

            ReboundLogger.WriteToLog(
                "BoolSettingCog GetStatus",
                $"The status of setting {Key} for {SettingsFileName} is {state}");

            return Task.FromResult(new CogStatus(state));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog GetStatus", 
                $"Failed to check status of setting {Key} for {SettingsFileName}.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, "An error occurred."));
        }
    }
}