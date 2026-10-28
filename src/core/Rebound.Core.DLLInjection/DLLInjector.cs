// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Memory;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.Threading;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CS0436 // Type conflicts with imported type

namespace Rebound.Core.DLLInjection;

/// <summary>
/// Initializes a new instance of the DLLInjector class with the specified DLL file path.
/// </summary>
/// <param name="dllPath">The full path to the DLL file to be injected. Must not be null or empty.</param>
public class DLLInjector(string dllPath) : IDisposable
{
    readonly string _dllPath = dllPath;
    readonly Lock _gate = new();
    CancellationTokenSource? _cts;

    /// <summary>
    /// Contains the names of processes that are targeted for monitoring or interaction.
    /// </summary>
    /// <remarks>Add process names to this list to specify which processes should be included in the
    /// operation. Use "*" to target all processes. The list is initialized as empty and can be modified at runtime.
    /// If the list is left empty, all processes will be targeted.</remarks>
    public Collection<string> TargetProcesses { get; } = [];

    /// <summary>
    /// Contains the names of processes that are excluded from injection.
    /// </summary>
    /// <remarks>Add process names to this list to specify which processes should be excluded from injection.
    /// The list is initialized as empty and can be modified at runtime.
    /// If the list is left empty, no processes will be excluded.</remarks>
    public Collection<string> ExcludedProcesses { get; } = [];

    /// <summary>
    /// Starts the DLL injection process for existing and newly launched target processes.
    /// Calling this more than once is harmless.
    /// </summary>
    public void StartInjection()
    {
        CancellationToken token;

        lock (_gate)
        {
            if (_cts is not null)
                return;

            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }

        // The monitor loop's first pass covers processes that already exist.
        _ = Task.Factory.StartNew(
            () => DLLInjectionAPI.MonitorNewProcessesLoop(
                _dllPath, 
                TargetProcesses, 
                ExcludedProcesses, 
                100, 
                token),
            token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Stops monitoring for new processes for this DLL.
    /// </summary>
    public void StopInjection()
    {
        CancellationTokenSource? cts;

        lock (_gate)
        {
            cts = _cts;
            _cts = null;
        }

        // Not disposed on purpose: the monitor loop may still be observing the token.
        cts?.Cancel();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts?.Dispose();
            _cts = null;
        }
    }
}

public static class DLLInjectionAPI
{
    private enum InjectResult
    {
        Success,
        AlreadyLoaded,
        Retry,
        Skip,
        Exited
    }

    // One claim per (process, DLL), so the initial scan, the monitor loop and
    // retry workers can never inject the same process twice.
    private static readonly ConcurrentDictionary<(uint pid, string dllPath), byte> InjectionClaims = new();

    // One live monitor per DLL (not one for the whole process).
    private static readonly ConcurrentDictionary<string, byte> ActiveMonitors = new(StringComparer.OrdinalIgnoreCase);

    private static readonly SemaphoreSlim InjectionSemaphore = new(8);

    private static readonly HashSet<string> DefaultExcludedProcesses =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "csrss.exe", "winlogon.exe", "smss.exe", "lsass.exe", "dwm.exe", "services.exe",
        Path.GetFileName(System.Environment.ProcessPath ?? "Rebound.ServiceHost.exe")
    ];

    private readonly record struct ProcessEntry(int Pid, string ExeName);

    #region Target / exclusion helpers

    private static HashSet<string> BuildExclusionSet(IEnumerable<string>? userExcludedProcesses)
    {
        var exclusions = new HashSet<string>(DefaultExcludedProcesses, StringComparer.OrdinalIgnoreCase);
        if (userExcludedProcesses != null)
        {
            foreach (var proc in userExcludedProcesses)
            {
                if (string.IsNullOrWhiteSpace(proc)) continue;
                var trimmed = proc.Trim();
                if (!trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    trimmed += ".exe";
                exclusions.Add(trimmed);
            }
        }
        return exclusions;
    }

    private static HashSet<string>? NormalizeTargets(IEnumerable<string>? targetProcesses)
    {
        if (targetProcesses == null)
            return null;

        HashSet<string>? normalized = null;

        foreach (var t in targetProcesses)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;

            normalized ??= [with(StringComparer.OrdinalIgnoreCase)];

            var n = t.Trim();
            if (n == "*")
            {
                normalized.Add("*");
                break;
            }
            if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                n = n[..^4];
            normalized.Add(n);
        }

        return normalized;
    }

    private static bool IsTargetProcess(string processName, HashSet<string>? normalizedTargets)
    {
        // If targets are null, empty, or contain "*", everything matches.
        if (normalizedTargets == null || normalizedTargets.Count == 0 || normalizedTargets.Contains("*"))
            return true;

        if (string.IsNullOrEmpty(processName))
            return false;

        var cleanName = processName;
        if (cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            cleanName = cleanName[..^4];

        return normalizedTargets.Contains(cleanName) || normalizedTargets.Contains(processName);
    }

    #endregion

    /// <summary>
    /// Attempts to enable the SeDebugPrivilege for the current process.
    /// </summary>
    public static unsafe bool TryEnableSeDebugPrivilege(out string? error)
    {
        error = null;

        try
        {
            // The Process object owns its handle; it must not be wrapped in a second owning handle.
            using var self = Process.GetCurrentProcess();

            if (!PInvoke.OpenProcessToken(
                self.SafeHandle,
                TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY,
                out var processToken))
            {
                error = $"OpenProcessToken failed: {Marshal.GetLastWin32Error()}";
                return false;
            }

            try
            {
                if (!PInvoke.LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
                {
                    error = $"LookupPrivilegeValue failed: {Marshal.GetLastWin32Error()}";
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
                            Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
                        }
                    }
                };

                if (!PInvoke.AdjustTokenPrivileges(processToken, false, &tp))
                {
                    error = $"AdjustTokenPrivileges failed: {Marshal.GetLastWin32Error()}";
                    return false;
                }

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
                processToken.Close();
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    #region Scanning and monitoring

    /// <summary>
    /// Cheap process list (one Toolhelp snapshot, no Process objects).
    /// </summary>
    private static unsafe List<ProcessEntry> EnumerateProcesses()
    {
        var ret = new List<ProcessEntry>();
        HANDLE snap = HANDLE.Null;
        try
        {
            snap = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);

            if ((nint)snap is 0 or -1)
                return ret;

            var pe = new PROCESSENTRY32W { dwSize = (uint)sizeof(PROCESSENTRY32W) };

            if (!PInvoke.Process32FirstW(snap, &pe))
                return ret;

            do ret.Add(new ProcessEntry((int)pe.th32ProcessID, pe.szExeFile.ToString()));
            while (PInvoke.Process32NextW(snap, &pe));
        }
        finally
        {
            if (snap != HANDLE.Null)
                _ = PInvoke.CloseHandle(snap);
        }
        return ret;
    }

    private static void ReleaseClaims(int pid)
    {
        foreach (var key in InjectionClaims.Keys)
            if (key.pid == pid)
                InjectionClaims.TryRemove(key, out _);
    }

    /// <summary>
    /// One pass over the process list. Every process not seen before gets its own retrying worker.
    /// </summary>
    private static void ScanOnce(
        string dllPath,
        HashSet<string>? targets,
        HashSet<string> exclusions,
        Dictionary<int, string> tracked,
        bool discoveredAfterStart,
        CancellationToken ct)
    {
        var snapshot = EnumerateProcesses();
        var alive = new HashSet<int>(snapshot.Count);

        foreach (var entry in snapshot)
            alive.Add(entry.Pid);

        // Forget processes that are gone so a recycled PID is treated as a new process.
        foreach (var pid in tracked.Keys.Where(p => !alive.Contains(p)).ToList())
        {
            tracked.Remove(pid);
            InjectionClaims.TryRemove(((uint)pid, dllPath), out _);
        }

        foreach (var entry in snapshot)
        {
            if (entry.Pid <= 4 || entry.Pid == System.Environment.ProcessId)
                continue;

            if (tracked.ContainsKey(entry.Pid))
                continue;

            tracked[entry.Pid] = entry.ExeName;

            if (exclusions.Contains(entry.ExeName) || !IsTargetProcess(entry.ExeName, targets))
                continue;

            int capturedPid = entry.Pid;
            string capturedName = entry.ExeName;

            _ = Task.Run(
                () => InjectWithRetryAsync((uint)capturedPid, capturedName, dllPath, discoveredAfterStart, ct),
                ct);
        }
    }

    /// <summary>
    /// Initiates DLL injection into all currently running processes using the specified DLL path.
    /// </summary>
    public static void InjectIntoExistingProcesses(string dllPath, Collection<string>? targetProcesses, Collection<string>? excludedProcesses)
    {
        ScanOnce(
            dllPath,
            NormalizeTargets(targetProcesses),
            BuildExclusionSet(excludedProcesses),
            [],
            discoveredAfterStart: false,
            CancellationToken.None);
    }

    /// <summary>
    /// Continuously monitors for newly started processes and injects the specified DLL.
    /// The first pass also covers processes that already exist.
    /// </summary>
    public static void MonitorNewProcessesLoop(
        string dllPath,
        Collection<string> targetProcesses,
        Collection<string> excludedProcesses,
        int pollMs = 100,
        CancellationToken cancellationToken = default)
    {
        if (!ActiveMonitors.TryAdd(dllPath, 0))
        {
            ReboundLogger.WriteToLog(
                "DLL injection monitor",
                $"Already monitoring for '{Path.GetFileName(dllPath)}', skipping start.",
                LogMessageSeverity.Message);
            return;
        }

        try
        {
            var exclusions = BuildExclusionSet(excludedProcesses);
            var targets = NormalizeTargets(targetProcesses);
            var tracked = new Dictionary<int, string>();
            bool firstPass = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ScanOnce(dllPath, targets, exclusions, tracked, discoveredAfterStart: !firstPass, cancellationToken);
                    firstPass = false;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ReboundLogger.WriteToLog(
                        "DLL injection monitor",
                        "Process scan failed.",
                        LogMessageSeverity.Message,
                        ex);
                }

                if (cancellationToken.WaitHandle.WaitOne(pollMs))
                    break;
            }
        }
        finally
        {
            ActiveMonitors.TryRemove(dllPath, out _);

            foreach (var key in InjectionClaims.Keys)
                if (string.Equals(key.dllPath, dllPath, StringComparison.OrdinalIgnoreCase))
                    InjectionClaims.TryRemove(key, out _);
        }
    }

    #endregion

    #region Injection

    /// <summary>
    /// Injects into one process, retrying while it is still starting up.
    /// Brand-new processes get a long retry window; processes that were already running get a few tries.
    /// </summary>
    private static async Task InjectWithRetryAsync(
        uint pid, string exeName, string dllPath, bool isNewProcess, CancellationToken ct)
    {
        var key = (pid, dllPath);

        if (!InjectionClaims.TryAdd(key, 0))
            return;

        int maxAttempts = isNewProcess ? 100 : 3;
        var delay = TimeSpan.FromMilliseconds(isNewProcess ? 100 : 500);
        string? lastReason = null;

        try
        {
            for (int attempt = 1; attempt <= maxAttempts && !ct.IsCancellationRequested; attempt++)
            {
                InjectResult result;
                string? reason;

                await InjectionSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    (result, reason) = TryInjectOnce(pid, dllPath, 5_000);
                }
                finally
                {
                    InjectionSemaphore.Release();
                }

                switch (result)
                {
                    case InjectResult.Success:
                        ReboundLogger.WriteToLog(
                            "DLL injection",
                            $"Injected '{Path.GetFileName(dllPath)}' into {exeName} ({pid}) on attempt {attempt}.",
                            LogMessageSeverity.Message);
                        return;

                    case InjectResult.AlreadyLoaded:
                        return;

                    case InjectResult.Exited:
                        InjectionClaims.TryRemove(key, out _);
                        return;

                    case InjectResult.Skip:
                        ReboundLogger.WriteToLog(
                            "DLL injection",
                            $"Skipped {exeName} ({pid}): {reason}",
                            LogMessageSeverity.Message);
                        return;
                }

                lastReason = reason;
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }

            if (!ct.IsCancellationRequested && isNewProcess)
            {
                ReboundLogger.WriteToLog(
                    "DLL injection",
                    $"Gave up on {exeName} ({pid}) after {maxAttempts} attempts: {lastReason}",
                    LogMessageSeverity.Message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLL injection",
                $"Injection into {exeName} ({pid}) failed with exception.",
                LogMessageSeverity.Message,
                ex);
        }
    }

    /// <summary>
    /// Attempts to asynchronously inject a DLL into the specified process.
    /// </summary>
    public static async Task InjectIntoProcessAsync(Process proc, string dllPath, int injectTimeoutMs = 10_000)
    {
        if (proc == null)
            return;

        int pid = proc.Id;
        string exeName;

        try
        {
            exeName = proc.ProcessName + ".exe";
        }
        catch
        {
            exeName = pid.ToString(CultureInfo.CurrentCulture);
        }

        if (DefaultExcludedProcesses.Contains(exeName))
            return;

        using var cts = new CancellationTokenSource(injectTimeoutMs);
        await InjectWithRetryAsync((uint)pid, exeName, dllPath, isNewProcess: false, cts.Token).ConfigureAwait(false);
    }

    private static (InjectResult Result, string? Reason) TryInjectOnce(
        uint pid, string dllPath, uint timeoutMs)
    {
        var raw = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_THREAD
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE
                | PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION,
            false,
            pid);

        if (raw == 0)
        {
            int error = Marshal.GetLastWin32Error();
            return (InjectResult.Retry, $"OpenProcess failed (error {error})");
        }

        using var hProcess = new SafeProcessHandle(raw, true);

        if (!PInvoke.GetExitCodeProcess(hProcess, out var exitCode))
        {
            int error = Marshal.GetLastWin32Error();
            return (InjectResult.Retry, $"GetExitCodeProcess failed (error {error})");
        }

        if (exitCode != 259) // STILL_ACTIVE;
            return (InjectResult.Exited, null);

        if (PInvoke.ProcessIdToSessionId(pid, out var sessionId))
        {
            if (sessionId == 0)
                return (InjectResult.Skip, "session 0 process");
        }

        ushort targetMachine = GetTargetMachine(hProcess);
        ushort dllMachine = ReadDllMachine(dllPath);

        if (targetMachine != 0 && dllMachine != 0 &&
            targetMachine != dllMachine)
        {
            string reason =
                $"architecture mismatch (process 0x{targetMachine:X4}, DLL 0x{dllMachine:X4})";
            return (InjectResult.Skip, reason);
        }

        var modules = TryGetModuleNames(hProcess);

        if (modules is null)
            return (InjectResult.Retry, "process module list unavailable");

        if (modules.Contains(Path.GetFileName(dllPath)))
            return (InjectResult.AlreadyLoaded, null);

        if (!modules.Contains("kernel32.dll"))
            return (InjectResult.Retry, "kernel32 is not loaded yet");

        // Before injecting the desired DLL, the MinHook-Detours runtime
        // must be injected first in order for the DLL in question to load.
        var libPath = Path.Combine(Path.GetDirectoryName(dllPath)!,
            RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "minhook-detours.x64.Release.dll" :
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "minhook-detours.ARM64.Release.dll" : 
            throw new NotImplementedException());

        var (okLib, errorReasonLib) = InjectCore(hProcess, libPath, timeoutMs);

        if (!okLib)
            return (InjectResult.Retry, errorReasonLib);

        // After injecting the required lib, inject the requested DLL itself
        var (ok, errorReason) = InjectCore(hProcess, dllPath, timeoutMs);

        return ok
            ? (InjectResult.Success, null)
            : (InjectResult.Retry, errorReason);
    }

    private static ushort GetTargetMachine(SafeHandle hProcess)
    {
        try
        {
            if (!PInvoke.IsWow64Process2(hProcess, out var processMachine, out var nativeMachine))
                return 0;

            // processMachine is 0 (IMAGE_FILE_MACHINE_UNKNOWN) when the process runs natively.
            return (ushort)(processMachine != 0 ? processMachine : nativeMachine);
        }
        catch
        {
            return 0;
        }
    }

    private static ushort ReadDllMachine(string dllPath)
    {
        try
        {
            using var fs = new FileStream(
                dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new BinaryReader(fs);

            fs.Seek(0x3C, SeekOrigin.Begin);
            int peHeaderOffset = reader.ReadInt32();

            fs.Seek(peHeaderOffset, SeekOrigin.Begin);

            if (reader.ReadUInt32() != 0x00004550) // "PE\0\0"
                return 0;

            return reader.ReadUInt16();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// File names of every module loaded in the process, or null if the list
    /// cannot be read yet (typical for a process that is still being created).
    /// </summary>
    private unsafe static HashSet<string>? TryGetModuleNames(SafeHandle hProcess)
    {
        using var handle = new SafeProcessHandle(hProcess.DangerousGetHandle(), false);
        uint pid = PInvoke.GetProcessId(handle);

        Span<nint> modules = stackalloc nint[64];

        for (int pass = 0; pass < 2; pass++)
        {
            if (!PInvoke.EnumProcessModulesEx(
                hProcess,
                MemoryMarshal.AsBytes(modules),
                out var needed,
                ENUM_PROCESS_MODULES_EX_FLAGS.LIST_MODULES_ALL))
            {
                int error = Marshal.GetLastWin32Error();
                return null;
            }

            int count = checked((int)(needed / (uint)sizeof(nint)));

            if (count == 0)
                return null;

            if (count > modules.Length)
            {
                if (pass == 1)
                    return null;

                modules = new nint[count + 32];
                continue;
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            char[] buffer = new char[2048];

            fixed (char* pBuffer = buffer)
            {
                for (int i = 0; i < count; i++)
                {
                    uint length = PInvoke.GetModuleFileNameEx(
                        new(hProcess.DangerousGetHandle()),
                        (HMODULE)modules[i],
                        new PWSTR(pBuffer),
                        (uint)buffer.Length);

                    if (length == 0)
                        continue;

                    if (length >= buffer.Length)
                        continue;

                    string fullPath = new(buffer, 0, (int)length);
                    string name = Path.GetFileName(fullPath);
                    names.Add(name);
                }
            }

            return names;
        }

        return null;
    }

    private static unsafe (bool Ok, string? Error) InjectCore(
        SafeProcessHandle hProcess, string dllPath, uint waitTimeoutMs)
    {
        using var kernel32 = PInvoke.GetModuleHandle("kernel32.dll");
        nint loadLibraryW = kernel32.IsInvalid ? 0 : PInvoke.GetProcAddress(kernel32, "LoadLibraryW");

        if (loadLibraryW == 0)
            return (false, "LoadLibraryW address not found");

        uint pid = PInvoke.GetProcessId(hProcess);

        ReadOnlySpan<byte> dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");

        var allocMem = PInvoke.VirtualAllocEx(
            hProcess, null, (uint)dllPathBytes.Length,
            VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
            PAGE_PROTECTION_FLAGS.PAGE_READWRITE);

        if ((nint)allocMem == 0)
            return (false, "VirtualAllocEx failed");

        if (!PInvoke.WriteProcessMemory(hProcess, allocMem, dllPathBytes))
        {
            PInvoke.VirtualFreeEx(hProcess, allocMem, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            return (false, "WriteProcessMemory failed");
        }

        var loadLibraryFunc = (delegate* unmanaged[Stdcall]<void*, uint>)loadLibraryW;
        using var hThread = PInvoke.CreateRemoteThread(hProcess, null, 0, loadLibraryFunc, allocMem, 0);

        if (hThread.IsInvalid)
        {
            PInvoke.VirtualFreeEx(hProcess, allocMem, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            return (false, "CreateRemoteThread failed");
        }

        var waitResult = PInvoke.WaitForSingleObject(hThread, waitTimeoutMs);

        if (waitResult == WAIT_EVENT.WAIT_TIMEOUT)
        {
            // The remote thread may still be reading the path, so the buffer is
            // deliberately NOT freed. A late success is picked up by the
            // "already loaded" check on the next attempt.
            return (false, "remote LoadLibraryW timed out");
        }

        if (waitResult != WAIT_EVENT.WAIT_OBJECT_0)
        {
            PInvoke.VirtualFreeEx(hProcess, allocMem, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            return (false, $"WaitForSingleObject returned {waitResult}");
        }

        if (!PInvoke.GetExitCodeThread(hThread, out var threadExitCode))
        {
            var error = Marshal.GetLastWin32Error();
            PInvoke.VirtualFreeEx(hProcess, allocMem, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            return (false, $"GetExitCodeThread failed: {error}");
        }

        PInvoke.VirtualFreeEx(
            hProcess, allocMem, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);

        ReboundLogger.WriteToLog(
            "DLL injection",
            $"Remote LoadLibraryW completed: PID={pid}, " +
            $"threadExitCode=0x{threadExitCode:X8}",
            LogMessageSeverity.Message);

        return threadExitCode == 0
            ? (false, "remote LoadLibraryW returned 0")
            : (true, null);
    }

    /// <summary>
    /// Injects the DLL into a single process by id (single attempt, no retries).
    /// </summary>
    public static bool Inject(uint pid, string dllPath, uint waitTimeoutMs = 10_000)
    {
        try
        {
            var (result, _) = TryInjectOnce(pid, dllPath, waitTimeoutMs);
            return result is InjectResult.Success or InjectResult.AlreadyLoaded;
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "DLL injector",
                "Exception during injection.",
                LogMessageSeverity.Message,
                ex);
            return false;
        }
    }

    public static bool CanOpenProcess(int pid)
    {
        nint handle = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_THREAD
            | PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION
            | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ
            | PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE
            | PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, (uint)pid);

        if (handle == 0)
            return false;

        _ = PInvoke.CloseHandle((HANDLE)handle);
        return true;
    }

    #endregion
}