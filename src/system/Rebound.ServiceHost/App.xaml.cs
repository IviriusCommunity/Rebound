// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Core.Helpers.Services;
using Rebound.Forge;
using Rebound.Forge.Engines;
using Rebound.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Shutdown;

namespace Rebound.ServiceHost;

[ReboundApp("Rebound.ServiceHost", "")]
public partial class App : Application
{
    private static readonly ConcurrentDictionary<int, byte> InjectedProcessIds = new();
    private static readonly SemaphoreSlim InjectionSemaphore = new(4);

    private static readonly HashSet<string> ExcludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrss.exe",
        "winlogon.exe",
        "smss.exe",
        "lsass.exe",
        "dwm.exe",
        "services.exe",
        "Rebound.Shell.exe",
        "Rebound.ServiceHost.exe"
    };

    private static async Task InjectIntoProcessAsync(Process proc, int injectTimeoutMs = 10_000)
    {
        try
        {
            // Quick pre-checks (no long lock)
            if (InjectedProcessIds.ContainsKey(proc.Id))
                return;

            if (ExcludedProcesses.Contains(proc.ProcessName + ".exe"))
                return;

            if (proc.SessionId == 0)
                return;

            if (!DLLInjector.CanOpenProcess(proc.Id))
                return;

            if (Environment.Is64BitOperatingSystem)
            {
                TerraFX.Interop.Windows.BOOL isWow64 = false;
                unsafe
                {
                    _ = TerraFX.Interop.Windows.Windows.IsWow64Process(new(proc.Handle.ToPointer()), &isWow64);
                }

                if (isWow64)
                {
                    Debug.WriteLine($"Skipping 32-bit process: {proc.ProcessName}");
                    return;
                }
            }

            // Acquire a semaphore slot so we don't start too many simultaneous injections.
            await InjectionSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // Double-check after acquiring a slot (race with other injectors)
                if (InjectedProcessIds.ContainsKey(proc.Id))
                    return;

                // Run the bounded injector on a threadpool thread to keep this method async-friendly.
                var success = await Task.Run(() => DLLInjector.Inject((uint)proc.Id, @$"{AppContext.BaseDirectory}\Hooks\Rebound.Forge.Hooks.Run.dll", (uint)injectTimeoutMs)).ConfigureAwait(false);

                if (success)
                {
                    InjectedProcessIds.TryAdd(proc.Id, 0);
                    Debug.WriteLine($"Successfully injected into {proc.ProcessName} (PID: {proc.Id})");
                }
                else
                {
                    Debug.WriteLine($"Injection failed/timeout for {proc.ProcessName} (PID: {proc.Id})");
                    // Optionally add to an exclusion list to avoid retrying too often
                }
            }
            finally
            {
                InjectionSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Injection error for {proc.ProcessName}: {ex.Message}");
        }
    }

    private static void InjectIntoExistingProcesses()
    {
        var allProcesses = Process.GetProcesses();
        Debug.WriteLine($"Scanning {allProcesses.Length} existing processes...");

        foreach (var proc in allProcesses)
        {
            if (!InjectedProcessIds.ContainsKey(proc.Id))
            {
                // Fire & forget but observe exceptions:
                _ = InjectIntoProcessAsync(proc).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Debug.WriteLine($"InjectIntoProcessAsync exception: {t.Exception.Flatten().InnerException?.Message}");
                    }
                }, TaskScheduler.Default);
            }
        }

        Debug.WriteLine($"Initial injection complete. Injected into {InjectedProcessIds.Count} processes.");
    }

    private static void MonitorNewProcesses()
    {
        Debug.WriteLine("Starting continuous process monitoring...");

        while (true)
        {
            try
            {
                var allProcesses = Process.GetProcesses();

                foreach (var proc in allProcesses)
                {
                    // If not already injected or currently being injected
                    if (!InjectedProcessIds.ContainsKey(proc.Id))
                    {
                        // Fire & forget async injection
                        _ = InjectIntoProcessAsync(proc).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                Debug.WriteLine($"InjectIntoProcessAsync exception in {proc.ProcessName}: " +
                                                $"{t.Exception.Flatten().InnerException?.Message}");
                            }
                        }, TaskScheduler.Default);
                    }
                }

                // Clean up dead processes from tracking
                var toRemove = new List<int>();

                foreach (var kvp in InjectedProcessIds)
                {
                    var pid = kvp.Key;
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (proc.HasExited)
                            toRemove.Add(pid);
                    }
                    catch
                    {
                        // Process no longer exists
                        toRemove.Add(pid);
                    }
                }

                foreach (var pid in toRemove)
                {
                    InjectedProcessIds.TryRemove(pid, out _);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in process monitor: {ex.Message}");
            }

            // Poll every 500ms for new processes
            Thread.Sleep(500);
        }
    }

    private static PipeHost? PipeServer;

    private void OnSingleInstanceLaunched(object sender, SingleInstanceLaunchEventArgs e) => Program.QueueAction(async () =>
    {
        if (e.IsFirstLaunch)
        {
            // Hook thread
            var hookThread = new Thread(() =>
            {
                InjectIntoExistingProcesses();

                var monitorThread = new Thread(MonitorNewProcesses)
                {
                    IsBackground = true,
                    Name = "Process Monitor"
                };
                monitorThread.Start();

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
                await PipeServer.StartAsync();
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
            await Task.Delay(1000);
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

