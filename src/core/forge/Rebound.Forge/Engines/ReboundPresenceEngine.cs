// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;

namespace Rebound.Forge.Engines;

public static class ReboundPresenceEngine
{
    /// <summary>
    /// Checks if Rebound is installed by verifying the existence of the Rebound program files folder.
    /// This method is meant to be used as a very quick check to determine if Rebound is installed and does
    /// not provide any guarantees about the integrity of the installation. It simply checks for the presence of the folder.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the Rebound program files folder exists, indicating that Rebound is likely installed; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsReboundInstalled()
        => File.Exists(Variables.ReboundProgramFilesFolder);
}
