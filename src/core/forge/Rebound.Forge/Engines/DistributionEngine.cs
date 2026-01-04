// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Storage;
using Rebound.Forge.Cogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Windows.Storage;

namespace Rebound.Forge.Engines;

public static class DistributionEngine
{
    /// <summary>
    /// Installs the Rebound Hub root certificate into the local machine's trusted root certificate store.
    /// </summary>
    /// <remarks>This method requires administrative privileges to modify the local machine's certificate
    /// store.</remarks>
    public static void InstallReboundHubCertificate()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Rebound.Hub_TemporaryKey.pfx");

        X509Certificate2 certificate =
            X509CertificateLoader.LoadPkcs12FromFile(
                path,
                password: null,
                keyStorageFlags:
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet);

        using (certificate)
        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
        }
    }

    /// <summary>
    /// Initiates the removal of the mod's uninstaller executable and its associated shortcut, then terminates
    /// the current process.
    /// </summary>
    /// <remarks>This method schedules the deletion of the uninstaller executable and its shortcut by
    /// launching a command prompt process. The current application process will exit immediately after initiating the
    /// removal. This operation is irreversible and should only be called when the uninstaller is no longer
    /// needed.</remarks>
    public static void UninstallUninstaller()
    {
        var baseDir = AppContext.BaseDirectory;
        var uninstaller = Path.Combine(baseDir, "Rebound.Uninstaller.exe");
        var shortcut = ShortcutCog.GetShortcutPath("Uninstall Rebound");

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments =
                $"/C timeout /t 2 >nul && " +
                $"del /f \"{uninstaller}\" && " +
                $"del /f \"{shortcut}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Environment.Exit(0);
    }
}
