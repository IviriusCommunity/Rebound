// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.Diagnostics;
using TerraFX.Interop.Windows;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents a cog that terminates a running process with a specified executable name.
/// </summary>
/// <remarks>
/// <para>
/// When this cog is <see cref="ApplyAsync"/> or <see cref="RemoveAsync"/> is called,
/// it attempts to locate and terminate all instances of the process specified by
/// <see cref="ProcessName"/>.
/// </para>
/// <para>
/// The <see cref="IsAppliedAsync"/> method always returns <see langword="true"/>.
/// It is recommended to apply this cog before other cogs to ensure that no
/// interfering processes are running (for example, to make sure a program is closed
/// before modifying its files).
/// </para>
/// </remarks>
public class ProcessKillCog : ICog
{
    /// <summary>
    /// The display process name. Example: Rebound Shell
    /// </summary>
    public required string ProcessName { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; } = true;

    /// <inheritdoc/>
    public string TaskDescription { get => $"Kill the process named {ProcessName}"; }

    private async Task KillProcess()
    {
        var processes = Process.GetProcesses().ToList();
        foreach (var process in processes)
        {
            if (process.ProcessName == ProcessName)
            {
                ReboundLogger.Log($"[ProcessKillCog] Killing process {process.ProcessName}");
                process.Kill();
            }
        }
    }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        await KillProcess().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        await KillProcess().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}