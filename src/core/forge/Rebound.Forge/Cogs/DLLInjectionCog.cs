// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.DLLInjection.COM;
using Rebound.Core.DLLInjection.Structure;
using Rebound.Core.Native.Helpers;
using Rebound.Core.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using TerraFX.Interop.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Event args fired when <see cref="DLLInjectionCog"/> needs the UI to confirm
/// killing one or more processes that still hold the DLL open after uninject.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="DllProcessKillConfirmationEventArgs"/>
/// with the list of processes currently blocking DLL removal.
/// </remarks>
/// <param name="blockingProcesses">
/// The processes that still have the DLL loaded or locked.
/// </param>
public sealed class DllProcessKillConfirmationEventArgs(IReadOnlyList<Process> blockingProcesses) : EventArgs
{
    /// <summary>
    /// The processes that still have the DLL loaded or locked.
    /// </summary>
    public IReadOnlyList<Process> BlockingProcesses { get; } = blockingProcesses;

    /// <summary>
    /// Complete this source with <see langword="true"/> to proceed with killing,
    /// or <see langword="false"/> to abort.
    /// </summary>
    public TaskCompletionSource<bool> DecisionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Registers a DLL for injection and handles uninject and cleanup on removal.
/// </summary>
/// <remarks>
/// This cog is security and stability critical. It always requires elevation regardless of caller configuration,
/// as DLL injection and removal directly affect the integrity of running processes. Every processis done
/// with edge case scenarios in mind.
/// </remarks>
public class DLLInjectionCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Register DLL {Path.GetFileName(DLLPath)} for injection into: {(TargetProcesses.Count > 0 ? string.Join(", ", TargetProcesses) : "All processes")}"; }

    /// <summary>
    /// DLL injection is always a privileged operation as the injector will always poll for DLLs and inject them on the go.
    /// This cannot be overridden by callers.
    /// </summary>
    public bool RequiresElevation => true;

    /// <summary>
    /// The processes into which the DLL will be injected.
    /// </summary>
    /// <remarks>
    /// Using <see cref="List{T}"/> intentionally here for serialization compatibility with
    /// <see cref="DllMeta"/>. Callers should treat this as append-only after construction.
    /// </remarks>
#pragma warning disable CA1002, CA2227
    public required List<string> TargetProcesses { get; set; }
#pragma warning restore CA1002, CA2227

    /// <summary>
    /// The path to the source DLL for x64 CPUs before it is copied to the injected DLLs folder.
    /// </summary>
    public required string DLLPathx64 { get; set; }

    /// <summary>
    /// The path to the source DLL for ARM64 CPUs before it is copied to the injected DLLs folder.
    /// </summary>
    public required string DLLPathARM64 { get; set; }

    /// <summary>
    /// Resolves the DLL path for the current architecture. 
    /// This is the path that will be copied to the injected DLLs folder and injected into target processes.
    /// </summary>
    private string DLLPath => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => DLLPathx64,
        Architecture.Arm64 => DLLPathARM64,
        _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
    };

    /// <summary>
    /// Raised when the cog needs the UI to confirm killing processes that still hold
    /// the DLL locked after all uninject attempts have completed.
    /// If no handler is subscribed, the kill step is skipped and file deletion is attempted anyway.
    /// </summary>
    public event EventHandler<DllProcessKillConfirmationEventArgs>? OnKillConfirmationRequested;

    private string InstalledDllPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileName(DLLPath));

    private string MetaPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileNameWithoutExtension(DLLPath) + ".dllmeta");

    /// <inheritdoc/>
    /// <remarks>
    /// This function will only copy the DLL that corresponds to the current architecture (x64 or ARM64) 
    /// to the injected DLLs folder and create a metadata file with the target processes and DLL hash.
    /// </remarks>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Applying DLL injection for {DLLPath} targeting: {string.Join(", ", TargetProcesses)}.");

            // Copy the DLL first. If anything after this fails, we clean up so we don't
            // leave an orphaned DLL on disk with no metadata to go with it.
            FileEx.Copy(DLLPath, InstalledDllPath);

            string hash;
            string metaContent;

            try
            {
                // Compute the SHA-256 hash of the installed DLL for later integrity verification.
                // We hash the destination copy, not the source, so what we store actually reflects what's on disk.
                hash = await ComputeSha256Async(InstalledDllPath, cancellationToken).ConfigureAwait(false);

                // Build and serialize the metadata file
                var meta = new DllMeta(TargetProcesses, hash);
                metaContent = meta.Serialize();
            }
            catch (Exception ex)
            {
                // Hash or serialization failed, now we have to clean up the DLL we just copied so we don't
                // leave a file on disk that GetStatusAsync would misidentify but is still present.
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Apply",
                    $"Failed to compute hash or serialize metadata for {InstalledDllPath}. Rolling back copied DLL.",
                    LogMessageSeverity.Error,
                    ex);

                TryDeleteFile(InstalledDllPath);
                return new(false, "HASH_OR_META_FAILED", false);
            }

            try
            {
                // Attempt to write metadata to disk
                await File.WriteAllTextAsync(MetaPath, metaContent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If this fails, we must roll back the DLL copy so the two files stay in sync.
                // A DLL without metadata is an inconsistent state we never want to leave behind
                // for security and especially stability reasons.
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Apply",
                    $"Failed to write metadata to {MetaPath}. Rolling back copied DLL.",
                    LogMessageSeverity.Error,
                    ex);

                TryDeleteFile(InstalledDllPath);
                return new(false, "META_WRITE_FAILED", false);
            }

            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"DLL injection applied. DLL: {InstalledDllPath}, Hash: {hash}, Meta: {MetaPath}.");

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Failed to apply DLL injection for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        // Validate the .dllmeta file and verify the DLL hash before touching anything.
        // Failures here are soft warning, we still proceed with removal because the
        // goal is to clean up regardless, but we flag it for logging if any serious
        // issue ever appears in the future since it's a modding framework still.
        var integrityOk = await CheckIntegrityAsync(cancellationToken).ConfigureAwait(false);
        if (!integrityOk)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Remove",
                $"Integrity check failed for {InstalledDllPath}. Proceeding with removal - this may indicate tampering.",
                LogMessageSeverity.Warning);
        }

        // For each target process name, find running instances and attempt to
        // send an uninject message via the COM server registered in the ROT.
        var meta = await TryReadDllMetaFileAsync(cancellationToken).ConfigureAwait(false);
        var targetProcessNames = meta?.TargetProcesses ?? TargetProcesses;

        var uninjectResults = await UninjectFromRunningProcessesAsync(
            targetProcessNames,
            DLLPath,
            cancellationToken).ConfigureAwait(false);

        foreach (var (pid, processName, hr) in uninjectResults)
        {
            if (hr == TerraFX.Interop.Windows.S.S_OK)
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Remove",
                    $"Successfully uninjected from {processName} (PID {pid}).");
            }
            else
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Remove",
                    $"Uninject returned 0x{hr:X8} for {processName} (PID {pid}). Will verify DLL lock state.",
                    LogMessageSeverity.Warning);
            }
        }

        // Even if uninject reported success, verify the DLL is no longer locked on disk.
        // If it still is, identify the blocking processes and ask the UI for permission
        // to terminate them. If no handler is subscribed, we skip the kill step and
        // let the retry logic determine whether deletion is possible.
        if (File.Exists(InstalledDllPath))
        {
            var blockingProcesses = FindProcessesWithDllLoaded(InstalledDllPath);

            if (blockingProcesses.Count > 0)
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Remove",
                    $"DLL still held by {blockingProcesses.Count} process(es): {string.Join(", ", blockingProcesses.Select(p => $"{p.ProcessName} (PID {p.Id})"))}.",
                    LogMessageSeverity.Warning);

                if (OnKillConfirmationRequested is not null)
                {
                    var args = new DllProcessKillConfirmationEventArgs(blockingProcesses);
                    OnKillConfirmationRequested.Invoke(this, args);

                    bool confirmed;
                    try
                    {
                        confirmed = await args.DecisionSource.Task
                            .WaitAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        ReboundLogger.WriteToLog(
                            "DLLInjectionCog Remove",
                            "Removal cancelled while waiting for kill confirmation.",
                            LogMessageSeverity.Warning);
                        return new(false, "OPERATION_CANCELLED", false);
                    }

                    if (confirmed)
                    {
                        foreach (var proc in blockingProcesses)
                        {
                            try
                            {
                                ReboundLogger.WriteToLog(
                                    "DLLInjectionCog Remove",
                                    $"Killing blocking process {proc.ProcessName} (PID {proc.Id}).");

                                proc.Kill();
                            }
                            catch (Exception ex)
                            {
                                // Don't fail hard here, continue trying the remaining processes.
                                // The retry logic will tell us if the lock is still held when we attempt deletion.
                                ReboundLogger.WriteToLog(
                                    "DLLInjectionCog Remove",
                                    $"Failed to kill {proc.ProcessName} (PID {proc.Id}).",
                                    LogMessageSeverity.Error,
                                    ex);
                            }
                        }
                    }
                    else
                    {
                        ReboundLogger.WriteToLog(
                            "DLLInjectionCog Remove",
                            "User declined process kill. Proceeding with file deletion attempt regardless.",
                            LogMessageSeverity.Warning);
                    }
                }
                else
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog Remove",
                        "No kill confirmation handler is subscribed. Skipping process termination and proceeding to file deletion.",
                        LogMessageSeverity.Warning);
                }
            }
        }

        // The file deletion itself
        try
        {
            if (File.Exists(InstalledDllPath))
            {
                await DeleteFileWithRetryAsync(InstalledDllPath, cancellationToken).ConfigureAwait(false);
                ReboundLogger.WriteToLog("DLLInjectionCog", $"Deleted DLL at {InstalledDllPath}.");
            }

            if (File.Exists(MetaPath))
            {
                await DeleteFileWithRetryAsync(MetaPath, cancellationToken).ConfigureAwait(false);
                ReboundLogger.WriteToLog("DLLInjectionCog", $"Deleted metadata at {MetaPath}.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog",
                $"Failed to delete DLL or metadata for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);

            return new(false, "FILE_DELETE_FAILED", false);
        }

        ReboundLogger.WriteToLog("DLLInjectionCog", $"DLL injection removed successfully for {DLLPath}.");
        return new(true, null, true);
    }

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        var exists = File.Exists(InstalledDllPath);
        var isIntact = await CheckIntegrityAsync().ConfigureAwait(false);

        ReboundLogger.WriteToLog(
            "DLLInjectionCog GetStatus",
            $"Status check for {InstalledDllPath}: exists={exists}, intact={isIntact}.");

        return new(exists && isIntact ? CogState.Installed : CogState.NotInstalled);
    }

    #region Private helpers

    /// <summary>
    /// Performs integrity checks on the installed DLL and its metadata file.
    /// Returns <see langword="false"/> if any check fails, but never throws.
    /// </summary>
    private async Task<bool> CheckIntegrityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // First check if the required files exist on disk
            if (!File.Exists(InstalledDllPath))
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog CheckIntegrity",
                    $"DLL not found at {InstalledDllPath}.",
                    LogMessageSeverity.Warning);
                return false;
            }

            if (!File.Exists(MetaPath))
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog CheckIntegrity",
                    $"Metadata file not found at {MetaPath}.",
                    LogMessageSeverity.Warning);
                return false;
            }

            var content = await File.ReadAllTextAsync(MetaPath, cancellationToken).ConfigureAwait(false);
            var meta = DllMeta.TryParse(content);

            // Check the meta file's integrity
            if (meta is null)
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog CheckIntegrity",
                    $"Metadata file at {MetaPath} is malformed.",
                    LogMessageSeverity.Warning);
                return false;
            }

            // Now check the hash if the meta file is intact, this must match for security reasons.
            if (meta.DllHash is not null)
            {
                // Both the stored hash and ComputeSha256Async produce uppercase hex,
                // so OrdinalIgnoreCase is just to make Visual Studio shut up for once.
                var currentHash = await ComputeSha256Async(InstalledDllPath, cancellationToken).ConfigureAwait(false);

                if (!string.Equals(currentHash, meta.DllHash, StringComparison.OrdinalIgnoreCase))
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog CheckIntegrity",
                        $"Hash mismatch for {InstalledDllPath}. Expected: {meta.DllHash}, Actual: {currentHash}. Possible tampering.",
                        LogMessageSeverity.Warning);
                    return false;
                }
            }
            else
            {
                // No hash in the metadata, this file was likely tampered with.
                // Cannot proceed for security reasons.
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog CheckIntegrity",
                    $"Metadata for {InstalledDllPath} has no DLL_HASH entry (possible tampering).",
                    LogMessageSeverity.Warning);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog CheckIntegrity",
                $"Integrity check threw an exception for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to read and parse the .dllmeta file. Returns <see langword="null"/> on any failure.
    /// </summary>
    private async Task<DllMeta?> TryReadDllMetaFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(MetaPath))
                return null;

            var content = await File.ReadAllTextAsync(
                MetaPath, 
                cancellationToken).ConfigureAwait(false);

            return DllMeta.TryParse(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// For each target process name, finds all running instances and attempts
    /// COM uninject via the ROT. Returns one result tuple per process instance found.
    /// </summary>
    private static async Task<List<(uint Pid, string ProcessName, int Hr)>> UninjectFromRunningProcessesAsync(
        IEnumerable<string> targetProcessNames,
        string dllPath,
        CancellationToken cancellationToken)
    {
        // Make the list of forbidden tuples
        var results = new List<(uint Pid, string ProcessName, int Hr)>();

        foreach (var processName in targetProcessNames)
        {
            // ThrowIfCancellationRequested on the outer loop so we stop processing
            // further process names entirely, not just the inner instances loop.
            cancellationToken.ThrowIfCancellationRequested();

            // Process.GetProcessesByName doesn't want the .exe suffix.
            // Strip it if the caller included it
            var nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            var instances = Process.GetProcessesByName(nameWithoutExt);

            foreach (var proc in instances)
            {
                // Once again, throw on cancellation requested.
                cancellationToken.ThrowIfCancellationRequested();

                uint pid = (uint)proc.Id;
                string dllStem = Path.GetFileNameWithoutExtension(dllPath);

                ReboundLogger.WriteToLog(
                    "DLLInjectionCog UninjectFromRunningProcesses",
                    $"Attempting COM uninject for {processName} (PID {pid}).");

                // The COM(TM)
                using var proxy = ReboundInjectionROT.TryGetForPid(pid, dllStem);

                if (proxy is null)
                {
                    // No ROT entry for this PID. The DLL may not be injected or may have already unloaded.
                    // Treat as soft success rather than an error so we don't block the rest of removal.
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog UninjectFromRunningProcesses",
                        $"No IReboundInjectionServer found in ROT for PID {pid}. DLL may not be injected or already unloaded.",
                        LogMessageSeverity.Warning);

                    results.Add((pid, processName, TerraFX.Interop.Windows.S.S_OK));
                    continue;
                }

                // Verify the server's self-reported PID matches; guards against stale ROT entries
                // where a previous process died and a new one happened to reuse the same moniker slot
                // (probably rare but can happen because it's Windows).
                int serverPid;
                try
                {
                    serverPid = (int)proxy.GetHostPid();
                }
                catch (Exception ex)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog UninjectFromRunningProcesses",
                        $"GetHostPid threw for PID {pid}. Skipping.",
                        LogMessageSeverity.Warning,
                        ex);

                    results.Add((pid, processName, TerraFX.Interop.Windows.E.E_FAIL));
                    continue;
                }

                if (serverPid != pid)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog UninjectFromRunningProcesses",
                        $"PID mismatch for ROT entry: expected {pid}, server reports {serverPid}. Stale entry — skipping.",
                        LogMessageSeverity.Warning);

                    results.Add((pid, processName, TerraFX.Interop.Windows.E.E_FAIL));
                    continue;
                }

                int hr;
                try
                {
                    hr = proxy.Uninject();
                }
                catch (Exception ex)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog UninjectFromRunningProcesses",
                        $"Uninject threw for {processName} (PID {pid}).",
                        LogMessageSeverity.Error,
                        ex);

                    results.Add((pid, processName, TerraFX.Interop.Windows.E.E_FAIL));
                    continue;
                }

                // E_PENDING means the DLL acknowledged the uninject request but hasn't finished
                // unwinding yet. Poll IsIdle() until it's done or we time out.
                // Since this is DLL injection, there's always a chance that the process hangs
                // doing nothing specific, so we just have to wait in order to maintain
                // the stability factor.
                if (hr == TerraFX.Interop.Windows.E.E_PENDING)
                    hr = await PollUntilIdleAsync(proxy, cancellationToken).ConfigureAwait(false);

                results.Add((pid, processName, hr));
            }
        }

        return results;
    }

    /// <summary>
    /// Polls <see cref="ReboundInjectionServerProxy.IsIdle"/> every 200 ms for up to
    /// <paramref name="timeoutMs"/> milliseconds. Returns <c>S_OK</c> if idle, <c>E_FAIL</c> on timeout.
    /// </summary>
    private static async Task<int> PollUntilIdleAsync(
        ReboundInjectionServerProxy proxy,
        CancellationToken cancellationToken,
        int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (cancellationToken.IsCancellationRequested)
                return TerraFX.Interop.Windows.E.E_FAIL;

            bool idle;
            try
            {
                idle = proxy.IsIdle();
            }
            catch (Exception ex)
            {
                // If IsIdle throws, the COM server is probably gone. Treat as idle so we move on.
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog PollUntilIdle",
                    "IsIdle threw during idle poll. Assuming DLL has unloaded.",
                    LogMessageSeverity.Warning,
                    ex);

                return TerraFX.Interop.Windows.S.S_OK;
            }

            if (idle)
                return TerraFX.Interop.Windows.S.S_OK;

            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }

        ReboundLogger.WriteToLog(
            "DLLInjectionCog PollUntilIdle",
            "Timed out waiting for DLL to become idle after uninject.",
            LogMessageSeverity.Warning);

        return TerraFX.Interop.Windows.E.E_FAIL;
    }

    /// <summary>
    /// Finds all processes that currently have the given DLL file locked.
    /// Uses a try-exclusive-open fast path first, then falls back to Restart Manager,
    /// then to module enumeration if Restart Manager is unavailable.
    /// </summary>
    private static List<Process> FindProcessesWithDllLoaded(string dllPath)
    {
        // Fast path: if we can open the file exclusively, no one has it locked
        try
        {
            using var fs = File.Open(dllPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return[];
        }
        catch (IOException)
        {
            // File is locked; fall through to find who holds it
        }
        catch
        {
            // Access denied or other unexpected error; treat as unlocked and let deletion fail naturally
            return[];
        }

        var lockers = new List<Process>();

        try
        {
            lockers.AddRange(RestartManagerHelper.GetLockingProcesses(dllPath));
        }
        catch (Exception ex)
        {
            // Restart Manager failed - fall back to walking all processes and checking loaded modules.
            // This is slower and noisier but covers cases where RstrtMgr is unavailable.
            ReboundLogger.WriteToLog(
                "DLLInjectionCog FindProcessesWithDllLoaded",
                "Restart Manager query failed. Falling back to module enumeration.",
                LogMessageSeverity.Warning,
                ex);

            string dllName = Path.GetFileName(dllPath);
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    foreach (ProcessModule module in proc.Modules)
                    {
                        if (string.Equals(module.ModuleName, dllName, StringComparison.OrdinalIgnoreCase))
                        {
                            lockers.Add(proc);
                            break;
                        }
                    }
                }
                catch
                {
                    // Access denied or process exited mid-enumeration - skip
                }
            }
        }

        return lockers;
    }

    /// <summary>
    /// Deletes a file with retry logic, polling every 200 ms for up to 10 seconds.
    /// Throws if the file still cannot be deleted after all retries are exhausted.
    /// </summary>
    private static async Task DeleteFileWithRetryAsync(string filePath, CancellationToken cancellationToken)
    {
        const int maxRetries = 50; // 50 × 200 ms = 10 seconds

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                File.Delete(filePath);
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog DeleteFileWithRetry",
                        $"Failed to delete {filePath} after {maxRetries} attempts.",
                        LogMessageSeverity.Error,
                        ex);
                    throw;
                }

                ReboundLogger.WriteToLog(
                    "DLLInjectionCog DeleteFileWithRetry",
                    $"Delete attempt {attempt}/{maxRetries} failed for {filePath}. Retrying in 200 ms.");

                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Computes the SHA-256 hash of a file and returns it as an uppercase hex string.
    /// </summary>
    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes); // ToHexString already returns uppercase
    }

    /// <summary>
    /// Attempts to delete a file without throwing. Used for rollback paths where
    /// a best-effort cleanup is preferable to masking the original error (aka laziness).
    /// </summary>
    private static void TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog TryDeleteFile",
                $"Rollback file deletion failed for {filePath}.",
                LogMessageSeverity.Warning,
                ex);
        }
    }

    #endregion
}