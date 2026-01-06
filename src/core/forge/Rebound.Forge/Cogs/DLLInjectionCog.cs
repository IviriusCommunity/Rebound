// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Storage;
namespace Rebound.Forge.Cogs;

/// <summary>
/// Registers a DLL for injection.
/// </summary>
public class DLLInjectionCog : ICog
{
    /// <summary>
    /// The processes in which the DLL will be injected.
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    public required List<string> TargetProcesses { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists

    /// <summary>
    /// The path to the DLL.
    /// </summary>
    public required string DLLPath { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription { get => $"Register {DLLPath} for injection"; }

    /// <inheritdoc/>
    public async Task ApplyAsync()
    {
        try
        {
            var targetPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, Path.GetFileName(DLLPath));
            var metaPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, Path.GetFileNameWithoutExtension(DLLPath) + ".dllmeta");
            ReboundLogger.Log("[DLLInjectionCog] Apply started.");
            FileEx.Copy(DLLPath, targetPath);
            ReboundLogger.Log($"[DLLInjectionCog] Copied file from {DLLPath} to {targetPath}.");

            // Create metadata file
            var metaContent = "TARGET_PROCESSES=" + string.Join("\\", TargetProcesses);
            await File.WriteAllTextAsync(metaPath, metaContent).ConfigureAwait(false);
            ReboundLogger.Log($"[DLLInjectionCog] Created metadata file at {metaPath}.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[DLLInjectionCog] Apply failed with exception.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {
        try
        {
            var targetPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, Path.GetFileName(DLLPath));
            var metaPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, Path.GetFileNameWithoutExtension(DLLPath) + ".dllmeta");
            ReboundLogger.Log("[DLLInjectionCog] Remove started.");

            // Delete DLL with retry logic
            if (File.Exists(targetPath))
            {
                await DeleteFileWithRetryAsync(targetPath);
                ReboundLogger.Log($"[DLLInjectionCog] Deleted file at {targetPath}.");
            }
            else
            {
                ReboundLogger.Log("[DLLInjectionCog] No DLL file found to delete.");
            }

            // Delete metadata file with retry logic
            if (File.Exists(metaPath))
            {
                await DeleteFileWithRetryAsync(metaPath);
                ReboundLogger.Log($"[DLLInjectionCog] Deleted metadata file at {metaPath}.");
            }
            else
            {
                ReboundLogger.Log("[DLLInjectionCog] No metadata file found to delete.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[DLLInjectionCog] Remove failed with exception.", ex);
        }
    }

    /// <summary>
    /// Attempts to delete a file with retry logic (500ms intervals, 10 second timeout).
    /// </summary>
    private async Task DeleteFileWithRetryAsync(string filePath)
    {
        int maxRetries = 20; // 20 * 500ms = 10 seconds
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                File.Delete(filePath);
                return; // Success
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    ReboundLogger.Log($"[DLLInjectionCog] Failed to delete {filePath} after {maxRetries} retries.", ex);
                    throw;
                }
                ReboundLogger.Log($"[DLLInjectionCog] Delete failed for {filePath}, retry {retryCount}/{maxRetries}");
                await Task.Delay(500);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        try
        {
            var targetPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, Path.GetFileName(DLLPath));
            bool exists = File.Exists(targetPath);
            ReboundLogger.Log($"[DLLInjectionCog] IsApplied check: {targetPath} exists? {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[DLLInjectionCog] IsApplied failed with exception.", ex);
            return false;
        }
    }

    /// <summary>
    /// Queries the target processes list from the metadata file.
    /// </summary>
    /// <param name="dllDisplayName">The display name of the DLL (without extension).</param>
    /// <returns>A list of target process names, or an empty list if the file doesn't exist or is invalid.</returns>
#pragma warning disable CA1002 // Do not expose generic lists
    public static List<string> GetTargetProcessesFromMeta(string dllDisplayName)
#pragma warning restore CA1002 // Do not expose generic lists
    {
        try
        {
            var metaPath = Path.Combine(Variables.ReboundProgramFilesDLLsFolder, dllDisplayName + ".dllmeta");

            if (!File.Exists(metaPath))
            {
                ReboundLogger.Log($"[DLLInjectionCog] Metadata file not found: {metaPath}");
                return new List<string>();
            }

            var content = File.ReadAllText(metaPath);

            if (content.StartsWith("TARGET_PROCESSES=", StringComparison.OrdinalIgnoreCase))
            {
                var processesString = content.Substring("TARGET_PROCESSES=".Length);
                var processes = processesString.Split('\\', StringSplitOptions.RemoveEmptyEntries).ToList();
                ReboundLogger.Log($"[DLLInjectionCog] Retrieved {processes.Count} target processes from {metaPath}");
                return processes;
            }
            else
            {
                ReboundLogger.Log($"[DLLInjectionCog] Invalid metadata file format: {metaPath}");
                return new List<string>();
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[DLLInjectionCog] Failed to read metadata for {dllDisplayName}", ex);
            return new List<string>();
        }
    }
}