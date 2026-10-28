// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.IPC;
using System;
using System.Diagnostics;
using System.Text;

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

    private void LauncherPipeServer_MessageReceived(
        PipeConnection connection,
        string message)
    {
        if (!message.StartsWith("LAUNCH:",
            StringComparison.OrdinalIgnoreCase))
            return;

        string[] parts = message[7..].Split(
            @"\lnbrk\",
            2,
            StringSplitOptions.None);

        if (parts.Length == 0 ||
            string.IsNullOrWhiteSpace(parts[0]))
            return;

        string arguments = parts.Length > 1
            ? Encoding.UTF8.GetString(
                Convert.FromBase64String(parts[1]))
            : string.Empty;

        Process.Start(new ProcessStartInfo
        {
            FileName = parts[0],
            Arguments = arguments,
            UseShellExecute = true
        });
    }

    public void Dispose() => Dispose(true);

    public virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            LauncherPipeServer?.Dispose();
        }
        LauncherPipeServer = null;
    }
}