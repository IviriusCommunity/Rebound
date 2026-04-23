// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.DLLInjection.COM;
using Rebound.Core.DLLInjection.Structure;
using Rebound.Core.Native.Helpers;
using Rebound.Core.Storage;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Event args fired when <see cref="DLLInjectionCog"/> needs the UI to confirm
/// killing one or more processes that still hold the DLL open after uninject.
/// </summary>
public sealed class DllProcessKillConfirmationEventArgs : EventArgs
{
    /// <summary>The processes that still have the DLL loaded/locked.</summary>
    public IReadOnlyList<Process> BlockingProcesses { get; }

    /// <summary>
    /// Complete this source with <c>true</c> to proceed with killing,
    /// <c>false</c> to abort.
    /// </summary>
    public TaskCompletionSource<bool> DecisionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DllProcessKillConfirmationEventArgs(IReadOnlyList<Process> blockingProcesses)
        => BlockingProcesses = blockingProcesses;
}

/// <summary>
/// Registers a DLL for injection and handles graceful uninject + cleanup on removal.
/// </summary>
public class DLLInjectionCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

    /// <summary>The processes in which the DLL will be injected.</summary>
#pragma warning disable CA1002, CA2227
    public required List<string> TargetProcesses { get; set; }
#pragma warning restore CA1002, CA2227

    /// <summary>The path to the source DLL (before copying to the injected DLLs folder).</summary>
    public required string DLLPath { get; set; }

    /// <inheritdoc/>
    public string TaskDescription => $"Register {DLLPath} for injection";

    /// <summary>
    /// Raised when the cog needs the UI to confirm killing processes that still hold
    /// the DLL locked after uninject attempts have completed.
    /// </summary>
    public event EventHandler<DllProcessKillConfirmationEventArgs>? OnKillConfirmationRequested;

    // ─── Paths ───────────────────────────────────────────────────────────────

    private string InstalledDllPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileName(DLLPath));
    private string MetaPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileNameWithoutExtension(DLLPath) + ".dllmeta");

    // ─── Apply ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Applying DLL injection for {DLLPath} targeting processes: {string.Join(", ", TargetProcesses)}");

            // Copy the DLL to the injected DLLs folder
            FileEx.Copy(DLLPath, InstalledDllPath);

            // Compute the SHA-256 hash of the installed DLL for later integrity verification
            string hash = await ComputeSha256Async(InstalledDllPath, cancellationToken).ConfigureAwait(false);

            // Write the metadata file (v2 format: processes + hash)
            var meta = new DllMeta(TargetProcesses, hash);
            await File.WriteAllTextAsync(MetaPath, meta.Serialize(), cancellationToken).ConfigureAwait(false);

            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Successfully applied DLL injection. DLL: {InstalledDllPath}, Hash: {hash}, Meta: {MetaPath}");

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Failed to apply DLL injection for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);
            return new(false, $"Failed to apply DLL injection for {DLLPath}.", false);
        }
    }

    // ─── Remove ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        // ── Phase 0: Integrity check ─────────────────────────────────────────
        //
        // Parse and validate the .dllmeta file and verify the DLL hash.
        // Failures here are soft warnings — we still proceed with removal.

        var integrityOk = await CheckIntegrityAsync(cancellationToken).ConfigureAwait(false);
        if (!integrityOk)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Remove",
                $"Integrity check failed for {InstalledDllPath}. Proceeding with removal anyway — this may indicate tampering.",
                LogMessageSeverity.Warning);
        }

        // ── Phase 1: COM uninject ────────────────────────────────────────────
        //
        // For each target process name, find running instances and attempt to
        // send an uninject message via the COM server registered in the ROT.

        var meta = await TryReadMetaAsync(cancellationToken).ConfigureAwait(false);
        var targetProcessNames = meta?.TargetProcesses ?? TargetProcesses;

        var uninjectResults = await UninjectFromRunningProcessesAsync(
            targetProcessNames,
            DLLPath,
            cancellationToken).ConfigureAwait(false);

        // Log per-process uninject results
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
                    $"Uninject returned 0x{hr:X8} for {processName} (PID {pid}). Will check if DLL is still locked.",
                    LogMessageSeverity.Warning);
            }
        }

        // ── Phase 2: Locking check + optional process kill ───────────────────
        //
        // Even if uninject succeeded, verify the DLL file is no longer locked.
        // If it still is, identify the blocking processes and ask the UI for
        // permission to terminate them.

        if (File.Exists(InstalledDllPath))
        {
            var blockingProcesses = FindProcessesWithDllLoaded(InstalledDllPath);

            if (blockingProcesses.Count > 0)
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Remove",
                    $"DLL is still held by {blockingProcesses.Count} process(es): {string.Join(", ", blockingProcesses.Select(p => $"{p.ProcessName} ({p.Id})"))}. Requesting UI confirmation to terminate.",
                    LogMessageSeverity.Warning);

                var args = new DllProcessKillConfirmationEventArgs(blockingProcesses);
                OnKillConfirmationRequested?.Invoke(this, args);

                bool confirmed;
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
                            ReboundLogger.WriteToLog(
                                "DLLInjectionCog Remove",
                                $"Failed to kill {proc.ProcessName} (PID {proc.Id}).",
                                LogMessageSeverity.Error,
                                ex);

                            // Don't hard-fail — continue trying other processes and
                            // let file deletion below tell us if the lock is still held.
                        }
                    }
                }
                else
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog Remove",
                        "User aborted process kill. Proceeding with file deletion attempt anyway.",
                        LogMessageSeverity.Warning);
                }
            }
        }

        // ── Phase 3: File deletion ───────────────────────────────────────────

        try
        {
            if (File.Exists(InstalledDllPath))
            {
                await DeleteFileWithRetryAsync(InstalledDllPath, cancellationToken).ConfigureAwait(false);
                ReboundLogger.WriteToLog("DLLInjectionCog Remove", $"Deleted DLL at {InstalledDllPath}.");
            }

            if (File.Exists(MetaPath))
            {
                await DeleteFileWithRetryAsync(MetaPath, cancellationToken).ConfigureAwait(false);
                ReboundLogger.WriteToLog("DLLInjectionCog Remove", $"Deleted metadata at {MetaPath}.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Remove",
                $"Failed to delete DLL or metadata files for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);

            return new CogOperationResult(false, "FILE_DELETE_FAILED", false);
        }

        return new CogOperationResult(true, null, true);
    }

    // ─── GetStatus ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<CogStatus> GetStatusAsync()
    {
        bool exists = File.Exists(InstalledDllPath);
        bool isIntact = await CheckIntegrityAsync(CancellationToken.None).ConfigureAwait(false);
        ReboundLogger.WriteToLog("DLLInjectionCog GetStatus", $"IsApplied: {InstalledDllPath} exists? {exists}");
        return new CogStatus(exists && isIntact ? CogState.Installed : CogState.NotInstalled);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Performs integrity checks on the installed DLL and its metadata file.
    /// Returns false if any check fails, but never throws.
    /// </summary>
    private async Task<bool> CheckIntegrityAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check DLL exists
            if (!File.Exists(InstalledDllPath))
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Integrity",
                    $"DLL not found at {InstalledDllPath}.",
                    LogMessageSeverity.Warning);
                return false;
            }

            // Check meta exists and is parseable
            if (!File.Exists(MetaPath))
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Integrity",
                    $"Metadata file not found at {MetaPath}.",
                    LogMessageSeverity.Warning);
                return false;
            }

            var content = await File.ReadAllTextAsync(MetaPath, cancellationToken).ConfigureAwait(false);
            var meta = DllMeta.TryParse(content);

            if (meta is null)
            {
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Integrity",
                    $"Metadata file at {MetaPath} is malformed.",
                    LogMessageSeverity.Warning);
                return false;
            }

            // Verify hash if present
            if (meta.DllHash is not null)
            {
                var currentHash = await ComputeSha256Async(InstalledDllPath, cancellationToken).ConfigureAwait(false);

                if (!string.Equals(currentHash, meta.DllHash, StringComparison.OrdinalIgnoreCase))
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog Integrity",
                        $"Hash mismatch for {InstalledDllPath}. Expected: {meta.DllHash}, Actual: {currentHash}. Possible tampering.",
                        LogMessageSeverity.Warning);
                    return false;
                }
            }
            else
            {
                // Hash absent — legacy meta file, warn but don't fail hard
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Integrity",
                    $"Metadata for {DLLPath} has no DLL_HASH entry (legacy format). Skipping hash verification.",
                    LogMessageSeverity.Warning);
            }

            return true;
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Integrity",
                $"Integrity check threw an exception for {DLLPath}.",
                LogMessageSeverity.Error,
                ex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to read and parse the .dllmeta file. Returns null on any failure.
    /// </summary>
    private async Task<DllMeta?> TryReadMetaAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(MetaPath))
                return null;

            var content = await File.ReadAllTextAsync(MetaPath, cancellationToken).ConfigureAwait(false);
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
        var results = new List<(uint Pid, string ProcessName, int Hr)>();

        foreach (var processName in targetProcessNames)
        {
            // Strip .exe suffix if caller included it — Process.GetProcessesByName doesn't want it
            var nameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            var instances = Process.GetProcessesByName(nameWithoutExt);

            foreach (var proc in instances)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                uint pid = (uint)proc.Id;

                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Uninject",
                    $"Attempting COM uninject for {processName} (PID {pid}).");

                string dllStem = Path.GetFileNameWithoutExtension(dllPath);
                using var proxy = ReboundInjectionROT.TryGetForPid(pid, dllStem);

                if (proxy is null)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog Uninject",
                        $"No IReboundInjectionServer found in ROT for PID {pid}. DLL may not be injected or already unloaded.",
                        LogMessageSeverity.Warning);

                    // Treat as soft success — DLL might already be gone
                    results.Add((pid, processName, TerraFX.Interop.Windows.S.S_OK));
                    continue;
                }

                // Verify the server's self-reported PID matches — guards against stale ROT entries
                var serverPid = proxy.GetHostPid();
                if (serverPid != pid)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog Uninject",
                        $"PID mismatch for ROT entry: expected {pid}, server reports {serverPid}. Skipping.",
                        LogMessageSeverity.Warning);

                    results.Add((pid, processName, TerraFX.Interop.Windows.E.E_FAIL));
                    continue;
                }

                int hr = proxy.Uninject();

                // If E_PENDING, poll IsIdle() up to 5 seconds before giving up
                if (hr == TerraFX.Interop.Windows.E.E_PENDING)
                {
                    hr = await PollUntilIdleAsync(proxy, cancellationToken).ConfigureAwait(false);
                }

                results.Add((pid, processName, hr));
            }
        }

        return results;
    }

    /// <summary>
    /// Polls <see cref="ReboundInjectionServerProxy.IsIdle"/> every 200ms for up to
    /// <paramref name="timeoutMs"/> milliseconds. Returns S_OK if idle, E_FAIL on timeout.
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

            if (proxy.IsIdle())
                return TerraFX.Interop.Windows.S.S_OK;

            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }

        ReboundLogger.WriteToLog(
            "DLLInjectionCog Uninject",
            "Timed out waiting for DLL to become idle after uninject.",
            LogMessageSeverity.Warning);

        return TerraFX.Interop.Windows.E.E_FAIL;
    }

    /// <summary>
    /// Finds all processes that currently have the given DLL file path loaded.
    /// Uses a try-exclusive-open approach: if the file can be opened with FileShare.None,
    /// it's not locked; otherwise enumerate processes to find who holds it.
    /// 
    /// Note: This uses the Restart Manager API (RstrtMgr) for accurate results on Windows.
    /// Falls back to checking all processes' loaded modules if RstrtMgr is unavailable.
    /// </summary>
    private static List<Process> FindProcessesWithDllLoaded(string dllPath)
    {
        // Quick check: if we can open exclusively, no one has it locked
        try
        {
            using var fs = File.Open(dllPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return []; // File is free
        }
        catch (IOException)
        {
            // File is locked — fall through to find who
        }
        catch
        {
            return []; // Access denied or other — treat as unlocked
        }

        // Use Restart Manager to find locking processes
        var lockers = new List<Process>();

        try
        {
            lockers.AddRange(RestartManagerHelper.GetLockingProcesses(dllPath));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog",
                "Restart Manager query failed; falling back to module enumeration.",
                LogMessageSeverity.Warning,
                ex);

            // Fallback: walk all processes and check their loaded modules
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
                    // Access denied, process exited — skip
                }
            }
        }

        return lockers;
    }

    /// <summary>
    /// Deletes a file with retry logic (200ms intervals, 10 second timeout).
    /// </summary>
    private static async Task DeleteFileWithRetryAsync(string filePath, CancellationToken cancellationToken)
    {
        const int maxRetries = 50; // 50 * 200ms = 10 seconds
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                File.Delete(filePath);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    ReboundLogger.WriteToLog(
                        "DLLInjectionCog",
                        $"Failed to delete {filePath} after {maxRetries} retries.",
                        LogMessageSeverity.Error,
                        ex);
                    throw;
                }

                ReboundLogger.WriteToLog(
                    "DLLInjectionCog",
                    $"Delete failed for {filePath}, retry {retryCount}/{maxRetries}");

                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Computes the SHA-256 hash of a file and returns it as a lowercase hex string.
    /// </summary>
    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes).ToUpperInvariant();
    }

    // ─── Static helpers (public, for external use) ────────────────────────────

    /// <summary>
    /// Queries the target processes list from the metadata file.
    /// </summary>
#pragma warning disable CA1002
    public static List<string> GetTargetProcessesFromMeta(string dllDisplayName)
#pragma warning restore CA1002
    {
        try
        {
            var metaPath = Path.Combine(Variables.ReboundInjectedDLLsFolder, dllDisplayName + ".dllmeta");

            if (!File.Exists(metaPath))
            {
                ReboundLogger.WriteToLog("DLLInjectionCog", $"Metadata file not found: {metaPath}");
                return [];
            }

            var content = File.ReadAllText(metaPath);
            var meta = DllMeta.TryParse(content);

            if (meta is null)
            {
                ReboundLogger.WriteToLog("DLLInjectionCog", $"Invalid metadata file format: {metaPath}");
                return [];
            }

            return [.. meta.TargetProcesses];
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog("DLLInjectionCog", $"Failed to read metadata for {dllDisplayName}", LogMessageSeverity.Error, ex);
            return [];
        }
    }
}