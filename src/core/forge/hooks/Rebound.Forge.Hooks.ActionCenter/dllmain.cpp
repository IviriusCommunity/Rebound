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
#include "MinHook.h"

// Process name to watch for
static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound Service Host.exe";

// Store module handle for safe unloading
static HMODULE g_moduleHandle = nullptr;

// Running flag
static volatile bool g_running = true;

// Hooks installed flag
static volatile bool g_hooksInstalled = false;

// From twinui.pcshell.dll
// DashboardManager::StartSwipe(uint param_1, tagPOINT param_2)
// Responsible for showing the Action Center

using StartSwipe_t = long(*)(void*, unsigned int, void*);

static StartSwipe_t g_originalStartSwipe = nullptr;

// --------------------------------------------
// Helper Functions
// --------------------------------------------

PIMAGE_SECTION_HEADER FindSection(HMODULE mod, const char* name)
{
    auto dos = (PIMAGE_DOS_HEADER)mod;
    auto nt = (PIMAGE_NT_HEADERS)((BYTE*)mod + dos->e_lfanew);
    auto sec = IMAGE_FIRST_SECTION(nt);

    for (int i = 0; i < nt->FileHeader.NumberOfSections; i++, sec++)
        if (!memcmp(sec->Name, name, strlen(name)))
            return sec;

    return nullptr;
}

void* ResolveStartSwipe()
{
    HMODULE mod = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!mod) return nullptr;

    PIMAGE_SECTION_HEADER textSection = FindSection(mod, ".text");
    if (!textSection) return nullptr;

    BYTE* sectionStart = (BYTE*)mod + textSection->VirtualAddress;
    BYTE* sectionEnd = sectionStart + textSection->Misc.VirtualSize;

    // Pattern from CActionCenterExperienceManager::StartSwipe function prologue:
    // 48 89 5C 24 10              MOV [RSP+10h], RBX
    // 55                          PUSH RBP
    // 48 8D AC 24 70 FF FF FF     LEA RBP, [RSP-90h]
    // 48 81 EC 90 01 00 00        SUB RSP, 190h
    static const BYTE sigBytes[] = {
        0x48, 0x89, 0x5C, 0x24, 0x10,                    // MOV [RSP+10h], RBX
        0x55,                                             // PUSH RBP
        0x48, 0x8D, 0xAC, 0x24, 0x70, 0xFF, 0xFF, 0xFF,  // LEA RBP, [RSP-90h]
        0x48, 0x81, 0xEC, 0x90, 0x01, 0x00, 0x00         // SUB RSP, 190h
    };

    constexpr SIZE_T sigLen = sizeof(sigBytes);

    for (BYTE* p = sectionStart; p <= sectionEnd - sigLen; p++)
    {
        if (memcmp(p, sigBytes, sigLen) == 0)
        {
            return p; // Found the function start
        }
    }

    return nullptr;
}

// --------------------------------------------
// Settings Helper - Simple XML parser
// --------------------------------------------
bool GetBoolSetting(const wchar_t* key, const wchar_t* appName, bool defaultValue)
{
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
DWORD FindProcessByName(const wchar_t* processName)
{
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

DWORD WINAPI MonitorInjectorThread(LPVOID lpParam)
{
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

    // Disable hook if installed
    if (g_hooksInstalled) {
        OutputDebugStringW(L"[ActionCenterHook] Monitor: Disabling hooks due to service shutdown\n");

        HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
        if (twinui && g_originalStartSwipe) {
            void* target = ResolveStartSwipe();
            if (target) {
                MH_DisableHook(target);
            }
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
long __fastcall StartSwipe_Hook(void* thisPtr, unsigned int param1, void* param2)
{
    OutputDebugStringW(L"[ActionCenterHook] StartSwipe called!\n");

    bool hookEnabled = GetBoolSetting(L"InstallActionCenter", L"rebound", true);

    if (!hookEnabled) {
        if (g_originalStartSwipe)
            return g_originalStartSwipe(thisPtr, param1, param2);
        return 0;
    }

    if (IsReboundShellRunning())
    {
        OutputDebugStringW(L"[ActionCenterHook] Redirecting to Rebound Shell\n");
        SendMessageToApp(L"Shell::ShowActionCenter");
        return 0; // Success - don't show Windows Action Center
    }
    else
    {
        MessageBoxW(NULL,
            L"Rebound Shell is not running. Start it or disable the hook in settings.",
            L"Rebound Shell Not Running",
            MB_OK | MB_ICONWARNING);

        // Fall back to original
        if (g_originalStartSwipe)
            return g_originalStartSwipe(thisPtr, param1, param2);
        return 0;
    }
}

// --------------------------------------------
// Install hook
// --------------------------------------------
bool InstallStartSwipeHook()
{
    OutputDebugStringW(L"[ActionCenterHook] Installing StartSwipe hook\n");

    HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!twinui) {
        OutputDebugStringW(L"[ActionCenterHook] twinui.pcshell.dll not loaded yet, waiting...\n");

        for (int i = 0; i < 50; i++) {
            Sleep(100);
            twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui) break;
        }

        if (!twinui) {
            OutputDebugStringW(L"[ActionCenterHook] twinui.pcshell.dll never loaded\n");
            return false;
        }
    }

    void* targetAddress = ResolveStartSwipe();
    if (!targetAddress) {
        OutputDebugStringW(L"[ActionCenterHook] Failed to resolve StartSwipe\n");
        return false;
    }

    wchar_t msg[256];
    swprintf_s(msg, L"[ActionCenterHook] twinui.pcshell.dll base: 0x%p\n", twinui);
    OutputDebugStringW(msg);
    swprintf_s(msg, L"[ActionCenterHook] Target address: 0x%p\n", targetAddress);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_CreateHook(
        targetAddress,
        &StartSwipe_Hook,
        reinterpret_cast<LPVOID*>(&g_originalStartSwipe)
    );

    if (status == MH_ERROR_ALREADY_CREATED) {
        status = MH_OK;
    }

    if (status != MH_OK) {
        wchar_t errorMsg[256];
        swprintf_s(errorMsg, L"MH_CreateHook failed with status: %d", status);
        OutputDebugStringW(errorMsg);
        return false;
    }

    status = MH_EnableHook(targetAddress);
    if (status != MH_OK) {
        wchar_t errorMsg[256];
        swprintf_s(errorMsg, L"MH_EnableHook failed with status: %d", status);
        OutputDebugStringW(errorMsg);
        return false;
    }

    return true;
}

// --------------------------------------------
// Deferred initialization thread
// --------------------------------------------
DWORD WINAPI DeferredInitThread(LPVOID lpParam)
{
    Sleep(300);

    wchar_t processName[MAX_PATH];
    GetModuleFileNameW(nullptr, processName, MAX_PATH);

    wchar_t msg[512];
    swprintf_s(msg, L"[ActionCenterHook] Process: %s\n", processName);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_Initialize();
    swprintf_s(msg, L"[ActionCenterHook] MH_Initialize status: %d\n", status);
    OutputDebugStringW(msg);

    if (status == MH_ERROR_ALREADY_INITIALIZED) status = MH_OK;
    if (status != MH_OK) {
        OutputDebugStringW(L"[ActionCenterHook] MH_Initialize failed\n");
        return 1;
    }

    wchar_t processNameLower[MAX_PATH];
    wcscpy_s(processNameLower, processName);
    _wcslwr_s(processNameLower);

    if (wcsstr(processNameLower, L"explorer.exe")) {
        OutputDebugStringW(L"[ActionCenterHook] Detected explorer.exe - installing Action Center hook\n");

        if (InstallStartSwipeHook()) {
            g_hooksInstalled = true;
            OutputDebugStringW(L"[ActionCenterHook] Action Center hook installed!\n");
        }
        else {
            OutputDebugStringW(L"[ActionCenterHook] Hook installation FAILED\n");
        }
    }
    else {
        swprintf_s(msg, L"[ActionCenterHook] NOT explorer.exe, skipping hook\n");
        OutputDebugStringW(msg);
    }

    return 0;
}

// --------------------------------------------
// DLL entry point
// --------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID)
{
    static HANDLE g_initMutex = nullptr;

    if (fdwReason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinstDLL);

        wchar_t mutexName[256];
        swprintf_s(mutexName, L"ReboundShell_ActionCenterHook_%lu", GetCurrentProcessId());
        g_initMutex = CreateMutexW(nullptr, FALSE, mutexName);
        if (GetLastError() == ERROR_ALREADY_EXISTS) {
            if (g_initMutex) CloseHandle(g_initMutex);
            return FALSE;
        }

        g_moduleHandle = hinstDLL;

        HANDLE hInitThread = CreateThread(nullptr, 0, DeferredInitThread, nullptr, 0, nullptr);
        if (hInitThread) {
            CloseHandle(hInitThread);
        }

        HANDLE hMonitorThread = CreateThread(nullptr, 0, MonitorInjectorThread, nullptr, 0, nullptr);
        if (hMonitorThread) {
            CloseHandle(hMonitorThread);
        }
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        OutputDebugStringW(L"[ActionCenterHook] DllMain: DLL_PROCESS_DETACH\n");
        g_running = false;

        if (g_hooksInstalled) {
            OutputDebugStringW(L"[ActionCenterHook] Disabling hooks...\n");

            HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui && g_originalStartSwipe) {
                void* target = ResolveStartSwipe();
                if (target) {
                    MH_DisableHook(target);
                }
            }

            MH_Uninitialize();
        }

        if (g_initMutex) {
            CloseHandle(g_initMutex);
            g_initMutex = nullptr;
        }
    }

    return TRUE;
}