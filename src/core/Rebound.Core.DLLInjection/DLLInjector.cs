// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        Debug.WriteLine($"[DLLInjector] Starting injection process...");
        Debug.WriteLine($"[DLLInjector] Target processes: {(TargetProcesses.Count > 0 ? string.Join(", ", TargetProcesses) : "ALL")}");

        DLLInjectionAPI.InjectIntoExistingProcesses(_dllPath, TargetProcesses);
        Task.Run(() => DLLInjectionAPI.MonitorNewProcessesLoop(_dllPath, TargetProcesses));

        Debug.WriteLine($"[DLLInjector] Injection monitoring started");
    }
}


public static class DLLInjectionAPI
{
    private static ConcurrentDictionary<(int pid, string dllPath), int> InjectedProcessDlls = new();
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
        Debug.WriteLine("[Privilege] Attempting to enable SeDebugPrivilege...");
        error = null;
        HANDLE processToken;
        LUID luid;

        try
        {
            if (!TerraFX.Interop.Windows.Windows.OpenProcessToken(
                new((void*)Process.GetCurrentProcess().Handle),
                0x0020 | 0x0008,
                &processToken))
            {
                error = $"OpenProcessToken failed: {Marshal.GetLastWin32Error()}";
                Debug.WriteLine($"[Privilege] ERROR: {error}");
                return false;
            }

            try
            {
                if (!TerraFX.Interop.Windows.Windows.LookupPrivilegeValue(null, SE.SE_DEBUG_NAME.ToPCWSTR().Value, &luid))
                {
                    error = $"LookupPrivilegeValue failed: {Marshal.GetLastWin32Error()}";
                    Debug.WriteLine($"[Privilege] ERROR: {error}");
                    return false;
                }

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

                if (!TerraFX.Interop.Windows.Windows.AdjustTokenPrivileges(processToken, false, &tp, 0, null, null))
                {
                    error = $"AdjustTokenPrivileges failed: {Marshal.GetLastWin32Error()}";
                    Debug.WriteLine($"[Privilege] ERROR: {error}");
                    return false;
                }

                var last = Marshal.GetLastWin32Error();
                if (last != 0)
                {
                    error = $"AdjustTokenPrivileges succeeded but returned last error {last}";
                    Debug.WriteLine($"[Privilege] WARNING: {error}");
                    return false;
                }

                Debug.WriteLine("[Privilege] Successfully enabled SeDebugPrivilege");
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
            Debug.WriteLine($"[Privilege] EXCEPTION: {ex.Message}");
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
        if (proc == null)
        {
            Debug.WriteLine("[Inject] Process is null, skipping");
            return;
        }

        int pid = proc.Id;
        var key = (pid, dllPath);

        try
        {
            if (InjectedProcessDlls.ContainsKey(key))
            {
                Debug.WriteLine($"[Inject] PID {pid} with DLL {dllPath} already injected, skipping");
                return;
            }

            proc.Refresh();
            if (proc.HasExited)
            {
                Debug.WriteLine($"[Inject] PID {pid} has exited, skipping");
                return;
            }

            if (ExcludedProcesses.Contains(proc.ProcessName + ".exe"))
            {
                Debug.WriteLine($"[Inject] PID {pid} ({proc.ProcessName}) is excluded, skipping");
                return;
            }

            if (proc.SessionId == 0)
            {
                Debug.WriteLine($"[Inject] PID {pid} ({proc.ProcessName}) is in session 0 (system process), skipping");
                return;
            }

            if (!CanOpenProcess(pid))
            {
                Debug.WriteLine($"[Inject] Cannot open PID {pid} ({proc.ProcessName}), insufficient permissions");
                return;
            }

            if (!InjectedProcessDlls.TryAdd(key, 2))
            {
                Debug.WriteLine($"[Inject] PID {pid} with DLL {dllPath} injection already in progress, skipping");
                return;
            }

            Debug.WriteLine($"[Inject] Starting injection of DLL '{Path.GetFileName(dllPath)}' into PID {pid} ({proc.ProcessName})...");

            using var cts = new CancellationTokenSource(injectTimeoutMs);
            try
            {
                await InjectionSemaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InjectedProcessDlls.TryRemove(key, out _);
                Debug.WriteLine($"[Inject] Timed out waiting for semaphore slot for PID {pid} with DLL '{Path.GetFileName(dllPath)}'");
                return;
            }

            try
            {
                proc.Refresh();
                if (proc.HasExited)
                {
                    InjectedProcessDlls.TryRemove(key, out _);
                    Debug.WriteLine($"[Inject] PID {pid} exited while waiting for semaphore");
                    return;
                }

                if (Environment.Is64BitOperatingSystem)
                {
                    try
                    {
                        BOOL isWow64 = false;
                        unsafe
                        {
#pragma warning disable CS9123
                            _ = TerraFX.Interop.Windows.Windows.IsWow64Process(new((void*)proc.Handle), &isWow64);
#pragma warning restore CS9123
                        }
                        if (isWow64)
                        {
                            InjectedProcessDlls.TryRemove(key, out _);
                            Debug.WriteLine($"[Inject] PID {pid} is 32-bit on 64-bit OS, skipping");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Inject] Wow64 check warning for PID {pid}: {ex.Message}");
                    }
                }

                var injectTask = Task.Run(() =>
                {
                    try
                    {
                        return Inject((uint)pid, dllPath, (uint)injectTimeoutMs);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"[Inject] Injector threw exception for PID {pid}: {e.Message}");
                        return false;
                    }
                }, cts.Token);

                var finished = await Task.WhenAny(injectTask, Task.Delay(injectTimeoutMs, cts.Token)).ConfigureAwait(false);
                if (finished != injectTask)
                {
                    InjectedProcessDlls.TryRemove(key, out _);
                    Debug.WriteLine($"[Inject] Injection timed out for PID {pid} with DLL '{Path.GetFileName(dllPath)}'");
                    return;
                }

                bool success = false;
                try
                {
                    success = await injectTask.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"[Inject] Injection fault for PID {pid}: {e.Message}");
                }

                if (success)
                {
                    InjectedProcessDlls[key] = 0;
                    Debug.WriteLine($"[Inject] ✓ Successfully injected DLL '{Path.GetFileName(dllPath)}' into PID {pid} ({proc.ProcessName})");
                }
                else
                {
                    InjectedProcessDlls.TryRemove(key, out _);
                    Debug.WriteLine($"[Inject] ✗ Injection failed for PID {pid} ({proc.ProcessName}) with DLL '{Path.GetFileName(dllPath)}'");
                }
            }
            finally
            {
                InjectionSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Inject] InjectIntoProcessAsync error for PID {pid}: {ex.Message}");
            InjectedProcessDlls.TryRemove(key, out _);
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
        var procs = Process.GetProcesses();
        Debug.WriteLine($"[Scan] Scanning {procs.Length} processes...");

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
            Debug.WriteLine($"[Scan] Targeting specific processes: {string.Join(", ", normalizedTargets)}");
        }
        else
        {
            Debug.WriteLine("[Scan] No target filter specified - will attempt injection into all eligible processes");
        }

        List<Process> filteredProcs;
        if (normalizedTargets != null)
        {
            filteredProcs = new List<Process>();
            foreach (var proc in procs)
            {
                try
                {
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
            Debug.WriteLine($"[Scan] Filtered to {filteredProcs.Count} target processes");
        }
        else
        {
            filteredProcs = new List<Process>(procs);
        }

        foreach (var p in filteredProcs)
        {
            var capture = p;
            Task.Run(() =>
                InjectIntoProcessAsync(capture, dllPath).ContinueWith(t =>
                {
                    if (t.Exception != null)
                        Debug.WriteLine($"[Scan] Task exception: {t.Exception.Flatten().InnerException?.Message}");
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
        if (_monitorRunning)
        {
            Debug.WriteLine("[Monitor] Already running, skipping start.");
            return;
        }

        _monitorRunning = true;
        Debug.WriteLine("[Monitor] Starting Toolhelp-based process monitor...");

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
            Debug.WriteLine($"[Monitor] Tracking targets: {string.Join(", ", normalizedTargets)}");
        }
        else
        {
            Debug.WriteLine("[Monitor] No specific target processes specified, injecting into all.");
        }

        var seen = new HashSet<int>();
        while (true)
        {
            try
            {
                var pids = EnumerateProcessIds();

                foreach (var pid in pids)
                {
                    // Skip if this pid with this dllPath was already injected
                    if (InjectedProcessDlls.ContainsKey((pid, dllPath)))
                    {
                        seen.Add(pid);
                        continue;
                    }
                    if (seen.Contains(pid))
                        continue;

                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (proc.HasExited)
                        {
                            Debug.WriteLine($"[Monitor] Process {pid} has exited.");
                            seen.Add(pid);
                            continue;
                        }

                        if (normalizedTargets != null)
                        {
                            var procName = proc.ProcessName ?? string.Empty;
                            if (!normalizedTargets.Contains(procName))
                            {
                                seen.Add(pid);
                                continue;
                            }
                        }

                        Debug.WriteLine($"[Monitor] Attempting injection of DLL '{Path.GetFileName(dllPath)}' into process {pid} ({proc.ProcessName})...");
                        Task.Run(() => InjectIntoProcessAsync(proc, dllPath));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Monitor] Exception handling process {pid}: {ex.Message}");
                    }
                    finally
                    {
                        seen.Add(pid);
                    }
                }

                // Cleanup dead process+DLL injection entries
                var toRemove = new List<(int pid, string dll)>();
                foreach (var kvp in InjectedProcessDlls)
                {
                    int pid = kvp.Key.pid;
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (p.HasExited) toRemove.Add(kvp.Key);
                    }
                    catch
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in toRemove)
                {
                    Debug.WriteLine($"[Monitor] Cleaning up dead injected PID+DLL: {key.pid} + {Path.GetFileName(key.dll)}");
                    InjectedProcessDlls.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Monitor] Monitor loop error: {ex.Message}");
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
            snap = TerraFX.Interop.Windows.Windows.CreateToolhelp32Snapshot(0x00000002, 0); // TH32CS_SNAPPROCESS

            if (snap == IntPtr.Zero || snap == new IntPtr(-1))
            {
                return ret;
            }

            var pe = new PROCESSENTRY32W
            {
                dwSize = (uint)sizeof(PROCESSENTRY32W)
            };

            if (!TerraFX.Interop.Windows.Windows.Process32FirstW(snap, &pe))
            {
                return ret;
            }

            do
            {
                ret.Add((int)pe.th32ProcessID);
            }
            while (TerraFX.Interop.Windows.Windows.Process32NextW(snap, &pe));
        }
        catch (Exception ex)
        {
        }
        finally
        {
            if (snap != HANDLE.NULL && snap != HANDLE.INVALID_VALUE)
            {
                _ = TerraFX.Interop.Windows.Windows.CloseHandle(snap);
            }
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
        var hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(PROCESS_ALL_ACCESS, false, (uint)pid);
        if (hProcess == IntPtr.Zero)
        {
            Debug.WriteLine($"[CanOpenProcess] Cannot open process {pid}.");
            return false;
        }
        TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
        Debug.WriteLine($"[CanOpenProcess] Can open process {pid}.");
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
        Debug.WriteLine($"[Inject] Starting injection into PID {pid} with DLL '{dllPath}'");
        HANDLE hProcess = HANDLE.NULL;
        void* allocMem = null;
        HANDLE hThread = HANDLE.NULL;

        try
        {
            try
            {
                var proc = Process.GetProcessById((int)pid);

                BOOL isWow64 = false;
                TerraFX.Interop.Windows.Windows.IsWow64Process(new((void*)proc.Handle), &isWow64);

                if (File.Exists(dllPath))
                {
                    using var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
                    using var reader = new BinaryReader(fs);

                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peHeaderOffset = reader.ReadInt32();

                    fs.Seek(peHeaderOffset, SeekOrigin.Begin);
                    uint peSignature = reader.ReadUInt32();

                    if (peSignature == 0x00004550) // "PE\0\0"
                    {
                        ushort machine = reader.ReadUInt16();
                        string architecture = machine switch
                        {
                            0x014c => "x86 (32-bit)",
                            0x8664 => "x64 (64-bit)",
                            0xAA64 => "ARM64",
                            _ => $"Unknown (0x{machine:X4})"
                        };
                        Debug.WriteLine($"[Inject] DLL architecture: {architecture}, Target isWow64: {isWow64}");

                        if ((machine == 0x014c && !isWow64) || (machine == 0x8664 && isWow64))
                        {
                            Debug.WriteLine("[Inject] Architecture mismatch, cannot inject.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Inject] Exception checking architecture: {ex.Message}");
            }

            hProcess = TerraFX.Interop.Windows.Windows.OpenProcess(PROCESS_ALL_ACCESS, false, pid);

            if (hProcess == IntPtr.Zero)
            {
                Debug.WriteLine($"[Inject] Failed to open process {pid}.");
                return false;
            }

            var dllPathBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + "\0");
            allocMem = TerraFX.Interop.Windows.Windows.VirtualAllocEx(hProcess, null, (uint)dllPathBytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            if ((nint)allocMem == IntPtr.Zero)
            {
                Debug.WriteLine($"[Inject] VirtualAllocEx failed for process {pid}.");
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            bool writeSuccess;
            fixed (byte* pBytes = dllPathBytes)
            {
                writeSuccess = TerraFX.Interop.Windows.Windows.WriteProcessMemory(hProcess, allocMem, pBytes, (uint)dllPathBytes.Length, null);
            }

            if (!writeSuccess)
            {
                Debug.WriteLine($"[Inject] WriteProcessMemory failed for process {pid}.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            var hKernel32 = TerraFX.Interop.Windows.Windows.GetModuleHandleW("kernel32.dll".ToPCWSTR().Value);
            if (hKernel32 == IntPtr.Zero)
            {
                Debug.WriteLine("[Inject] GetModuleHandleW failed for kernel32.dll.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            var loadLibraryAddr = TerraFX.Interop.Windows.Windows.GetProcAddress(hKernel32, (sbyte*)Marshal.StringToHGlobalAnsi("LoadLibraryW").ToPointer());

            if (loadLibraryAddr == null)
            {
                Debug.WriteLine("[Inject] GetProcAddress failed for LoadLibraryW.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            var loadLibraryFunc = (delegate* unmanaged<void*, uint>)loadLibraryAddr;
            hThread = TerraFX.Interop.Windows.Windows.CreateRemoteThread(hProcess, null, 0, loadLibraryFunc, allocMem, 0, null);

            if (hThread == IntPtr.Zero)
            {
                Debug.WriteLine($"[Inject] CreateRemoteThread failed for process {pid}.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            uint waitResult = TerraFX.Interop.Windows.Windows.WaitForSingleObject(hThread, waitTimeoutMs);

            if (waitResult == WAIT.WAIT_TIMEOUT)
            {
                Debug.WriteLine($"[Inject] WaitForSingleObject timed out for process {pid}.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }
            else if (waitResult != WAIT.WAIT_OBJECT_0)
            {
                Debug.WriteLine($"[Inject] WaitForSingleObject returned unexpected code {waitResult} for process {pid}.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            uint exitCode;
            bool getExitSuccess = TerraFX.Interop.Windows.Windows.GetExitCodeThread(hThread, &exitCode);

            if (!getExitSuccess)
            {
                Debug.WriteLine($"[Inject] GetExitCodeThread failed for process {pid}.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            Debug.WriteLine($"[Inject] Remote thread exit code: {exitCode}");

            if (exitCode == 0)
            {
                Debug.WriteLine("[Inject] Remote thread exit code 0 indicates LoadLibraryW failed.");
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
                TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);
                return false;
            }

            TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);

            Debug.WriteLine($"[Inject] Successfully injected DLL into process {pid}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Inject] Exception during injection: {ex.Message}");
            if (allocMem != null && hProcess != IntPtr.Zero)
                TerraFX.Interop.Windows.Windows.VirtualFreeEx(hProcess, allocMem, 0, MEM_RELEASE);
            if (hThread != IntPtr.Zero)
                TerraFX.Interop.Windows.Windows.CloseHandle(hThread);
            if (hProcess != IntPtr.Zero)
                TerraFX.Interop.Windows.Windows.CloseHandle(hProcess);

            return false;
        }
    }
}