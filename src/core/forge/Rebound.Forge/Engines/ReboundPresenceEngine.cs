// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Security.Principal;
using Windows.Management.Deployment;

namespace Rebound.Forge.Engines;

public static class ReboundPresenceEngine
{
    /// <summary>
    /// Checks if Rebound is installed.
    /// </summary>
    public static Task<bool> IsReboundInstalledAsync()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value;

        if (sid is null)
            return Task.FromResult(false);

        var pm = new PackageManager();

        return Task.FromResult(
            pm.FindPackagesForUser(
                sid,
                "Rebound.ServiceHost_rcz2tbwv5qzb8")
            .Any());
    }
}
