// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Helpers;
using Rebound.Forge.Engines;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.System.Shutdown;

namespace Rebound.ServiceHost;

//[ReboundApp("Rebound.ServiceHost", "")]
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

            if (!Injector.CanOpenProcess(proc.Id))
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
                var success = await Task.Run(() => Injector.Inject((uint)proc.Id, @$"{AppContext.BaseDirectory}\Hooks\Rebound.Forge.Hooks.Run.dll", (uint)injectTimeoutMs)).ConfigureAwait(false);

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

    private static TrustedPipeServer? PipeServer;

    public App()
    {
        // Window hooks
        var thread = new Thread(() =>
        {
            // Inject into all existing processes first
            InjectIntoExistingProcesses();

            // Start monitoring for new processes in background
            var monitorThread = new Thread(MonitorNewProcesses)
            {
                IsBackground = false,
                Name = "Process Monitor"
            };
            monitorThread.Start();

            // Keep message pump alive so all hooks keep working
            NativeMessageLoop();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = false;
        thread.Start();

        // Server
        _ = Task.Run(StartPipeServer);
    }

    private void StartPipeServer()
    {
        PipeServer = new TrustedPipeServer("REBOUND_SERVICE_HOST");
        _ = PipeServer.StartAsync();

        PipeServer.MessageReceived += PipeServer_MessageReceived;
    }

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

    private async Task PipeServer_MessageReceived(string arg)
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

// New RunFileDlg hook (ordinal #61)
internal class Injector
{
    public static bool CanOpenProcess(int pid)
    {
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(PROCESS_ALL_ACCESS, bInheritHandle: false, dwProcessId: (uint)pid);
        if (hProcess == IntPtr.Zero) return false;
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
        return true;
    }

    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint MEM_RELEASE = 0x8000;
    private const uint INFINITE = 0xFFFFFFFF;

    public static unsafe bool Inject(uint pid, string dllPath, uint waitTimeoutMs = 10_000)
    {
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(
            dwDesiredAccess: PROCESS_ALL_ACCESS,
            bInheritHandle: false,
            dwProcessId: pid);

        if (hProcess == IntPtr.Zero)
        {
            Debug.WriteLine($"Failed to open process {pid}. Error: {Marshal.GetLastWin32Error()}");
            return false;
        }

        var dllPathBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + "\0");
        var allocMem = TerraFX.Interop.Windows.Windows.VirtualAllocEx(
            hProcess,
            lpAddress: null,
            dwSize: (uint)dllPathBytes.Length,
            flAllocationType: MEM_COMMIT | MEM_RESERVE,
            flProtect: PAGE_READWRITE);

        if ((nint)allocMem == IntPtr.Zero)
        {
            Debug.WriteLine($"Failed to allocate memory in target process. Error: {Marshal.GetLastWin32Error()}");
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        fixed (byte* pBytes = dllPathBytes)
        {
            if (!TerraFX.Interop.Windows.Windows.WriteProcessMemory(
                hProcess,
                lpBaseAddress: allocMem,
                lpBuffer: pBytes,
                nSize: (uint)dllPathBytes.Length,
                lpNumberOfBytesWritten: null))
            {
                Debug.WriteLine($"Failed to write DLL path to target process. Error: {Marshal.GetLastWin32Error()}");
                _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }
        }

        var hKernel32 = TerraFX.Interop.Windows.Windows.GetModuleHandleW("kernel32.dll".ToPCWSTR().Value);
        var loadLibraryAddr = TerraFX.Interop.Windows.Windows.GetProcAddress(
            hKernel32,
            (sbyte*)Marshal.StringToHGlobalAnsi("LoadLibraryW"));

        if (loadLibraryAddr == null)
        {
            Debug.WriteLine("Failed to get address of LoadLibraryW.");
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        var loadLibraryFunc = (delegate* unmanaged<void*, uint>)loadLibraryAddr;

        var hThread = TerraFX.Interop.Windows.Windows.CreateRemoteThread(
            hProcess,
            null,
            0,
            loadLibraryFunc,
            allocMem,
            0,
            null
        );

        if (hThread == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"Failed to create remote thread in process {pid}. Error: {error}");
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Wait with timeout
        uint waitResult = TerraFX.Interop.Windows.Windows.WaitForSingleObject(hThread, waitTimeoutMs);

        uint exitCode = 0;
        if (waitResult == TerraFX.Interop.Windows.WAIT.WAIT_OBJECT_0)
        {
            _ = TerraFX.Interop.Windows.Windows.GetExitCodeThread(hThread, &exitCode);
        }
        else if (waitResult == TerraFX.Interop.Windows.WAIT.WAIT_TIMEOUT)
        {
            Debug.WriteLine($"LoadLibraryW timed out in process {pid} after {waitTimeoutMs}ms.");
            // Clean up handles and memory, but the remote thread may still be running inside LoadLibraryW.
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }
        else
        {
            Debug.WriteLine($"WaitForSingleObject failed for process {pid}. Error: {Marshal.GetLastWin32Error()}");
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Normal cleanup
        _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);

        if (exitCode == 0)
        {
            Debug.WriteLine($"DLL injection FAILED for process {pid} - LoadLibraryW returned NULL (DLL not loaded or dependencies missing)");
            return false;
        }

        Debug.WriteLine($"Successfully injected into x64 process {pid} - Module handle: 0x{exitCode:X}");
        return true;
    }
}