// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents a cog that terminates a running process with a specified executable name.
/// </summary>
/// <remarks>
/// <para>
/// When this cog is <see cref="ApplyAsync"/> or <see cref="RemoveAsync"/> is called,
/// it attempts to locate and terminate all instances of the process specified by
/// <see cref="ExecutableName"/>.
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
    public required string ProcessName { get; set; }

    public bool Ignorable { get; } = true;

    public ProcessKillCog() { }

    private async Task KillProcess()
    {
        var processes = Process.GetProcesses().ToList();
        foreach (var process in processes)
        {
            if (process.ProcessName == ProcessName)
            {
                process.Kill();
            }
        }
    }

    public async Task ApplyAsync()
    {
        await KillProcess();
    }

    public async Task RemoveAsync()
    {
        await KillProcess();
    }

    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}