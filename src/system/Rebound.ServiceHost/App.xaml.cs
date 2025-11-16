// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.IPC;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Forge.Cogs;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Shutdown;

namespace Rebound.ServiceHost;

[ReboundApp("Rebound.ServiceHost", "")]
public partial class App : Application
{
    private static PipeHost? PipeServer;

    private void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e) => UIThreadQueue.QueueAction(async () =>
    {
        if (e.IsFirstLaunch)
        {
            // Hook thread
            var hookThread = new Thread(() =>
            {
                string dllsFolder = Variables.ReboundProgramDataDLLsFolder;

                // Since some processes like task manager are heavily locked down to prevent malicious injection,
                // it's required to enable SeDebugPrivilege to allow injection inside those processes.
                _ = DLLInjectionAPI.TryEnableSeDebugPrivilege(out _);

                // Get all DLL files in the folder
                if (Directory.Exists(dllsFolder))
                {
                    var dllFiles = Directory.GetFiles(dllsFolder, "*.dll");

                    foreach (var dllPath in dllFiles)
                    {
                        try
                        {
                            var dllName = Path.GetFileNameWithoutExtension(dllPath);
                            var targetProcesses = DLLInjectionCog.GetTargetProcessesFromMeta(dllName);

                            if (targetProcesses.Count == 0)
                            {
                                ReboundLogger.Log($"[HookThread] No target processes found for {dllName}, skipping.");
                                continue;
                            }

                            // Start the DLL injector in a separate thread for each DLL
                            var monitorThread = new Thread(() =>
                            {
                                var injector = new DLLInjector(dllPath);
                                injector.TargetProcesses.AddRange(targetProcesses);
                                injector.StartInjection();
                                ReboundLogger.Log($"[HookThread] Started injection for {dllName} into {targetProcesses.Count} processes.");
                            })
                            {
                                IsBackground = true,
                                Name = $"Process Monitor - {dllName}"
                            };
                            monitorThread.Start();
                        }
                        catch (Exception ex)
                        {
                            ReboundLogger.Log($"[HookThread] Failed to start injector for {Path.GetFileName(dllPath)}", ex);
                        }
                    }
                }
                else
                {
                    ReboundLogger.Log($"[HookThread] DLLs folder not found: {dllsFolder}");
                }

                // Keep message loop running for hooks
                NativeMessageLoop();
            })
            {
                IsBackground = true,
                Name = "Hook Thread"
            };
            hookThread.SetApartmentState(ApartmentState.STA);
            hookThread.Start();

            // Pipe server thread
            var pipeThread = new Thread(async () =>
            {
                PipeServer = new("REBOUND_SERVICE_HOST", AccessLevel.ModWhitelist);
                PipeServer.MessageReceived += PipeServer_MessageReceived;
                await PipeServer.StartAsync().ConfigureAwait(false);
            })
            {
                IsBackground = true,
                Name = "Pipe Server Thread"
            };
            pipeThread.SetApartmentState(ApartmentState.STA);
            pipeThread.Start();

            NativeMessageLoop();
        }
        else Process.GetCurrentProcess().Kill();
    });

    private static readonly SHUTDOWN_REASON[] MajorReasons =
    [
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_HARDWARE,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OPERATINGSYSTEM,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_HARDWARE,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_POWER,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_SYSTEM
    ];

    private static readonly SHUTDOWN_REASON[] MinorReasons =
    [
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_OTHER,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_MAINTENANCE,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_INSTALLATION,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_HARDWARE_DRIVER,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_POWER_SUPPLY,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_BLUESCREEN
    ];

    private static readonly SHUTDOWN_REASON[] Flags =
    [
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_PLANNED,
        0x00000000, // Unplanned
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_USER_DEFINED,
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_DIRTY_UI
    ];

    private async void PipeServer_MessageReceived(PipeConnection connection, string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return;

        if (arg == "Shell::RestartToUEFI")
        {
            RunShutdownCommand("/r /fw /t 0");
        }
        else if (arg == "Shell::RestartToRecovery")
        {
            RunShutdownCommand("/r /o /t 0");
        }
        else if (arg == "Shell::Shutdown")
        {
            RunShutdownCommand("/s /t 0");
        }
        else if (arg == "Shell::Restart")
        {
            RunShutdownCommand("/r /t 0");
        }
        else if (arg.StartsWith("Shell::ShutdownServer#", StringComparison.InvariantCultureIgnoreCase))
        {
            var parts = arg["Shell::ShutdownServer#".Length..].ToCharArray();

            if (parts.Length >= 2 &&
                int.TryParse(parts[0].ToString(), out var reasonIndex) &&
                int.TryParse(parts[1].ToString(), out var modeIndex))
            {
                // Clamp to array length just to be safe
                reasonIndex = Math.Clamp(reasonIndex, 0, MajorReasons.Length - 1);
                modeIndex = Math.Clamp(modeIndex, 0, Flags.Length - 1);

                var reasonCode = MajorReasons[reasonIndex] | MinorReasons[reasonIndex] | Flags[modeIndex];

                RunShutdownCommand("/s /t 0", reasonCode);
            }
        }

        else if (arg.StartsWith("IFEOEngine::Pause#", StringComparison.InvariantCultureIgnoreCase))
        {
            var part = arg["IFEOEngine::Pause#".Length..];

            await IFEOEngine.PauseIFEOEntryAsync(part);
        }
        else if (arg.StartsWith("IFEOEngine::Resume#", StringComparison.InvariantCultureIgnoreCase))
        {
            var part = arg["IFEOEngine::Resume#".Length..];

            await IFEOEngine.ResumeIFEOEntryAsync(part);
        }

        return;
    }

    private static void RunShutdownCommand(string args, SHUTDOWN_REASON reason) => _ = PInvoke.InitiateSystemShutdownEx(
            null, "Shutdown initiated via broker".ToPWSTR(),
            0, true, args.Contains("/r", StringComparison.InvariantCultureIgnoreCase), reason);

    private static void RunShutdownCommand(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = arguments,
            UseShellExecute = true,
            CreateNoWindow = true
        };
        try
        {
            _ = Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log or handle error here if needed
            Debug.WriteLine($"Failed to execute shutdown command: {ex.Message}");
        }
    }

    private static void NativeMessageLoop()
    {
        while (true)
        {
            _ = PInvoke.GetMessage(out var msg, Windows.Win32.Foundation.HWND.Null, 0, 0);
            _ = PInvoke.TranslateMessage(msg);
            _ = PInvoke.DispatchMessage(msg);
        }
    }
}

