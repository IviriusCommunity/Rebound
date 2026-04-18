// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;

namespace Rebound.Forge.Cogs;

public class ProcessLaunchConfirmationEventArgs : EventArgs
{
    public TaskCompletionSource<bool> DecisionSource { get; } = new();
}

public enum ProcessLaunchOn
{
    Apply,
    Remove,
    Both
}

public enum ProcessLaunchTargetType
{
    ExecutablePath,
    PackageFamilyName
}

public record ProcessLaunchTarget(string Target, ProcessLaunchTargetType TargetType);

/// <summary>
/// Represents a cog that launches an executable or a package app.
/// Always ignorable — launching is a fire-and-forget side effect with no persistent state to check.
/// </summary>
public class ProcessLaunchCog : ICog
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
    /// The launch target — either a full executable path or a package family name.
    /// </summary>
    public required ProcessLaunchTarget LaunchTarget { get; set; }

    /// <summary>
    /// Whether the user must confirm before the launch proceeds.
    /// </summary>
    public bool RequiresUserConfirmation { get; set; } = true;

    /// <summary>
    /// Whether to launch on apply, remove, or both.
    /// </summary>
    public ProcessLaunchOn LaunchOn { get; set; } = ProcessLaunchOn.Apply;

    /// <summary>
    /// Raised when the cog needs user confirmation before launching.
    /// </summary>
    public event EventHandler<ProcessLaunchConfirmationEventArgs>? OnConfirmationRequested;

    private async Task<CogOperationResult> LaunchProcess(CancellationToken cancellationToken = default)
    {
        // Ask for confirmation if required
        if (RequiresUserConfirmation)
        {
            var args = new ProcessLaunchConfirmationEventArgs();
            OnConfirmationRequested?.Invoke(this, args);

            bool confirmed;
            try
            {
                confirmed = await args.DecisionSource.Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new CogOperationResult(false, "OPERATION_CANCELLED", false, true);
            }

            if (!confirmed)
                return new CogOperationResult(false, "USER_ABORTED", false, true);
        }

        return LaunchTarget.TargetType switch
        {
            ProcessLaunchTargetType.ExecutablePath => await LaunchExecutable(cancellationToken).ConfigureAwait(false),
            ProcessLaunchTargetType.PackageFamilyName => await LaunchPackage(cancellationToken).ConfigureAwait(false),
            _ => new CogOperationResult(false, "INVALID_TARGET_TYPE", false, true)
        };
    }

    private Task<CogOperationResult> LaunchExecutable(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(new CogOperationResult(false, "OPERATION_CANCELLED", false, true));

        // Check if the target is a valid string
        if (string.IsNullOrWhiteSpace(LaunchTarget.Target))
            return Task.FromResult(new CogOperationResult(false, "INVALID_PATH", true, true));

        // Check if the target exists
        if (!File.Exists(LaunchTarget.Target))
        {
            ReboundLogger.WriteToLog("Process Launch Cog", $"File does not exist: {LaunchTarget.Target}", LogMessageSeverity.Error);
            return Task.FromResult(new CogOperationResult(false, "FILE_NOT_FOUND", true, true));
        }

        // Try shell execute as user first
        if (TryShellExecute(LaunchTarget.Target, elevate: false))
            return Task.FromResult(new CogOperationResult(true, null, true, true));

        ReboundLogger.WriteToLog("Process Launch Cog", "Normal launch failed, retrying elevated.", LogMessageSeverity.Warning);
        
        // If it doesn't work, retry as admin
        if (TryShellExecute(LaunchTarget.Target, elevate: true))
            return Task.FromResult(new CogOperationResult(true, null, true, true));

        ReboundLogger.WriteToLog("Process Launch Cog", "All launch attempts failed.", LogMessageSeverity.Error);
        return Task.FromResult(new CogOperationResult(false, "LAUNCH_FAILED", true, true));
    }

    private async Task<CogOperationResult> LaunchPackage(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return new CogOperationResult(false, "OPERATION_CANCELLED", false, true);

        try
        {
            var packageManager = new PackageManager();
            var package = packageManager
                .FindPackagesForUser(string.Empty, LaunchTarget.Target)
                .FirstOrDefault();

            if (package == null)
            {
                ReboundLogger.WriteToLog("Process Launch Cog", $"Package not found: {LaunchTarget.Target}", LogMessageSeverity.Error);
                return new CogOperationResult(false, "PACKAGE_NOT_FOUND", true, true);
            }

            var apps = await package.GetAppListEntriesAsync().AsTask(cancellationToken).ConfigureAwait(false);

            if (apps.Count == 0)
            {
                ReboundLogger.WriteToLog("Process Launch Cog", $"No app entries found in package: {LaunchTarget.Target}", LogMessageSeverity.Error);
                return new CogOperationResult(false, "NO_APP_ENTRIES", true, true);
            }

            await apps[0].LaunchAsync().AsTask(cancellationToken).ConfigureAwait(false);

            ReboundLogger.WriteToLog("Process Launch Cog", $"Successfully launched package: {LaunchTarget.Target}");
            return new CogOperationResult(true, null, true, true);
        }
        catch (OperationCanceledException)
        {
            return new CogOperationResult(false, "OPERATION_CANCELLED", false, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("Process Launch Cog", $"Failed to launch package: {LaunchTarget.Target}", LogMessageSeverity.Error, ex);
            return new CogOperationResult(false, "PACKAGE_LAUNCH_FAILED", true, true);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (LaunchOn is ProcessLaunchOn.Apply or ProcessLaunchOn.Both)
            return await LaunchProcess(cancellationToken).ConfigureAwait(false);

        return new CogOperationResult(true, null, true, true);
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        if (LaunchOn is ProcessLaunchOn.Remove or ProcessLaunchOn.Both)
            return await LaunchProcess(cancellationToken).ConfigureAwait(false);

        return new CogOperationResult(true, null, true, true);
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
        => Task.FromResult(new CogStatus(CogState.Ignorable));

    const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    const uint SEE_MASK_FLAG_NO_UI = 0x00000400;

    private static unsafe bool TryShellExecute(string path, bool elevate)
    {
        using ManagedPtr<char> pathPtr = path;
        using ManagedPtr<char> runasPtr = "runas";

        var sei = new SHELLEXECUTEINFOW
        {
            cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
            lpFile = pathPtr,
            lpVerb = elevate ? runasPtr : (char*)null,
            nShow = SW.SW_SHOWNORMAL,
            fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_FLAG_NO_UI
        };

        if (!TerraFX.Interop.Windows.Windows.ShellExecuteExW(&sei))
        {
            LogShellError(Marshal.GetLastWin32Error(), elevate);
            return false;
        }

        if (sei.hProcess != HANDLE.NULL)
            TerraFX.Interop.Windows.Windows.CloseHandle(sei.hProcess);

        ReboundLogger.WriteToLog("Process Launch Cog", $"Launch successful ({(elevate ? "elevated" : "normal")}).");
        return true;
    }

    private static void LogShellError(int error, bool elevate)
    {
        string reason = error switch
        {
            2 => "File not found",
            5 => "Access denied",
            740 => "Elevation required",
            1155 => "Executable format invalid or blocked",
            1223 => "User cancelled UAC",
            _ => $"Win32 error {error}"
        };

        ReboundLogger.WriteToLog("Process Launch Cog", $"ShellExecute ({(elevate ? "elevated" : "normal")}) failed: {reason}", LogMessageSeverity.Warning);
    }
}