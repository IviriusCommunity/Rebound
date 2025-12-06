// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

#include "pch.h"
#include <windows.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <shlobj.h>
#include <fstream>
#include <sstream>
#include <string>
#include "MinHook.h"

// --------------------------------------------
// Typedefs
// --------------------------------------------
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);

// Process name to watch for
static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound Service Host.exe";

// Store module handle for safe unloading
static HMODULE g_moduleHandle = nullptr;

// Running flag
static volatile bool g_running = true;

// Hooks installed flag
static volatile bool g_hooksInstalled = false;

// Original function pointer
static RunFileDlg_t g_originalRunFileDlg = nullptr;

// --------------------------------------------
// Settings Helper - Simple XML parser
// --------------------------------------------
bool GetBoolSetting(const wchar_t* key, const wchar_t* appName, bool defaultValue) {
    wchar_t userProfile[MAX_PATH];
    if (FAILED(SHGetFolderPathW(nullptr, CSIDL_PROFILE, nullptr, 0, userProfile))) {
        return defaultValue;
    }

    std::wstring filePath = std::wstring(userProfile) + L"\\.rebound\\" + appName + L".xml";

    if (GetFileAttributesW(filePath.c_str()) == INVALID_FILE_ATTRIBUTES) {
        return defaultValue;
    }

    std::wifstream file(filePath);
    if (!file.is_open()) {
        return defaultValue;
    }

    std::wstringstream buffer;
    buffer << file.rdbuf();
    std::wstring content = buffer.str();
    file.close();

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

    value.erase(0, value.find_first_not_of(L" \t\n\r"));
    value.erase(value.find_last_not_of(L" \t\n\r") + 1);

    if (_wcsicmp(value.c_str(), L"true") == 0 || value == L"1") {
        return true;
    }
    else if (_wcsicmp(value.c_str(), L"false") == 0 || value == L"0") {
        return false;
    }

    return defaultValue;
}

// --------------------------------------------
// Named Pipe Communication
// --------------------------------------------
bool SendMessageToApp(const std::wstring& message)
{
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    int retries = 0;
    while (retries < 10)
    {
        hPipe = CreateFileW(
            L"\\\\.\\pipe\\REBOUND_SHELL",
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
    utf8Message.resize(size_needed - 1);
    WideCharToMultiByte(CP_UTF8, 0, message.c_str(), -1, utf8Message.data(), size_needed, nullptr, nullptr);

    utf8Message += "\n";

    WriteFile(hPipe, utf8Message.data(), static_cast<DWORD>(utf8Message.size()), &bytesWritten, nullptr);
    CloseHandle(hPipe);
    return true;
}

bool SendMessageToServiceHost(const std::wstring& message)
{
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    int retries = 0;
    while (retries < 10)
    {
        hPipe = CreateFileW(
            L"\\\\.\\pipe\\REBOUND_SERVICE_HOST",
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
    utf8Message.resize(size_needed - 1);
    WideCharToMultiByte(CP_UTF8, 0, message.c_str(), -1, utf8Message.data(), size_needed, nullptr, nullptr);

    utf8Message += "\n";

    WriteFile(hPipe, utf8Message.data(), static_cast<DWORD>(utf8Message.size()), &bytesWritten, nullptr);
    CloseHandle(hPipe);
    return true;
}

bool IsReboundShellRunning()
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
            if (_wcsicmp(pe.szExeFile, L"Rebound Shell.exe") == 0)
            {
                found = true;
                break;
            }
        } while (Process32Next(hSnapshot, &pe));
    }

    CloseHandle(hSnapshot);
    return found;
}

// --------------------------------------------
// Process Monitor Functions
// --------------------------------------------
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
    const int checkIntervalMs = 1000;
    const int missingThreshold = 3;
    int missingCount = 0;

    while (g_running) {
        DWORD pid = FindProcessByName(REBOUND_SERVICE_HOST_EXE);
        if (pid != 0) {
            missingCount = 0;
        }
        else {
            missingCount++;
        }

        if (missingCount >= missingThreshold) {
            break;
        }

        Sleep(checkIntervalMs);
    }

    g_running = false;

    // Only disable our specific hook if it was installed
    if (g_hooksInstalled) {
        HMODULE shell32 = GetModuleHandleW(L"shell32.dll");
        if (shell32 && g_originalRunFileDlg) {
            FARPROC target = GetProcAddress(shell32, MAKEINTRESOURCEA(61));
            if (target) MH_DisableHook(target);
        }
    }

    Sleep(100);

    if (g_moduleHandle) {
        FreeLibraryAndExitThread(g_moduleHandle, 0);
    }

    return 0;
}

// --------------------------------------------
// Hook Function
// --------------------------------------------
int WINAPI RunFileDlg_Hook(HWND hwnd, HICON icon, LPCWSTR path, LPCWSTR title, LPCWSTR prompt, UINT flags)
{
    bool hookEnabled = GetBoolSetting(L"InstallRun", L"rebound", true);

    if (!hookEnabled) {
        if (g_originalRunFileDlg)
            return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
        return 0;
    }

    if (IsReboundShellRunning())
    {
        SendMessageToApp(L"Shell::SpawnRunWindow##" + std::wstring(title ? title : L""));
        return 0;
    }
    else
    {
        std::wstring dialogMessage = L"DialogWindow::Show#256##Rebound Shell Not Running##Rebound Shell is not currently running. The Run dialog hook is enabled, but the Rebound Shell process could not be found. Please start Rebound Shell or disable the Run dialog hook in settings.";
        SendMessageToServiceHost(dialogMessage);

        if (g_originalRunFileDlg)
            return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
        return 0;
    }
}

// --------------------------------------------
// Install hook - with detailed logging
// --------------------------------------------
bool InstallRunFileDlgHook() {
    OutputDebugStringW(L"[RunHook] InstallRunFileDlgHook: enter\n");

    HMODULE shell32 = LoadLibraryW(L"shell32.dll");
    if (!shell32) {
        OutputDebugStringW(L"[RunHook] LoadLibraryW(shell32.dll) FAILED\n");
        return false;
    }

    wchar_t msg[256];
    swprintf_s(msg, L"[RunHook] shell32 handle: 0x%p\n", shell32);
    OutputDebugStringW(msg);

    FARPROC target = GetProcAddress(shell32, MAKEINTRESOURCEA(61));
    swprintf_s(msg, L"[RunHook] RunFileDlg (ordinal 61) address: 0x%p\n", target);
    OutputDebugStringW(msg);

    if (!target) {
        OutputDebugStringW(L"[RunHook] GetProcAddress(ordinal 61) returned NULL\n");
        return false;
    }

    MH_STATUS status = MH_CreateHook(target, &RunFileDlg_Hook,
        reinterpret_cast<LPVOID*>(&g_originalRunFileDlg));

    swprintf_s(msg, L"[RunHook] MH_CreateHook status: %d\n", status);
    OutputDebugStringW(msg);

    // Tolerate already-created hooks
    if (status == MH_ERROR_ALREADY_CREATED) {
        OutputDebugStringW(L"[RunHook] Hook already created, trying to enable\n");
        status = MH_OK; // Treat as success, will try to enable
    }

    if (status != MH_OK) {
        OutputDebugStringW(L"[RunHook] MH_CreateHook FAILED\n");
        return false;
    }

    status = MH_EnableHook(target);
    swprintf_s(msg, L"[RunHook] MH_EnableHook status: %d\n", status);
    OutputDebugStringW(msg);

    if (status != MH_OK) {
        OutputDebugStringW(L"[RunHook] MH_EnableHook FAILED\n");
        return false;
    }

    OutputDebugStringW(L"[RunHook] Hook installed successfully!\n");
    return true;
}

// --------------------------------------------
// Deferred initialization thread
// --------------------------------------------
DWORD WINAPI DeferredInitThread(LPVOID lpParam) {
    OutputDebugStringW(L"[RunHook] DeferredInitThread: enter\n");

    // Allow loader lock to be released
    Sleep(300);

    OutputDebugStringW(L"[RunHook] Calling MH_Initialize\n");

    MH_STATUS status = MH_Initialize();

    wchar_t msg[256];
    swprintf_s(msg, L"[RunHook] MH_Initialize status: %d\n", status);
    OutputDebugStringW(msg);

    if (status == MH_ERROR_ALREADY_INITIALIZED) {
        OutputDebugStringW(L"[RunHook] MH_Initialize returned MH_ERROR_ALREADY_INITIALIZED, continuing\n");
        status = MH_OK;
    }
    if (status != MH_OK) {
        OutputDebugStringW(L"[RunHook] MH_Initialize FAILED, continuing without hooks\n");
        return 1;
    }

    if (InstallRunFileDlgHook()) {
        g_hooksInstalled = true;
        OutputDebugStringW(L"[RunHook] Hook installed successfully!\n");
    }
    else {
        OutputDebugStringW(L"[RunHook] Hook installation FAILED, continuing without hooks\n");
    }

    return 0;
}

// --------------------------------------------
// DLL entry point
// --------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID) {
    static HANDLE g_initMutex = nullptr;

    if (fdwReason == DLL_PROCESS_ATTACH) {
        OutputDebugStringW(L"[RunHook] DllMain: DLL_PROCESS_ATTACH enter\n");

        DisableThreadLibraryCalls(hinstDLL);

        wchar_t mutexName[256];
        swprintf_s(mutexName, L"ReboundShell_RunHook_%lu", GetCurrentProcessId());
        g_initMutex = CreateMutexW(nullptr, FALSE, mutexName);
        if (GetLastError() == ERROR_ALREADY_EXISTS) {
            OutputDebugStringW(L"[RunHook] Mutex already exists, DLL already loaded\n");
            if (g_initMutex) CloseHandle(g_initMutex);
            return FALSE;
        }

        g_moduleHandle = hinstDLL;

        // Start deferred initialization thread (avoids loader lock issues)
        HANDLE hInitThread = CreateThread(nullptr, 0, DeferredInitThread, nullptr, 0, nullptr);
        if (hInitThread) {
            CloseHandle(hInitThread);
            OutputDebugStringW(L"[RunHook] Deferred init thread created\n");
        }
        else {
            OutputDebugStringW(L"[RunHook] Failed to create init thread\n");
        }

        // Start monitor thread
        HANDLE hMonitorThread = CreateThread(nullptr, 0, MonitorInjectorThread, nullptr, 0, nullptr);
        if (hMonitorThread) {
            CloseHandle(hMonitorThread);
            OutputDebugStringW(L"[RunHook] Monitor thread created\n");
        }

        OutputDebugStringW(L"[RunHook] DllMain: returning TRUE\n");
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        OutputDebugStringW(L"[RunHook] DllMain: DLL_PROCESS_DETACH\n");
        g_running = false;

        if (g_hooksInstalled) {
            HMODULE shell32 = GetModuleHandleW(L"shell32.dll");
            if (shell32 && g_originalRunFileDlg) {
                FARPROC target = GetProcAddress(shell32, MAKEINTRESOURCEA(61));
                if (target) MH_DisableHook(target);
            }
        }

        if (g_initMutex) {
            CloseHandle(g_initMutex);
            g_initMutex = nullptr;
        }
    }

    return TRUE;
}