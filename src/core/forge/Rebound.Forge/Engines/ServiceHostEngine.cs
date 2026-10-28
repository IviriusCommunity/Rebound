// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Rebound.Forge.Engines;

public static class ServiceHostEngine
{
    /// <summary>
    /// Periodically checks if Rebound Service Host is alive and launches it if not.
    /// </summary>
    public static void StartWatchdog()
    {
        while (true)
        {
            if (!Process.GetProcesses().Any(p => p.ProcessName == "Rebound.ServiceHost"))
            {
                try
                {
                    ApplicationLaunchEngine.LaunchApp("Rebound.ServiceHost_rcz2tbwv5qzb8");
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
        if (!Process.GetProcesses().Any(p => p.ProcessName == "Rebound.ServiceHost"))
        {
            try
            {
                ApplicationLaunchEngine.LaunchApp("Rebound.ServiceHost_rcz2tbwv5qzb8");
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