// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.IPC;
using System;
using System.Diagnostics;

namespace Rebound.ServiceHost.Modules;

internal partial class LauncherModule : IDisposable
{
    private PipeHost? LauncherPipeServer;

    public LauncherModule()
    {
        LauncherPipeServer = new PipeHost(
            "REBOUND_LAUNCHER",
            AccessLevel.Everyone);
        LauncherPipeServer.MessageReceived += LauncherPipeServer_MessageReceived;
    }

    public void Start() => _ = LauncherPipeServer?.StartAsync();

    private void LauncherPipeServer_MessageReceived(PipeConnection arg1, string arg2)
    {
        if (arg2.StartsWith("LAUNCH:", StringComparison.InvariantCultureIgnoreCase))
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = arg2[7..],
                UseShellExecute = true
            });
        }
    }

    public void Dispose()
        => Dispose(true);

    public virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LauncherPipeServer?.Dispose();
        }
        LauncherPipeServer = null;
    }
}