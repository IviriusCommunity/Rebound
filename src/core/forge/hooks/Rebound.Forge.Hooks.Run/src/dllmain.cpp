// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

#include "pch.h"
#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <shlobj.h>
#include <fstream>
#include <sstream>
#include <string>
#include <atomic>
#include "MinHook.h"

typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);

static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound.ServiceHost.exe";
static const wchar_t* REBOUND_SHELL_EXE = L"Rebound.Shell.exe";
static const wchar_t* REBOUND_SHELL_PIPE = L"\\\\.\\pipe\\REBOUND_SHELL";
static const wchar_t* REBOUND_DLL_INJECTION_PIPE = L"\\\\.\\pipe\\REBOUND_DLL_INJECTION";

enum class LifecycleState
{
    Starting,
    Running,
    Stopping,
    Stopped
};

static HMODULE g_moduleHandle = nullptr;
static HMODULE g_shell32 = nullptr;
static FARPROC g_hookTarget = nullptr;

static HANDLE g_shutdownEvent = nullptr;
static HANDLE g_initThread = nullptr;
static HANDLE g_monitorThread = nullptr;
static HANDLE g_pipeThread = nullptr;
static HANDLE g_shutdownThread = nullptr;
static HANDLE g_initMutex = nullptr;

static std::atomic<LifecycleState> g_state{
    LifecycleState::Starting
};

static std::atomic<bool> g_hooksInstalled{ false };
static bool g_ownsMinHookInitialization = false;

static HANDLE g_callbacksDrainedEvent = nullptr;
static std::atomic<unsigned long> g_activeCallbacks{ 0 };

static RunFileDlg_t g_originalRunFileDlg = nullptr;

static void RequestShutdown()
{
    LifecycleState state = g_state.load(std::memory_order_acquire);

    while (state != LifecycleState::Stopping &&
        state != LifecycleState::Stopped)
    {
        if (g_state.compare_exchange_weak(
            state,
            LifecycleState::Stopping,
            std::memory_order_acq_rel,
            std::memory_order_acquire))
        {
            break;
        }
    }

    if (state != LifecycleState::Stopped && g_shutdownEvent)
        SetEvent(g_shutdownEvent);
}

class HookCallGuard
{
public:
    HookCallGuard()
    {
        if (g_activeCallbacks.fetch_add(1, std::memory_order_acq_rel) == 0)
            ResetEvent(g_callbacksDrainedEvent);
    }

    ~HookCallGuard()
    {
        if (g_activeCallbacks.fetch_sub(1, std::memory_order_acq_rel) == 1)
            SetEvent(g_callbacksDrainedEvent);
    }

    HookCallGuard(const HookCallGuard&) = delete;
    HookCallGuard& operator=(const HookCallGuard&) = delete;
};

/// <summary>
/// Send a message to a named pipe server.
/// </summary>
/// <param name="pipeServer">
/// The address of the named pipe server.
/// </param>
/// <param name="message">
/// The message to be sent to the server.
/// </param>
/// <returns>
/// True if the message is sent. Otherwise false.
/// </returns>
static bool SendMessageToApp(const LPCWSTR& pipeServer, const std::wstring& message) {
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    int retries = 0;
    while (retries < 10)
    {
        hPipe = CreateFileW(pipeServer, GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);

        if (hPipe != INVALID_HANDLE_VALUE)
            break;

        if (GetLastError() != ERROR_PIPE_BUSY)
            return false;

        Sleep(50);
        retries++;
    }

    if (hPipe == INVALID_HANDLE_VALUE)
        return false;

    DWORD bytesWritten = 0;
    std::string utf8Message;

    int size_needed = WideCharToMultiByte(CP_UTF8, 0, message.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (size_needed <= 0)
    {
        CloseHandle(hPipe);
        return false;
    }
    utf8Message.resize(static_cast<size_t>(size_needed));

    if (WideCharToMultiByte(
        CP_UTF8, 0,
        message.c_str(), -1,
        utf8Message.data(), size_needed,
        nullptr, nullptr) == 0) 
    {
        CloseHandle(hPipe);
        return false;
    }

    utf8Message.pop_back(); // Remove the null terminator.
    utf8Message += '\n';

    WriteFile(hPipe, utf8Message.data(), static_cast<DWORD>(utf8Message.size()), &bytesWritten, nullptr);
    CloseHandle(hPipe);
    return true;
}

/// <summary>
/// Check if Rebound Shell is running in the background.
/// </summary>
static bool IsReboundShellRunning()
{
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) return false;

    PROCESSENTRY32 pe = {};
    pe.dwSize = sizeof(PROCESSENTRY32);
    bool found = false;

    if (Process32First(hSnapshot, &pe))
    {
        do
        {
            if (_wcsicmp(pe.szExeFile, REBOUND_SHELL_EXE) == 0)
            {
                found = true;
                break;
            }
        } while (Process32Next(hSnapshot, &pe));
    }

    CloseHandle(hSnapshot);
    return found;
}

/// <summary>
/// Finds a pricess by name and returns its PID.
/// </summary>
/// <param name="processName">
/// The process name to track down.
/// </param>
/// <returns>
/// A DWORD value containing the PID of the located process.
/// </returns>
static DWORD FindProcessByName(const wchar_t* processName) {
    DWORD processId = 0;
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    if (snapshot != INVALID_HANDLE_VALUE) {
        PROCESSENTRY32W entry{};
        entry.dwSize = sizeof(PROCESSENTRY32W);

        if (Process32FirstW(snapshot, &entry)) {
            do {
                if (_wcsicmp(processName, entry.szExeFile) == 0) {
                    processId = entry.th32ProcessID;
                    break;
                }
            } while (Process32NextW(snapshot, &entry));
        }
        CloseHandle(snapshot);
    }
    return processId;
}

/// <summary>
/// Continuously heck if Rebound Service Host is alive.
/// </summary>
static DWORD WINAPI MonitorInjectorThread(LPVOID)
{
    constexpr int checkIntervalMs = 1000;
    constexpr int missingThreshold = 3;

    int missingCount = 0;

    while (WaitForSingleObject(g_shutdownEvent, checkIntervalMs)
        == WAIT_TIMEOUT)
    {
        if (FindProcessByName(REBOUND_SERVICE_HOST_EXE) != 0)
        {
            missingCount = 0;
        }
        else if (++missingCount >= missingThreshold)
        {
            RequestShutdown();
            break;
        }
    }

    return 0;
}

static int WINAPI RunFileDlg_Hook(
    HWND hwnd, HICON icon, LPCWSTR path,
    LPCWSTR title, LPCWSTR prompt, UINT flags)
{
    HookCallGuard guard;

    if (g_state.load(std::memory_order_acquire) != LifecycleState::Running)
        return g_originalRunFileDlg
        ? g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags)
        : 0;

    if (IsReboundShellRunning())
    {
        SendMessageToApp(REBOUND_SHELL_PIPE, L"Shell::SpawnRunWindow##" + std::wstring(title ? title : L""));
        return 0;
    }
    else
    {
        if (g_originalRunFileDlg)
            return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
        return 0;
    }
}

/// <summary>
/// Install the RunFileDlg function hook.
/// </summary>
static bool InstallRunFileDlgHook() {
    g_shell32 = LoadLibraryW(L"shell32.dll");
    if (!g_shell32)
        return false;

    g_hookTarget = GetProcAddress(g_shell32, MAKEINTRESOURCEA(61));
    if (!g_hookTarget)
        return false;

    MH_STATUS status = MH_CreateHook(
        g_hookTarget,
        reinterpret_cast<void*>(&RunFileDlg_Hook),
        reinterpret_cast<void**>(&g_originalRunFileDlg));

    if (status != MH_OK)
        return false;

    status = MH_EnableHook(g_hookTarget);
    if (status != MH_OK) {
        MH_RemoveHook(g_hookTarget);
        g_originalRunFileDlg = nullptr;
        return false;
    }

    g_hooksInstalled.store(true);
    return true;
}

/// <summary>
/// Initialize API hooking in a separate thread.
/// </summary>
static DWORD WINAPI DeferredInitThread(LPVOID)
{
    if (g_state.load(std::memory_order_acquire) != LifecycleState::Starting)
        return 0;

    MH_STATUS status = MH_Initialize();

    if (status == MH_ERROR_ALREADY_INITIALIZED)
        status = MH_OK;

    if (status != MH_OK)
    {
        RequestShutdown();
        return 1;
    }

    // Shutdown may have been requested while initialization was running.
    if (g_state.load(std::memory_order_acquire) != LifecycleState::Starting)
        return 0;

    if (!InstallRunFileDlgHook())
    {
        RequestShutdown();
        return 1;
    }

    LifecycleState expected = LifecycleState::Starting;

    // If shutdown already started, leave cleanup to the coordinator.
    g_state.compare_exchange_strong(
        expected,
        LifecycleState::Running,
        std::memory_order_release,
        std::memory_order_relaxed);

    return 0;
}

/// <summary>
/// Listens to the Rebound DLL injection named pipe for an uninject
/// command and uninjects the DLL on trigger.
/// </summary>
static DWORD WINAPI PipeListenerThread(LPVOID)
{
    constexpr wchar_t pipeName[] =
        L"\\\\.\\pipe\\REBOUND_DLL_INJECTION";

    while (WaitForSingleObject(g_shutdownEvent, 0) == WAIT_TIMEOUT)
    {
        HANDLE pipe = CreateFileW(
            pipeName,
            GENERIC_READ,
            0,
            nullptr,
            OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED,
            nullptr);

        if (pipe == INVALID_HANDLE_VALUE)
        {
            if (GetLastError() == ERROR_PIPE_BUSY)
                WaitNamedPipeW(pipeName, 250);
            else
                WaitForSingleObject(g_shutdownEvent, 250);

            continue;
        }

        HANDLE readEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);

        if (!readEvent)
        {
            CloseHandle(pipe);
            break;
        }

        while (WaitForSingleObject(g_shutdownEvent, 0) == WAIT_TIMEOUT)
        {
            char buffer[512]{};
            DWORD bytesRead = 0;

            OVERLAPPED ov{};
            ov.hEvent = readEvent;

            ResetEvent(readEvent);

            BOOL success = ReadFile(
                pipe, buffer, sizeof(buffer) - 1, nullptr, &ov);

            if (!success)
            {
                DWORD error = GetLastError();

                if (error != ERROR_IO_PENDING)
                    break;

                HANDLE waits[] = { g_shutdownEvent, readEvent };

                DWORD result = WaitForMultipleObjects(
                    2, waits, FALSE, INFINITE);

                if (result == WAIT_OBJECT_0)
                {
                    CancelIoEx(pipe, &ov);
                    WaitForSingleObject(readEvent, INFINITE);

                    // Reap the cancelled operation.
                    GetOverlappedResult(pipe, &ov, &bytesRead, FALSE);
                    break;
                }

                if (result != WAIT_OBJECT_0 + 1 ||
                    !GetOverlappedResult(pipe, &ov, &bytesRead, FALSE))
                {
                    break;
                }
            }
            else
            {
                // Immediate completion of the overlapped operation.
                if (!GetOverlappedResult(pipe, &ov, &bytesRead, FALSE))
                    break;
            }

            if (bytesRead == 0)
                break;

            buffer[bytesRead] = '\0';
            std::string msg(buffer);

            const size_t lastNonWs =
                msg.find_last_not_of("\r\n \t");

            if (lastNonWs == std::string::npos)
                msg.clear();
            else
                msg.erase(lastNonWs + 1);

            if (msg.find("UNINJECT") != std::string::npos &&
                msg.find("Rebound.Forge.Hooks.Run.dll") != std::string::npos)
            {
                RequestShutdown();
                break;
            }
        }

        // Cancel any outstanding read before closing the pipe.
        CancelIoEx(pipe, nullptr);
        CloseHandle(readEvent);
        CloseHandle(pipe);
    }

    return 0;
}

static bool WaitForCallbacksToDrain()
{
    while (g_activeCallbacks.load(std::memory_order_acquire) != 0)
    {
        if (WaitForSingleObject(g_callbacksDrainedEvent, INFINITE)
            != WAIT_OBJECT_0)
        {
            return false;
        }
    }

    return true;
}

static void WaitAndCloseThread(HANDLE& thread)
{
    if (!thread)
        return;

    WaitForSingleObject(thread, INFINITE);
    CloseHandle(thread);
    thread = nullptr;
}

static DWORD WINAPI ShutdownCoordinatorThread(LPVOID)
{
    g_initThread = CreateThread(
        nullptr, 0, DeferredInitThread, nullptr, 0, nullptr);

    g_monitorThread = CreateThread(
        nullptr, 0, MonitorInjectorThread, nullptr, 0, nullptr);

    g_pipeThread = CreateThread(
        nullptr, 0, PipeListenerThread, nullptr, 0, nullptr);

    if (!g_initThread || !g_monitorThread || !g_pipeThread)
        RequestShutdown();

    WaitForSingleObject(g_shutdownEvent, INFINITE);

    // Stop and join every worker before removing the hook.
    WaitAndCloseThread(g_initThread);
    WaitAndCloseThread(g_monitorThread);
    WaitAndCloseThread(g_pipeThread);

    if (g_hooksInstalled.load(std::memory_order_acquire))
    {
        MH_STATUS status = MH_DisableHook(g_hookTarget);

        if (status != MH_OK && status != MH_ERROR_DISABLED)
        {
            OutputDebugStringW(
                L"Rebound Run hook: failed to disable; retaining DLL.\n");
            return 1;
        }

        if (!WaitForCallbacksToDrain())
            return 1;

        status = MH_RemoveHook(g_hookTarget);

        if (status != MH_OK)
        {
            OutputDebugStringW(
                L"Rebound Run hook: failed to remove; retaining DLL.\n");
            return 1;
        }

        g_hooksInstalled.store(false, std::memory_order_release);
        g_originalRunFileDlg = nullptr;
    }

    if (g_shell32)
    {
        FreeLibrary(g_shell32);
        g_shell32 = nullptr;
    }

    g_state.store(LifecycleState::Stopped, std::memory_order_release);

    if (g_initMutex)
    {
        CloseHandle(g_initMutex);
        g_initMutex = nullptr;
    }

    CloseHandle(g_callbacksDrainedEvent);
    g_callbacksDrainedEvent = nullptr;

    CloseHandle(g_shutdownEvent);
    g_shutdownEvent = nullptr;

    if (g_shutdownThread)
    {
        CloseHandle(g_shutdownThread);
        g_shutdownThread = nullptr;
    }

    FreeLibraryAndExitThread(g_moduleHandle, 0);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hinstDLL);

        g_moduleHandle = hinstDLL;

        wchar_t mutexName[256]{};
        swprintf_s(
            mutexName,
            L"ReboundShell_RunHook_%lu",
            GetCurrentProcessId());

        g_initMutex = CreateMutexW(nullptr, FALSE, mutexName);

        if (!g_initMutex)
            return FALSE;

        if (GetLastError() == ERROR_ALREADY_EXISTS)
        {
            CloseHandle(g_initMutex);
            g_initMutex = nullptr;
            return FALSE;
        }

        g_shutdownEvent = CreateEventW(
            nullptr, TRUE, FALSE, nullptr);

        g_callbacksDrainedEvent = CreateEventW(
            nullptr, TRUE, TRUE, nullptr);

        if (!g_shutdownEvent || !g_callbacksDrainedEvent)
        {
            if (g_shutdownEvent)
                CloseHandle(g_shutdownEvent);

            if (g_callbacksDrainedEvent)
                CloseHandle(g_callbacksDrainedEvent);

            CloseHandle(g_initMutex);

            g_shutdownEvent = nullptr;
            g_callbacksDrainedEvent = nullptr;
            g_initMutex = nullptr;

            return FALSE;
        }

        g_shutdownThread = CreateThread(
            nullptr,
            0,
            ShutdownCoordinatorThread,
            nullptr,
            0,
            nullptr);

        if (!g_shutdownThread)
        {
            CloseHandle(g_callbacksDrainedEvent);
            CloseHandle(g_shutdownEvent);
            CloseHandle(g_initMutex);

            g_callbacksDrainedEvent = nullptr;
            g_shutdownEvent = nullptr;
            g_initMutex = nullptr;

            return FALSE;
        }
    }

    // No hook operations, waits, or worker-thread cleanup under loader lock.
    return TRUE;
}