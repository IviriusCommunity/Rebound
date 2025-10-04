#include "pch.h"
#include <windows.h>
#include <stdint.h>
#include <process.h>
#include <stdio.h>
#include "MinHook.h"
#include <string>
#include <tlhelp32.h>
#include <shlobj.h>
#include <fstream>
#include <sstream>

// --------------------------------------------
// Typedefs
// --------------------------------------------
#ifdef _M_X64
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#else
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#endif

// Add near the top (optional) - process name to watch for
static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound Service Host.exe";

// Original function pointer
static RunFileDlg_t g_originalRunFileDlg = nullptr;

// Thread handles we keep so we can wait for them during cleanup
static HANDLE g_workerThread = nullptr;
static HANDLE g_hookThread = nullptr;

// store module handle for safe unloading
static HMODULE g_moduleHandle = nullptr;

// --------------------------------------------
// Forward declarations
// --------------------------------------------
DWORD WINAPI WorkerThreadStarter(LPVOID lpVoid);
DWORD WINAPI HookThread(LPVOID hModule);
bool InstallRunFileDlgHook();
int WINAPI RunFileDlg_Hook(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);

// --------------------------------------------
// Settings Helper - Simple XML parser
// --------------------------------------------
bool GetBoolSetting(const wchar_t* key, const wchar_t* appName, bool defaultValue) {
    wchar_t localAppData[MAX_PATH];
    if (FAILED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, 0, localAppData))) {
        return defaultValue;
    }

    std::wstring filePath = std::wstring(localAppData) + L"\\Rebound\\" + appName + L".xml";

    // Check if file exists
    if (GetFileAttributesW(filePath.c_str()) == INVALID_FILE_ATTRIBUTES) {
        return defaultValue;
    }

    // Read file content
    std::wifstream file(filePath);
    if (!file.is_open()) {
        return defaultValue;
    }

    std::wstringstream buffer;
    buffer << file.rdbuf();
    std::wstring content = buffer.str();
    file.close();

    // Simple XML parsing - look for <key>value</key>
    std::wstring openTag = std::wstring(L"<") + key + L">";
    std::wstring closeTag = std::wstring(L"</") + key + L">";

    size_t startPos = content.find(openTag);
    if (startPos == std::wstring::npos) {
        return defaultValue;
    }

    startPos += openTag.length();
    size_t endPos = content.find(closeTag, startPos);
    if (endPos == std::wstring::npos) {
        return defaultValue;
    }

    std::wstring value = content.substr(startPos, endPos - startPos);

    // Trim whitespace
    value.erase(0, value.find_first_not_of(L" \t\n\r"));
    value.erase(value.find_last_not_of(L" \t\n\r") + 1);

    // Parse boolean
    if (_wcsicmp(value.c_str(), L"true") == 0 || value == L"1") {
        return true;
    }
    else if (_wcsicmp(value.c_str(), L"false") == 0 || value == L"0") {
        return false;
    }

    return defaultValue;
}

// --------------------------------------------
// Message queue for asynchronous pipe sending
// --------------------------------------------
struct MsgNode {
    SLIST_ENTRY link;
    wchar_t msg[256];
};

static volatile bool g_running = true;

static SLIST_HEADER g_messageList;
static HANDLE g_workerEvent = nullptr;

std::string WideToUtf8(const wchar_t* wstr) {
    if (!wstr) return {};
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, nullptr, 0, nullptr, nullptr);
    if (size_needed <= 0) return {};
    std::string result(size_needed - 1, 0);
    WideCharToMultiByte(CP_UTF8, 0, wstr, -1, result.data(), size_needed, nullptr, nullptr);
    return result;
}

bool SendMessageToApp(const std::wstring& message)
{
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    // Try to connect to the named pipe, retry if not yet available
    while (true)
    {
        hPipe = CreateFileW(
            L"\\\\.\\pipe\\REBOUND_SHELL",  // Pipe name
            GENERIC_WRITE,
            0,
            nullptr,
            OPEN_EXISTING,
            0,
            nullptr
        );

        if (hPipe != INVALID_HANDLE_VALUE)
            break;

        if (GetLastError() != ERROR_PIPE_BUSY)
            return false; // pipe unavailable

        // wait 50ms before retry
        Sleep(50);
    }

    DWORD bytesWritten = 0;
    std::string utf8Message;

    // convert UTF-16 to UTF-8
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, message.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (size_needed <= 0)
    {
        CloseHandle(hPipe);
        return false;
    }
    utf8Message.resize(size_needed - 1); // exclude null terminator
    WideCharToMultiByte(CP_UTF8, 0, message.c_str(), -1, utf8Message.data(), size_needed, nullptr, nullptr);

    // append newline
    utf8Message += "\n";

    WriteFile(hPipe, utf8Message.data(), static_cast<DWORD>(utf8Message.size()), &bytesWritten, nullptr);
    CloseHandle(hPipe);
    return true;
}

DWORD WINAPI WorkerThreadLoop(LPVOID) {
    HANDLE pipe = INVALID_HANDLE_VALUE;

    while (g_running) {
        WaitForSingleObject(g_workerEvent, INFINITE);

        if (!g_running) break;

        PSLIST_ENTRY entry;
        while ((entry = InterlockedPopEntrySList(&g_messageList)) != nullptr) {
            auto node = CONTAINING_RECORD(entry, MsgNode, link);

            // ensure UTF-8 conversion with newline
            std::string utf8 = WideToUtf8(node->msg);
            utf8 += "\n";

            // lazy pipe connect with retry
            while (pipe == INVALID_HANDLE_VALUE && g_running) {
                pipe = CreateFileA("\\\\.\\pipe\\REBOUND_SERVICE_HOST", GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
                if (pipe == INVALID_HANDLE_VALUE) Sleep(50);
            }

            if (pipe != INVALID_HANDLE_VALUE) {
                DWORD written = 0;
                WriteFile(pipe, utf8.data(), (DWORD)utf8.size(), &written, nullptr);
            }

            HeapFree(GetProcessHeap(), 0, node);
        }
    }

    if (pipe != INVALID_HANDLE_VALUE) CloseHandle(pipe);
    return 0;
}

DWORD WINAPI HookThread(LPVOID hModule) {
    if (MH_Initialize() != MH_OK) {

        return 1;
    }

    // Setup pipe worker
    g_workerEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (g_workerEvent) {
        InitializeSListHead(&g_messageList);
        g_workerThread = CreateThread(nullptr, 0, WorkerThreadStarter, nullptr, 0, nullptr);
        if (g_workerThread) {
            // keep the handle so we can WaitForSingleObject on it at cleanup
        }
    }

    // Install hook
    if (!InstallRunFileDlgHook()) {

    }
    else {

    }

    return 0;
}

DWORD WINAPI WorkerThreadStarter(LPVOID lpVoid) {
    WorkerThreadLoop(lpVoid);
    return 0;
}

bool InstallRunFileDlgHook() {
    HMODULE shell32 = LoadLibraryW(L"shell32.dll");
    if (!shell32) return false;

    FARPROC target = GetProcAddress(shell32, MAKEINTRESOURCEA(61) /*"RunFileDlg"*/);
    if (!target) return false;

    if (MH_CreateHook(target, &RunFileDlg_Hook,
        reinterpret_cast<LPVOID*>(&g_originalRunFileDlg)) != MH_OK) return false;

    if (MH_EnableHook(target) != MH_OK) return false;

    return true;
}

bool IsReboundShellRunning()
{
    bool found = false;

    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) return false;

    PROCESSENTRY32 pe = {};
    pe.dwSize = sizeof(PROCESSENTRY32);

    if (Process32First(hSnapshot, &pe))
    {
        do
        {
            std::wstring exeName(pe.szExeFile);
            if (exeName == L"Rebound Shell.exe")
            {
                found = true;
                break;
            }
        } while (Process32Next(hSnapshot, &pe));
    }

    CloseHandle(hSnapshot);
    return found;
}

int WINAPI RunFileDlg_Hook(HWND hwnd, HICON icon, LPCWSTR path, LPCWSTR title, LPCWSTR prompt, UINT flags)
{
    // Check if the hook is enabled in settings
    bool hookEnabled = GetBoolSetting(L"InstallRun", L"rebound", true);

    if (!hookEnabled) {
        // Setting is disabled, call original function
        if (g_originalRunFileDlg)
            return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
        return 0;
    }

    // Check if Rebound Shell is running
    if (IsReboundShellRunning())
    {
        SendMessageToApp(L"Shell::SpawnRunWindow##" + std::wstring(title ? title : L""));
        return 0; // skip original function
    }
    else
    {
        // Show dialog that Rebound Shell isn't running
        MessageBoxW(
            hwnd,
            L"Rebound Shell is not currently running.\n\nThe Run dialog hook is enabled, but the Rebound Shell process could not be found.\n\nPlease start Rebound Shell or disable the Run dialog hook in settings.",
            L"Rebound Shell Not Running",
            MB_OK | MB_ICONWARNING | MB_SETFOREGROUND
        );

        // Still call original as fallback
        if (g_originalRunFileDlg)
            return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
        return 0;
    }
}

// Find process ID by name
DWORD FindProcessByName(const wchar_t* processName) {
    DWORD processId = 0;
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    if (snapshot != INVALID_HANDLE_VALUE) {
        PROCESSENTRY32W entry;
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

DWORD WINAPI MonitorInjectorThread(LPVOID lpParam) {
    const int checkIntervalMs = 1000;       // check every 1s
    const int missingThreshold = 3;         // require 3 consecutive misses before unloading
    int missingCount = 0;

    // Keep monitoring until we decide to unload
    while (true) {
        // If the service host process is present, reset the missing counter
        DWORD pid = FindProcessByName(REBOUND_SERVICE_HOST_EXE);
        if (pid != 0) {
            missingCount = 0;
        }
        else {
            // not found
            missingCount++;
        }

        // Only initiate unload after missingThreshold consecutive misses
        if (missingCount >= missingThreshold) {
            break; // go to cleanup/unload
        }

        // Sleep a bit before next check (also allows orderly exit if DLL is detached)
        Sleep(checkIntervalMs);

        // Also exit this monitor if global flag already cleared (e.g. dll detach)
        if (!g_running) {
            return 0;
        }
    }

    // --- Begin graceful shutdown/unload ---

    // Signal the worker thread to stop and wake it if waiting
    g_running = false;
    if (g_workerEvent) {
        SetEvent(g_workerEvent);
    }

    // Disable hooks so no new hooked calls happen
    MH_DisableHook(MH_ALL_HOOKS);
    MH_Uninitialize();

    // Wait a short while for worker and hook threads to exit (best-effort)
    const DWORD waitTimeoutMs = 5000;

    if (g_workerThread) {
        DWORD wr = WaitForSingleObject(g_workerThread, waitTimeoutMs);
        // Close handle if thread ended or timed out (best-effort)
        CloseHandle(g_workerThread);
        g_workerThread = nullptr;
    }

    if (g_hookThread) {
        // If the hook thread isn't this thread, wait a bit for it to finish
        DWORD hookThreadId = GetThreadId(g_hookThread);
        if (hookThreadId != GetCurrentThreadId()) {
            WaitForSingleObject(g_hookThread, waitTimeoutMs);
            CloseHandle(g_hookThread);
        }
        else {
            // We're running inside g_hookThread — just close the handle
            CloseHandle(g_hookThread);
        }
        g_hookThread = nullptr;
    }

    // Small grace period
    Sleep(50);

    // Finally unload the DLL and exit this thread (will not return)
    if (g_moduleHandle) {
        FreeLibraryAndExitThread(g_moduleHandle, 0);
    }

    return 0;
}

// --------------------------------------------
// DLL entry point
// --------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID) {

    static HANDLE g_dllMutex = nullptr;

    if (fdwReason == DLL_PROCESS_ATTACH) {
        g_dllMutex = CreateMutexW(nullptr, FALSE, L"Global\\ReboundShell_RunHook");
        if (GetLastError() == ERROR_ALREADY_EXISTS)
        {
            // DLL already injected in this process
            return FALSE;
        }

        DisableThreadLibraryCalls(hinstDLL);

        DWORD currentPid = GetCurrentProcessId();
        wchar_t initMsg[256];
        swprintf_s(initMsg, L"[DLL] Attached to process %lu", currentPid);

        // save module handle
        g_moduleHandle = hinstDLL;

        // Start the hook thread and keep handle
        g_hookThread = CreateThread(nullptr, 0, HookThread, hinstDLL, 0, nullptr);
        if (g_hookThread) {
            // do NOT CloseHandle(g_hookThread) here — we need to wait on it during cleanup
        }

        // Start the monitor thread (keep its handle if you want to wait on it later)
        HANDLE hMonitorThread = CreateThread(nullptr, 0, MonitorInjectorThread, hinstDLL, 0, nullptr);
        if (hMonitorThread) {
            CloseHandle(hMonitorThread); // optionally close if you never intend to wait on monitor
        }
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        g_running = false;
        if (g_workerEvent) {
            SetEvent(g_workerEvent); // wake worker so it can exit
        }

        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();

        // Wait for threads to finish (best-effort)
        if (g_workerThread) {
            WaitForSingleObject(g_workerThread, 2000);
            CloseHandle(g_workerThread);
            g_workerThread = nullptr;
        }
        if (g_hookThread) {
            // if the hook thread is not the current thread
            if (GetCurrentThreadId() != GetThreadId(g_hookThread))
                WaitForSingleObject(g_hookThread, 2000);
            CloseHandle(g_hookThread);
            g_hookThread = nullptr;
        }

        if (g_dllMutex) {
            CloseHandle(g_dllMutex);
            g_dllMutex = nullptr;
        }
    }
    return TRUE;
}