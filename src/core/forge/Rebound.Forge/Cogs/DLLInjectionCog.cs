// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.DLLInjection.Structure;
using Rebound.Core.Storage;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

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
    public string CogDescription
    {
        get
        {
            var targets = TargetProcesses.Count == 1 && TargetProcesses[0] == "*"
                ? "All processes"
                : string.Join(", ", TargetProcesses);

            return $"Register DLL {Path.GetFileName(DLLPath)} for injection into: {targets}";
        }
    }

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
    /// The processes into which the DLL will not be injected.
    /// </summary>
    /// <remarks>
    /// Using <see cref="List{T}"/> intentionally here for serialization compatibility with
    /// <see cref="DllMeta"/>. Callers should treat this as append-only after construction.
    /// </remarks>
#pragma warning disable CA1002, CA2227
    public required List<string> ExcludedProcesses { get; set; }
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

    private string InstalledDllPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileName(DLLPath));

    private string MetaPath => Path.Combine(Variables.ReboundInjectedDLLsFolder, Path.GetFileNameWithoutExtension(DLLPath) + ".dllmeta");

    /// <inheritdoc/>
    /// <remarks>
    /// Copies the DLL for the current process architecture into the protected
    /// injected DLLs directory and writes its target-process metadata.
    /// </remarks>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "DLLInjectionCog Apply",
                $"Applying DLL injection for {DLLPath} targeting: {string.Join(", ", TargetProcesses)}.");

            // Ensure the destination folder exists and is properly protected.
            EnsureProtectedDirectory(Variables.ReboundInjectedDLLsFolder);

            // Copy the DLL first. If anything after this fails, we clean up so we don't
            // leave an orphaned DLL on disk with no metadata to go with it.
            FileEx.Copy(DLLPath, InstalledDllPath);

            string metaContent;

            try
            {
                // Build and serialize the metadata file
                var meta = new DllMeta(TargetProcesses, ExcludedProcesses);
                metaContent = meta.Serialize();
            }
            catch (Exception ex)
            {
                // Serialization failed, now we have to clean up the DLL we just copied so we don't
                // leave a file on disk that GetStatusAsync would misidentify but is still present.
                ReboundLogger.WriteToLog(
                    "DLLInjectionCog Apply",
                    $"Failed to serialize metadata for {InstalledDllPath}. Rolling back copied DLL.",
                    LogMessageSeverity.Error,
                    ex);

                TryDeleteFile(InstalledDllPath);
                return new(false, "META_FAILED", false);
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
                $"DLL injection applied. DLL: {InstalledDllPath}, Meta: {MetaPath}.");

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
        // Validate the .dllmeta file before touching anything.
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
    /// Creates a directory at the specified path and sets access permissions to allow only the local Administrators
    /// group and the Local System account full control.
    /// </summary>
    /// <remarks>Existing permissions are replaced and inheritance is disabled.</remarks>
    /// <param name="path">The path of the directory to create and protect.</param>
    private static void EnsureProtectedDirectory(string path)
    {
        Directory.CreateDirectory(path);

        var administrators = new SecurityIdentifier(
            WellKnownSidType.BuiltinAdministratorsSid, null);

        var system = new SecurityIdentifier(
            WellKnownSidType.LocalSystemSid, null);

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(
            isProtected: true,
            preserveInheritance: false);

        security.SetOwner(administrators);

        const FileSystemRights rights = FileSystemRights.FullControl;
        const InheritanceFlags inheritance =
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        security.AddAccessRule(new FileSystemAccessRule(
            administrators,
            rights,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        security.AddAccessRule(new FileSystemAccessRule(
            system,
            rights,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));

        new DirectoryInfo(path).SetAccessControl(security);
    }

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