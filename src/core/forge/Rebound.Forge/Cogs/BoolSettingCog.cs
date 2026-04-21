// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Settings;
namespace Rebound.Forge.Cogs;

public class BoolSettingCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

    /// <summary>
    /// The name of the application the setting applies to.
    /// </summary>
    public required string AppName { get; set; }

    /// <summary>
    /// The setting's name.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The value that will be applied when <see cref="ApplyAsync"/> is called. The opposite
    /// value will be written when <see cref="RemoveAsync"/> is called.
    /// </summary>
    public required bool AppliedValue { get; set; }

    /// <inheritdoc/>
    public string TaskDescription { get => $"Set {Key} to {AppliedValue}"; }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SettingsManager.SetValue(Key, AppName, AppliedValue);
            return new CogOperationResult(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't apply the bool setting cog.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "Couldn't apply the bool setting cog.", true);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SettingsManager.SetValue(Key, AppName, !AppliedValue);
            return new CogOperationResult(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't remove the bool setting cog.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "Couldn't remove the bool setting cog.", true);
        }
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        try
        {
            return new(AppliedValue == SettingsManager.GetValue(Key, AppName, !AppliedValue) ? CogState.Installed : CogState.NotInstalled);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't check if the bool setting cog is applied.",
                LogMessageSeverity.Error,
                ex);
            return new CogStatus(CogState.Unknown, "Couldn't check the status of the cog.");
        }
    }
}