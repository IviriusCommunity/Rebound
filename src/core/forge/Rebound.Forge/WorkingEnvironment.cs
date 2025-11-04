// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge;

public static class WorkingEnvironment
{
    #region Version

    public static void UpdateVersion()
    {
        try
        {
            var versionFile = Path.Combine(Variables.ReboundDataFolder, "version.txt");

            File.WriteAllText(versionFile, $"{Variables.ReboundVersion}");
            ReboundLogger.Log($"[WorkingEnvironment] Updated version.txt to {Variables.ReboundVersion}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[WorkingEnvironment] Failed to update version.txt:", ex);
        }
    }

    #endregion
}