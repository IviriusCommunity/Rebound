// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;
namespace Rebound.Forge.Cogs;

/// <summary>
/// Registers a DLL for injection.
/// </summary>
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
            ReboundLogger.Log("[BoolSettingCog] Apply failed with exception.", ex);
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
            ReboundLogger.Log("[BoolSettingCog] Remove failed with exception.", ex);
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
            ReboundLogger.Log("[BoolSettingCog] IsApplied failed with exception.", ex);
            return false;
        }
    }
}