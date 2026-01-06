// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Rebound.Forge.Engines;

public class ServiceHostWatchdogEngine
{
    public static void Start()
    {
        while (true)
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
                }
                catch
                {

                }
            }
            Thread.Sleep(2000);
        }
    }
}