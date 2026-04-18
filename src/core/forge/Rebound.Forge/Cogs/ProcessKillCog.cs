// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.Diagnostics;

namespace Rebound.Forge.Cogs;

public class ProcessKillConfirmationEventArgs : EventArgs
{
    public TaskCompletionSource<bool> DecisionSource { get; } = new();
}

public enum ProcessKillOn
{
    Apply,
    Remove,
    Both
}

/// <summary>
/// Represents a cog that terminates a running process with a specified executable name.
/// </summary>
/// <remarks>
/// <para>
/// When this cog is <see cref="ApplyAsync"/> or <see cref="RemoveAsync"/> is called,
/// it attempts to locate and terminate all instances of the process specified by
/// <see cref="ProcessName"/>.
/// </para>
/// </remarks>
public class ProcessKillCog : ICog
{
    /// <summary>
    /// The display process name. Example: "Rebound Shell", without the additional quotes.
    /// </summary>
    public required string ProcessName { get; set; }

    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

    public bool RequiresUserConfirmation { get; set; } = true;

    public ProcessKillOn KillOn { get; set; } = ProcessKillOn.Both;

    /// <summary>
    /// Raised when the cog needs user confirmation to proceed with killing the process. The event args
    /// provide a <see cref="TaskCompletionSource{Boolean}"/> that the UI can use to signal the user's decision.
    /// </summary>
    public event EventHandler<ProcessKillConfirmationEventArgs>? OnConfirmationRequested;

    private async Task<CogOperationResult> KillProcess(CancellationToken cancellationToken = default)
    {
        bool targetExists = false;

        // First iteration through all processes to check if the target process is running
        var firstIterationProcesses = Process.GetProcesses().ToList();
        foreach (var process in firstIterationProcesses)
        {
            if (process.ProcessName == ProcessName)
            {
                // Found a process with the target name, log it and set the flag
                ReboundLogger.WriteToLog("Process Kill Cog", $"Found process {process.ProcessName} (PID {process.Id})");
                targetExists = true;
                break;
            }
        }

        // If there's no processes, return immediately since there's nothing to do anyway
        if (!targetExists)
        {
            ReboundLogger.WriteToLog("Process Kill Cog", $"No processes named {ProcessName} found, skipping kill operation");
            return new CogOperationResult(true, null, true);
        }

        // The args (yes)
        var args = new ProcessKillConfirmationEventArgs();

        bool confirmed;

        // Check if the action requires user confirmation
        if (RequiresUserConfirmation)
        {
            // Fire the event for the UI to catch
            OnConfirmationRequested?.Invoke(this, args);

            // Wait for the UI to respond with the user's decision
            try
            {
                confirmed = await args.DecisionSource.Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new CogOperationResult(false, "OPERATION_CANCELLED", false);
            }
        }
        // Otherwise, proceed directly
        else confirmed = true;

        // If confirmed, proceed to kill the processes
        if (confirmed)
        {
            // Second iteration through all processes to check if the target process is running
            var secondIterationProcesses = Process.GetProcesses().ToList();
            foreach (var process in secondIterationProcesses)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new CogOperationResult(false, "OPERATION_CANCELLED", false);

                if (process.ProcessName == ProcessName)
                {
                    // Found a process with the target name, kill it immediately and log the action
                    ReboundLogger.WriteToLog("Process Kill Cog", $"Killing process {process.ProcessName} (PID {process.Id})");
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception but continue trying to kill other processes
                        ReboundLogger.WriteToLog(
                            "Process Kill Cog",
                            $"Failed to kill process {process.ProcessName} (PID {process.Id}): {ex.Message}",
                            LogMessageSeverity.Error,
                            ex);

                        return new CogOperationResult(false, "FAILED_TO_KILL_TASK", false);
                    }
                }
            }

            return new CogOperationResult(true, null, true);
        }
        // If not confirmed, return a failure result indicating the operation was aborted
        else
            return new CogOperationResult(false, "USER_ABORTED", false);

    }

    /// <returns>
    /// Error text "USER_ABORTED" if the user declined to kill the process, "OPERATION_CANCELLED" if the user manually stops the operation, or "FAILED_TO_KILL_TASK" if an error occurred while trying to kill the process.
    /// </returns>
    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    { 
        if (KillOn is ProcessKillOn.Apply or ProcessKillOn.Both)
            return await KillProcess(cancellationToken).ConfigureAwait(false);
        else
            return new CogOperationResult(true, null, true);
    }

    /// <returns>
    /// Error text "USER_ABORTED" if the user declined to kill the process, "OPERATION_CANCELLED" if the user manually stops the operation, or "FAILED_TO_KILL_TASK" if an error occurred while trying to kill the process.
    /// </returns>
    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        if (KillOn is ProcessKillOn.Remove or ProcessKillOn.Both)
            return await KillProcess(cancellationToken).ConfigureAwait(false);
        else
            return new CogOperationResult(true, null, true);
    }

    /// <returns>
    /// Always ignorable, since the cog's effect is immediate and doesn't have a persistent state to check against.
    /// </returns>
    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
        => Task.FromResult(new CogStatus(CogState.Ignorable));
}