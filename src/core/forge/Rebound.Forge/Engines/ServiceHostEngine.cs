// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Rebound.Forge.Engines;

public class ServiceHostEngine
{
    /// <summary>
    /// Periodically checks if Rebound Service Host is alive and launches it if not.
    /// </summary>
    public static void StartWatchdog()
    {
        while (true)
        {
            if (!Process.GetProcesses().Any(p => p.ProcessName == "Rebound Service Host"))
            {
                try
                {
                    if (File.Exists(Path.Combine(Variables.ReboundProgramFilesFolder, "ServiceHost", "Rebound Service Host.exe")))
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Path.Combine(Variables.ReboundProgramFilesFolder, "ServiceHost", "Rebound Service Host.exe"),
                            UseShellExecute = true,
                            Verb = "runas"
                        });
                }
                catch
                {

                }
            }
            Thread.Sleep(2000);
        }
    }

    /// <summary>
    /// Attempts to launch Rebound Service Host.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if Rebound Service Host has been launched successfully. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool StartServiceHost()
    {
        if (!Process.GetProcesses().Any(p => p.ProcessName == "Rebound Service Host"))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Variables.ReboundProgramFilesFolder, "ServiceHost", "Rebound Service Host.exe"),
                    UseShellExecute = true,
                    Verb = "runas"
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
        else return true;
    }
}