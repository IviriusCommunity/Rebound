// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Core.DLLInjection;

public class DLLInjector
{
    string _dllPath;

    /// <summary>
    /// Contains the names of processes that are targeted for monitoring or interaction.
    /// </summary>
    /// <remarks>Add process names to this list to specify which processes should be included in the
    /// operation. The list is initialized as empty and can be modified at runtime.
    /// If the list is left empty, all processes will be targeted.</remarks>
    public List<string> TargetProcesses { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the DLLInjector class with the specified DLL file path.
    /// </summary>
    /// <param name="dllPath">The full path to the DLL file to be injected. Must not be null or empty.</param>
    public DLLInjector(string dllPath) => _dllPath = dllPath;

    /// <summary>
    /// Starts the DLL injection process for existing and newly launched target processes.
    /// </summary>
    /// <remarks>This method initiates injection into all currently running target processes and begins
    /// monitoring for new processes to inject as they start. The monitoring runs asynchronously and continues until the
    /// application is terminated. Ensure that the DLL path and target process list are correctly configured before
    /// calling this method.</remarks>
    public void StartInjection()
    {
        DLLInjectionAPI.InjectIntoExistingProcesses(_dllPath, TargetProcesses);
        Task.Run(() => DLLInjectionAPI.MonitorNewProcessesLoop(_dllPath, TargetProcesses));
    }
}


public static class DLLInjectionAPI
{
    private static readonly ConcurrentDictionary<int, byte> InjectedProcessIds = new();
    private static readonly SemaphoreSlim InjectionSemaphore = new(4);
    private static readonly HashSet<string> ExcludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrss.exe","winlogon.exe","smss.exe","lsass.exe","dwm.exe","services.exe",
        "Rebound.Shell.exe","Rebound.ServiceHost.exe"
    };

    /// <summary>
    /// Attempts to enable the SeDebugPrivilege for the current process, allowing it to debug and access protected
    /// system processes.
    /// </summary>
    /// <remarks>Enabling SeDebugPrivilege may require administrative rights. This method does not throw
    /// exceptions; instead, any error encountered is returned in the <paramref name="error"/> parameter. If the
    /// privilege is already enabled, the method will still return true. Use this method before attempting operations
    /// that require elevated debugging privileges on Windows.</remarks>
    /// <param name="error">When the method returns <see langword="false"/>, contains a description of the error that occurred; otherwise,
    /// <see langword="null"/>.</param>
    /// <returns>true if the SeDebugPrivilege was successfully enabled for the current process; otherwise, false.</returns>
    public static unsafe bool TryEnableSeDebugPrivilege(out string? error)
    {
        error = null;
        HANDLE processToken;
        LUID luid;

        try
        {
            // Get current process token
            if (!TerraFX.Interop.Windows.Windows.OpenProcessToken(
                new((void*)Process.GetCurrentProcess().Handle), // The process handle here is a memory address
                0x0020 | 0x0008, // TOKEN_ADJUST_PRIVILEGES, TOKEN_QUERY
                &processToken))
            {
                error = $"OpenProcessToken failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            try
            {
                // Lookup the LUID for SeDebugPrivilege
                if (!TerraFX.Interop.Windows.Windows.LookupPrivilegeValue(null, SE.SE_DEBUG_NAME.ToPCWSTR().Value, &luid))
                {
                    error = $"LookupPrivilegeValue failed: {Marshal.GetLastWin32Error()}";
                    return false;
                }

                // Adjust token privileges to enable SeDebugPrivilege
                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new()
                    {
                        e0 = new()
                        {
                            Luid = luid,
                            Attributes = SE.SE_PRIVILEGE_ENABLED
                        }
                    }
                };

                // Enable the privilege for the current process
                if (!TerraFX.Interop.Windows.Windows.AdjustTokenPrivileges(processToken, false, &tp, 0, null, null))
                {
                    error = $"AdjustTokenPrivileges failed: {Marshal.GetLastWin32Error()}";
                    return false;
                }

                // AdjustTokenPrivileges may succeed but still set last error; check Marshal.GetLastWin32Error() == 0
                var last = Marshal.GetLastWin32Error();
                if (last != 0)
                {
                    error = $"AdjustTokenPrivileges succeeded but returned last error {last}";
                    return false;
                }

                return true;
            }
            finally
            {
                _ = TerraFX.Interop.Windows.Windows.CloseHandle(processToken);
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Attempts to asynchronously inject a DLL into the specified process if it meets eligibility criteria and has not
    /// already been injected.
    /// </summary>
    /// <remarks>The method performs several checks before attempting injection, including process state,
    /// session ID, exclusion lists, and required permissions. If the process is ineligible or injection fails, the
    /// method returns without throwing an exception. Injection is skipped for 32-bit processes on 64-bit operating
    /// systems. The method is thread-safe and prevents concurrent injection attempts for the same process.</remarks>
    /// <param name="proc">The target <see cref="System.Diagnostics.Process"/> instance into which the DLL will be injected. Must not be
    /// null and must reference a running process.</param>
    /// <param name="injectTimeoutMs">The maximum time, in milliseconds, to wait for the injection operation to complete. Defaults to 10,000
    /// milliseconds.</param>
    /// <returns>A task that represents the asynchronous injection operation. The task completes when the injection attempt
    /// finishes, regardless of success or failure.</returns>
    public static async Task InjectIntoProcessAsync(Process proc, string dllPath, int injectTimeoutMs = 10_000)
    {
        // If the process is null or has exited, return
        if (proc == null) return;

        // Get the process ID (there shouldn't be any issues regarding access here
        // unless the process it's trying to access is running as NT AUTHORITY\SYSTEM or TI)
        int pid = proc.Id;

        try
        {
            // Return if the process has already been injected into
            if (InjectedProcessIds.ContainsKey(pid)) return;

            // Make sure the process is still running and responsive
            proc.Refresh();
            if (proc.HasExited) return;

            // Excluded process
            if (ExcludedProcesses.Contains(proc.ProcessName + ".exe")) return;

            // Final checks: return if the session ID is zero or if there's not enough permissions to open the process
            if (proc.SessionId == 0) return;
            if (!CanOpenProcess(pid)) return;

            // Reserve in-progress marker (2 == in-progress)
            if (!InjectedProcessIds.TryAdd(pid, 2)) return;

            // Cancellation token for timeout, otherwise the thread might end up waiting forever
            // if the process it tries to inject into is an AV or heavily protected
            using var cts = new CancellationTokenSource(injectTimeoutMs);
            try
            {
                await InjectionSemaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InjectedProcessIds.TryRemove(pid, out _); Debug.WriteLine($"Timed out waiting slot for PID {pid}"); return;
            }

            try
            {
                // Check if the process is still running, for safety
                proc.Refresh();
                if (proc.HasExited)
                {
                    InjectedProcessIds.TryRemove(pid, out _);
                    return;
                }

                // 64-bit OS -> skip 32-bit processes (Rebound does not have support for x32 or ARM64)
                if (Environment.Is64BitOperatingSystem)
                {
                    try
                    {
                        BOOL isWow64 = false;
                        unsafe
                        {
#pragma warning disable CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
                            _ = TerraFX.Interop.Windows.Windows.IsWow64Process(new((void*)proc.Handle), &isWow64);
#pragma warning restore CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
                        }
                        if (isWow64)
                        {
                            InjectedProcessIds.TryRemove(pid, out _);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Wow64 check warning PID {pid}: {ex.Message}");
                    }
                }

                // Start injection thread
                var injectTask = Task.Run(() =>
                {
                    try
                    {
                        return Inject((uint)pid, dllPath, (uint)injectTimeoutMs);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Injector threw PID {pid}: {e.Message}");
                        return false;
                    }
                }, cts.Token);

                // Wait for injection success or timeout
                var finished = await Task.WhenAny(injectTask, Task.Delay(injectTimeoutMs, cts.Token)).ConfigureAwait(false);
                if (finished != injectTask)
                {
                    InjectedProcessIds.TryRemove(pid, out _); Debug.WriteLine($"Injection timed out PID {pid}");
                    return;
                }

                // Get injection result
                bool success = false;
                try
                {
                    success = await injectTask.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Injection fault PID {pid}: {e.Message}");
                }

                // Mark as injected or remove on failure
                if (success)
                {
                    InjectedProcessIds[pid] = 0;
                    Debug.WriteLine($"Injected PID {pid} ({proc.ProcessName})");
                }
                else
                {
                    InjectedProcessIds.TryRemove(pid, out _);
                    Debug.WriteLine($"Injection failed PID {pid}");
                }
            }
            finally
            {
                InjectionSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"InjectIntoProcessAsync error PID {proc?.Id}: {ex.Message}");
            InjectedProcessIds.TryRemove(proc?.Id ?? 0, out _);
        }
    }

    /// <summary>
    /// Initiates DLL injection into all currently running processes using the specified DLL path.
    /// </summary>
    /// <remarks>Injection is performed asynchronously for each process to avoid blocking the main thread and
    /// to minimize the impact of processes with DLL injection security. This method does not guarantee successful
    /// injection into every process; processes with elevated security or insufficient permissions may prevent
    /// injection. Exceptions encountered during injection are logged for diagnostic purposes.</remarks>
    /// <param name="dllPath">The full file path to the DLL to be injected into each process. Must refer to a valid, accessible DLL file.</param>
    public static void InjectIntoExistingProcesses(string dllPath, List<string>? targetProcesses)
    {
        // Get every running process
        var procs = Process.GetProcesses();
        Debug.WriteLine($"Scanning {procs.Length} processes...");

        // Normalize targetProcesses into a HashSet for fast, case-insensitive lookups.
        // Accept entries like "notepad" or "notepad.exe".
        HashSet<string>? normalizedTargets = null;

        if (targetProcesses != null && targetProcesses.Count > 0)
        {
            normalizedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in targetProcesses)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                var n = t.Trim();
                if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    n = n.Substring(0, n.Length - 4);
                normalizedTargets.Add(n);
            }
        }

        List<Process> filteredProcs;
        if (normalizedTargets != null)
        {
            filteredProcs = new List<Process>();
            foreach (var proc in procs)
            {
                try
                {
                    // Process.ProcessName is the exe name without extension; compare using normalizedTargets.
                    if (normalizedTargets.Contains(proc.ProcessName ?? string.Empty))
                    {
                        filteredProcs.Add(proc);
                    }
                }
                catch
                {
                    // Ignore processes that can't be accessed
                }
            }
            Debug.WriteLine($"Filtered to {filteredProcs.Count} target processes.");
        }
        else
        {
            // No targets given -> inject into everything
            filteredProcs = new List<Process>(procs);
        }

        // Perform injection on a separate thread per process
        foreach (var p in filteredProcs)
        {
            // capture local var so Task.Run uses the right one
            var capture = p;
            Task.Run(() =>
                InjectIntoProcessAsync(capture, dllPath).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Debug.WriteLine(t.Exception.Flatten().InnerException?.Message);
                },
                TaskScheduler.Default));
        }
    }

    // Monitor new processes using CreateToolhelp32Snapshot; trim-friendly and fast when polled frequently
    private static volatile bool _monitorRunning;

    /// <summary>
    /// Continuously monitors for newly started processes and injects the specified DLL into each eligible process as
    /// they are detected.
    /// </summary>
    /// <remarks>This method runs an infinite loop and should be executed on a background thread to avoid
    /// blocking the main application. It skips processes that have already been injected or previously observed. The
    /// method is not thread-safe and should not be called concurrently. Errors encountered during process enumeration
    /// or injection are logged and do not stop the monitoring loop.</remarks>
    /// <param name="dllPath">The full path to the DLL file to inject into new processes. Must not be null or empty.</param>
    /// <param name="pollMs">The interval, in milliseconds, between each scan for new processes. Must be greater than zero. The default is
    /// 200 milliseconds.</param>
    public static void MonitorNewProcessesLoop(string dllPath, List<string> targetProcesses, int pollMs = 200)
    {
        // Ensure only one monitor is running
        if (_monitorRunning)
            return;

        _monitorRunning = true;
        Debug.WriteLine("Starting Toolhelp-based process monitor...");

        // Normalize targetProcesses into a HashSet for fast, case-insensitive lookups.
        // Accept entries like "notepad" or "notepad.exe".
        HashSet<string> normalizedTargets = null;
        if (targetProcesses != null && targetProcesses.Count > 0)
        {
            normalizedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in targetProcesses)
            {
                if (string.IsNullOrWhiteSpace(t)) continue;
                var n = t.Trim();
                if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    n = n.Substring(0, n.Length - 4);
                normalizedTargets.Add(n);
            }
        }
        // If normalizedTargets == null, treat as "inject into everything".

        // Keep track of seen processes to avoid re-checking them
        var seen = new HashSet<int>();
        while (true)
        {
            try
            {
                // Get a list of all PIDs
                var pids = EnumerateProcessIds();
                foreach (var pid in pids)
                {
                    if (InjectedProcessIds.ContainsKey(pid))
                    {
                        seen.Add(pid); continue; // already injected
                    }
                    if (seen.Contains(pid)) continue; // already observed previously but not injected (race)

                    try
                    {
                        // Try to open the process and inject into it on a separate thread
                        // to avoid blocking the monitor loop on protected processes
                        var proc = Process.GetProcessById(pid);
                        if (proc.HasExited)
                        {
                            seen.Add(pid); continue;
                        }

                        // If a target list was provided, only inject if the process name matches.
                        if (normalizedTargets != null)
                        {
                            // ProcessName returns the executable name without ".exe"
                            var procName = proc.ProcessName ?? string.Empty; // safe fallback
                            if (!normalizedTargets.Contains(procName))
                            {
                                // Not a target — skip injection but mark as seen
                                seen.Add(pid);
                                continue;
                            }
                        }

                        Task.Run(() => InjectIntoProcessAsync(proc, dllPath));
                    }
                    catch
                    {
                        /* Process vanished or cannot be opened; we'll never know why */
                    }
                    finally
                    {
                        seen.Add(pid);
                    }
                }

                // Cleanup seen/InjectedProcessIds for dead processes to allow
                // injecting back into these PIDs on the very rare occasion that two PIDs will ever be the same
                var toRemove = new List<int>();
                foreach (var kvp in InjectedProcessIds)
                {
                    int pid = kvp.Key;
                    try
                    {
                        var p = Process.GetProcessById(pid); if (p.HasExited) toRemove.Add(pid);
                    }
                    catch
                    {
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
                Debug.WriteLine($"Monitor loop error: {ex.Message}");
            }
            Thread.Sleep(pollMs);
        }
    }

    /// <summary>
    /// Retrieves a list of process identifiers (PIDs) for all processes currently running on the system.
    /// </summary>
    /// <remarks>This method uses the Windows Toolhelp API to enumerate processes. The returned list may not
    /// include processes that start or terminate during enumeration. This method requires appropriate permissions to
    /// access process information.</remarks>
    /// <returns>A list of integers containing the process IDs of all active processes. The list will be empty if no processes
    /// are found or if the snapshot cannot be created.</returns>
    private static unsafe List<int> EnumerateProcessIds()
    {
        var ret = new List<int>();
        HANDLE snap = HANDLE.NULL;
        try
        {
            // Create snapshot of all processes
            snap = TerraFX.Interop.Windows.Windows.CreateToolhelp32Snapshot(0x00000002, 0); // TH32CS_SNAPPROCESS

            // Return if snapshot handle is invalid
            if (snap == IntPtr.Zero || snap == new IntPtr(-1))
                return ret;

            var pe = new PROCESSENTRY32W
            {
                dwSize = (uint)sizeof(PROCESSENTRY32W)
            };

            if (!TerraFX.Interop.Windows.Windows.Process32FirstW(snap, &pe))
                return ret;

            // Iterate through processes
            do
            {
                ret.Add((int)pe.th32ProcessID);
            }
            while (TerraFX.Interop.Windows.Windows.Process32NextW(snap, &pe));
        }
        finally
        {
            if (snap != HANDLE.NULL && snap != HANDLE.INVALID_VALUE) _ = TerraFX.Interop.Windows.Windows.CloseHandle(snap);
        }
        return ret;
    }

    /// <summary>
    /// Determines whether a process with the specified process ID can be opened with all access rights.
    /// </summary>
    /// <remarks>This method attempts to open the process with full access rights. A return value of <see
    /// langword="false"/> may indicate insufficient privileges, a non-existent process, or system restrictions. No
    /// handle is retained after the check.</remarks>
    /// <param name="pid">The identifier of the process to check. Must correspond to an existing process; otherwise, the method returns
    /// <see langword="false"/>.</param>
    /// <returns>Returns <see langword="true"/> if the process can be opened with all access rights; otherwise, <see
    /// langword="false"/>.</returns>
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

    /// <summary>
    /// Attempts to inject a DLL into a running process by creating a remote thread that loads the specified DLL.
    /// </summary>
    /// <remarks>This method requires sufficient privileges to open and modify the target process. If the
    /// operation fails due to permission issues, invalid process ID, or timeout, the method returns false. The caller
    /// is responsible for ensuring that the DLL is compatible with the target process architecture and environment. Use
    /// with caution, as DLL injection can have security and stability implications.</remarks>
    /// <param name="pid">The identifier of the target process into which the DLL will be injected.</param>
    /// <param name="dllPath">The full file system path to the DLL to be injected. Must not be null or empty.</param>
    /// <param name="waitTimeoutMs">The maximum time, in milliseconds, to wait for the remote thread to complete the DLL injection. Must be a
    /// non-negative value. The default is 10,000 milliseconds.</param>
    /// <returns>true if the DLL was successfully injected into the target process; otherwise, false.</returns>
    public static unsafe bool Inject(uint pid, string dllPath, uint waitTimeoutMs = 10_000)
    {
        // Obtain a handle to the target process
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(
            dwDesiredAccess: PROCESS_ALL_ACCESS,
            bInheritHandle: false,
            dwProcessId: pid);

        // Skip if the handle is invalid, most likely due to insufficient permissions
        if (hProcess == IntPtr.Zero)
        {
            return false;
        }

        // Obtain raw bytes of the DLL path and allocate memory in the target process
        var dllPathBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + "\0");
        var allocMem = TerraFX.Interop.Windows.Windows.VirtualAllocEx(
            hProcess,
            lpAddress: null,
            dwSize: (uint)dllPathBytes.Length,
            flAllocationType: MEM_COMMIT | MEM_RESERVE,
            flProtect: PAGE_READWRITE);

        // Return false if memory allocation failed, likely due to insufficient permissions or memory issues
        if ((nint)allocMem == IntPtr.Zero)
        {
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        fixed (byte* pBytes = dllPathBytes)
        {
            // Write the DLL path into the allocated memory of the target process
            if (!TerraFX.Interop.Windows.Windows.WriteProcessMemory(
                hProcess,
                lpBaseAddress: allocMem,
                lpBuffer: pBytes,
                nSize: (uint)dllPathBytes.Length,
                lpNumberOfBytesWritten: null))
            {
                // Writing memory failed, clean up and return false
                _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }
        }

        // Get the address of LoadLibraryW in kernel32.dll
        var hKernel32 = TerraFX.Interop.Windows.Windows.GetModuleHandleW("kernel32.dll".ToPCWSTR().Value);
        var loadLibraryAddr = TerraFX.Interop.Windows.Windows.GetProcAddress(
            hKernel32,
            (sbyte*)Marshal.StringToHGlobalAnsi("LoadLibraryW"));

        // Return false if the program couldn't get the address of LoadLibraryW
        if (loadLibraryAddr == null)
        {
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Transform the address to a callable function pointer
        var loadLibraryFunc = (delegate* unmanaged<void*, uint>)loadLibraryAddr;

        // Create a remote thread in the target process to execute LoadLibraryW with the DLL path
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
            // Thread creation failed, clean up and return false
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Wait with timeout
        uint waitResult = TerraFX.Interop.Windows.Windows.WaitForSingleObject(hThread, waitTimeoutMs);

        uint exitCode = 0;

        // Check the result of the wait operation
        if (waitResult == WAIT.WAIT_OBJECT_0)
        {
            _ = TerraFX.Interop.Windows.Windows.GetExitCodeThread(hThread, &exitCode);
        }

        // Handle timeout or failure
        else if (waitResult == WAIT.WAIT_TIMEOUT)
        {
            // Clean up handles and memory, but the remote thread may still be running inside LoadLibraryW.
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Other wait failures
        else
        {
            _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
            return false;
        }

        // Normal cleanup
        _ = TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
        _ = TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);

        return true;
    }
}