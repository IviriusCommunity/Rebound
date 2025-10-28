// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Rebound.Forge.Cogs;

internal class LauncherCog : ICog
{
    public required string Path { get; set; }

    public required string TargetPath { get; set; }

    public LauncherCog()
    {

    }

    public async Task ApplyAsync()
    {
        try
        {
            ReboundLogger.Log("[LauncherCog] Apply started.");

            WorkingEnvironment.EnsureFolderIntegrity();
            ReboundLogger.Log($"[LauncherCog] Ensured folder integrity.");

            // Copy the launcher file
            File.Copy(Path, TargetPath, true);
            ReboundLogger.Log($"[LauncherCog] Copied file from {Path} to {TargetPath}.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] Apply failed with exception.", ex);
        }
    }

    public async Task RemoveAsync()
    {
        try
        {
            ReboundLogger.Log("[LauncherCog] Remove started.");

            if (File.Exists(TargetPath))
            {
                File.Delete(TargetPath);
                ReboundLogger.Log($"[LauncherCog] Deleted file at {TargetPath}.");
            }
            else
            {
                ReboundLogger.Log("[LauncherCog] No file found to delete.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] Remove failed with exception.", ex);
        }
    }

    public async Task<bool> IsAppliedAsync()
    {
        try
        {
            bool exists = File.Exists(TargetPath);
            ReboundLogger.Log($"[LauncherCog] IsApplied check: {TargetPath} exists? {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[LauncherCog] IsApplied failed with exception.", ex);
            return false;
        }
    }
}