// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using Windows.Management.Deployment;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Specifies when a process should be launched in relation to an operation.
/// </summary>
public enum ProcessLaunchOn
{
    /// <summary>
    /// Launch the process on <see cref="ProcessKillCog.ApplyAsync(CancellationToken)"/>.
    /// </summary>
    Apply,
    /// <summary>
    /// Launch the process on <see cref="ProcessKillCog.RemoveAsync(CancellationToken)"/>.
    /// </summary>
    Remove,
    /// <summary>
    /// Launch the process on both <see cref="ProcessKillCog.ApplyAsync(CancellationToken)"/>
    /// and <see cref="ProcessKillCog.RemoveAsync(CancellationToken)"/>.
    /// </summary>
    Both
}

/// <summary>
/// Specifies whether the target is an executable path or an application package family name.
/// </summary>
public enum ProcessLaunchTargetType
{
    /// <summary>
    /// Target is an executable file.
    /// </summary>
    ExecutablePath,
    /// <summary>
    /// Target is an application package.
    /// </summary>
    PackageFamilyName
}

/// <summary>
/// Represents the launch target of the corresponding process launch operation.
/// </summary>
/// <param name="Target">
/// The target path itself. Can be an executable path or a package family name.
/// </param>
/// <param name="TargetType">
/// The type of the target path.
/// </param>
public record ProcessLaunchTarget(string Target, ProcessLaunchTargetType TargetType);

/// <summary>
/// Represents a cog that launches an executable or a package app.
/// </summary>
public class ProcessLaunchCog : IConfirmationPromptCog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Launch {(LaunchTarget.TargetType == ProcessLaunchTargetType.ExecutablePath ? "executable" : "package")} {LaunchTarget.Target}."; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <summary>
    /// The launch target. Either a full executable path or a package family name.
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
    public event EventHandler<ConfirmationPromptEventArgs>? OnConfirmationRequested;

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

    #region Helpers

    /// <summary>
    /// Generic wrapper function for launching either target.
    /// </summary>
    private async Task<CogOperationResult> LaunchProcess(CancellationToken cancellationToken = default)
    {
        ReboundLogger.WriteToLog(
            "ProcessLaunchCog LaunchProcess", 
            $"Initiating launch of target: {LaunchTarget.Target} (Type: {LaunchTarget.TargetType})");

        // Ask for confirmation if required
        if (RequiresUserConfirmation)
        {
            var args = new ConfirmationPromptEventArgs();
            OnConfirmationRequested?.Invoke(this, args);

            bool confirmed;

            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchProcess",
                "User confirmation required, awaiting response...");

            try
            {
                confirmed = await args.UserResponse.Task
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ReboundLogger.WriteToLog(
                    "ProcessLaunchCog LaunchProcess",
                    "User confirmation cancelled by operation cancellation.",
                    LogMessageSeverity.Warning);
                return new CogOperationResult(false, "OPERATION_CANCELLED", false, true);
            }

            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchProcess",
                $"User confirmation received: {(confirmed ? "confirmed" : "denied")}");

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

    /// <summary>
    /// Launches an executable file.
    /// </summary>
    private Task<CogOperationResult> LaunchExecutable(CancellationToken cancellationToken)
    {
        ReboundLogger.WriteToLog(
            "ProcessLaunchCog LaunchExecutable",
            $"Launching executable: {LaunchTarget.Target}");

        if (cancellationToken.IsCancellationRequested)
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchExecutable",
                "Launch cancelled by operation cancellation.",
                LogMessageSeverity.Warning);
            return Task.FromResult(new CogOperationResult(false, "OPERATION_CANCELLED", false, true));
        }

        // Check if the target is a valid string
        if (string.IsNullOrWhiteSpace(LaunchTarget.Target))
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchExecutable",
                "Invalid target: Target is null or whitespace.",
                LogMessageSeverity.Error);
            return Task.FromResult(new CogOperationResult(false, "INVALID_TARGET", false, true));
        }

        // Check if the target exists
        if (!File.Exists(LaunchTarget.Target))
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchExecutable",
                $"File does not exist: {LaunchTarget.Target}",
                LogMessageSeverity.Error);
            return Task.FromResult(new CogOperationResult(false, "FILE_NOT_FOUND", true, true));
        }

        // Try as admin directly
        if (RequiresElevation && TryShellExecute(LaunchTarget.Target, elevate: true))
            return Task.FromResult(new CogOperationResult(true, null, true, true));

        else
        {
            // Try shell execute as user first
            if (TryShellExecute(LaunchTarget.Target, elevate: false))
                return Task.FromResult(new CogOperationResult(true, null, true, true));

            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchExecutable",
                "Normal launch failed, retrying elevated.",
                LogMessageSeverity.Warning);

            // If it doesn't work, retry as admin
            if (TryShellExecute(LaunchTarget.Target, elevate: true))
                return Task.FromResult(new CogOperationResult(true, null, true, true));
        }

        ReboundLogger.WriteToLog(
            "ProcessLaunchCog LaunchExecutable",
            "All launch attempts failed.",
            LogMessageSeverity.Error);
        return Task.FromResult(new CogOperationResult(false, "LAUNCH_FAILED", true, true));
    }

    /// <summary>
    /// Launches a package by its family name.
    /// </summary>
    private unsafe Task<CogOperationResult> LaunchPackage(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchPackage",
                "Launch cancelled by operation cancellation.",
                LogMessageSeverity.Warning);
            return Task.FromResult(new CogOperationResult(false, "OPERATION_CANCELLED", false, true));
        }

        try
        {
            using ComPtr<IApplicationActivationManager> activationManager = default;
            using ManagedPtr<Guid> clsid = CLSID.CLSID_ApplicationActivationManager;
            using ManagedPtr<Guid> iid = IID.IID_IApplicationActivationManager;

            HRESULT hr = TerraFX.Interop.Windows.Windows.CoCreateInstance(
                clsid,
                null,
                (uint)CLSCTX.CLSCTX_LOCAL_SERVER,
                iid,
                (void**)activationManager.GetAddressOf());

            if (TerraFX.Interop.Windows.Windows.FAILED(hr))
            {
                ReboundLogger.WriteToLog(
                    "ProcessLaunchCog LaunchPackage",
                    $"Failed to create Application Activation Manager instance. HRESULT: 0x{hr:X}",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "ACTIVATION_MANAGER_CREATION_FAILED", true, true));
            }

            using ManagedPtr<char> appUserModelId = LaunchTarget.Target + "!App";
            activationManager.Get()->ActivateApplication(
                appUserModelId, 
                null, 
                (ACTIVATEOPTIONS)0x20000000, 
                null);

            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchPackage",
                $"Package launch successful: {LaunchTarget.Target}");

            return Task.FromResult(new CogOperationResult(true, null, true, true));
        }
        catch (OperationCanceledException)
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchPackage",
                "Launch cancelled by operation cancellation.",
                LogMessageSeverity.Warning);
            return Task.FromResult(new CogOperationResult(false, "OPERATION_CANCELLED", false, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ProcessLaunchCog LaunchPackage",
                $"Exception occurred during package launch: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", true, true));
        }
    }

    #endregion

    #region Native Interop for ShellExecute

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

            ReboundLogger.WriteToLog(
                "ProcessLaunchCog TryShellExecute",
                $"ShellExecute {(elevate ? "elevated" : "normal")} succeeded for path: {path}");
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

        ReboundLogger.WriteToLog(
            "ProcessLaunchCog TryShellExecute",
            $"ShellExecute {(elevate ? "elevated" : "normal")} failed. Reason: {reason}",
            LogMessageSeverity.Error);
    }

    #endregion
}