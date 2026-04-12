// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Settings;
namespace Rebound.Forge.Cogs;

public class BoolSettingCog : ICog
{
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
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription { get => $"Set {Key} to {AppliedValue}"; }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        try
        {
            SettingsManager.SetValue(Key, AppName, AppliedValue);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't apply the bool setting cog.",
                LogMessageSeverity.Error,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        try
        {
            SettingsManager.SetValue(Key, AppName, !AppliedValue);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't remove the bool setting cog.",
                LogMessageSeverity.Error,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        try
        {
            return AppliedValue == SettingsManager.GetValue(Key, AppName, !AppliedValue);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "BoolSettingCog apply",
                "Couldn't check if the bool setting cog is applied.",
                LogMessageSeverity.Error,
                ex);
            return false;
        }
    }
}