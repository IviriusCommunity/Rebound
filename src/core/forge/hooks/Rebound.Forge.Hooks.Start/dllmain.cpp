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

#pragma pack(push, 1)

struct TypeDescriptor {
    void* vftable;
    void* spare;
    char  name[1];
};

struct CompleteObjectLocator {
    uint32_t signature;
    uint32_t offset;
    uint32_t cdOffset;
    int32_t  pTypeDescriptor; // RVA
    int32_t  pClassDescriptor;
};

#pragma pack(pop)

// Process name to watch for
static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound Service Host.exe";

// Store module handle for safe unloading
static HMODULE g_moduleHandle = nullptr;

// Running flag
static volatile bool g_running = true;

// Hooks installed flag
static volatile bool g_hooksInstalled = false;

// From twinui.pcshell.dll
// -----
// XamlLauncher::ShowStartView(IMMERSIVELAUNCHERSHOWMETHOD showMethod,IMMERSIVELAUNCHERSHOWFLAGS showFlags)
// -----
// Haven't documented IMMERSIVELAUNCHERSHOWMETHOD and IMMERSIVELAUNCHERSHOWFLAGS yet, but ShowStartView
// alone is enough for this hook.
// -----
// ShowStartView is responsible for showing the start menu, probably via IPC with StartMenuExperienceHost
// or something internal. I know these aren't the best docs you'll find but it's what I have for now.

using ShowStartView_t = long(*)(void*, int, int);

static ShowStartView_t g_originalShowStartView = nullptr;

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

void** ResolveXamlLauncherVtable()
{
    HMODULE mod = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!mod) return nullptr;

    auto rdata = FindSection(mod, ".rdata");
    if (!rdata) return nullptr;

    BYTE* start = (BYTE*)mod + rdata->VirtualAddress;
    BYTE* end = start + rdata->Misc.VirtualSize;

    const char* needle = ".?AVXamlLauncher@@";
    TypeDescriptor* td = nullptr;

    // Find TypeDescriptor
    for (BYTE* p = start; p < end; p++)
    {
        if (!memcmp(p, needle, strlen(needle)))
        {
            td = (TypeDescriptor*)(p - offsetof(TypeDescriptor, name));
            break;
        }
    }

    if (!td)
        return nullptr;

    auto tdRva = (DWORD)((BYTE*)td - (BYTE*)mod);

    // Find COL that references it
    for (BYTE* p = start; p < end; p += sizeof(void*))
    {
        auto col = (CompleteObjectLocator*)p;

        __try {
            if (col->signature == 1 &&
                col->pTypeDescriptor == tdRva)
            {
                return (void**)((BYTE*)col + sizeof(CompleteObjectLocator));
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER) {}
    }

    return nullptr;
}

void* ResolveShowStartView()
{
    HMODULE mod = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!mod) return nullptr;

    // Known hardcoded offset from 4 hours of decompiling with Ghidra
    constexpr SIZE_T hardcodedOffset = 0x1D2320;

    BYTE* base = (BYTE*)mod;
    BYTE* candidate = base + hardcodedOffset;

    // Validate candidate is in executable memory
    MEMORY_BASIC_INFORMATION mbi{};
    if (!VirtualQuery(candidate, &mbi, sizeof(mbi)))
        return nullptr;

    if (!(mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ))) // allow execute-read
        return nullptr;

    // Validate prologue: function starts with bytes 48 89 5C (typical x64)
    if (candidate[0] == 0x48 && candidate[1] == 0x89 && candidate[2] == 0x5C)
        return candidate;

    return nullptr;
}

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

    // Disable hook if installed
    if (g_hooksInstalled) {
        OutputDebugStringW(L"[Hook] Monitor: Disabling hooks due to service shutdown\n");

        HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
        if (twinui && g_originalShowStartView) {
            void* target = ResolveShowStartView();
            MH_DisableHook(target);
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
long __fastcall ShowStartView_Hook(void* thisPtr, int showMethod, int showFlags)
{
    OutputDebugStringW(L"[StartMenuHook] ShowStartView called!\n");

    bool hookEnabled = GetBoolSetting(L"InstallStart", L"rebound", true);

    if (!hookEnabled) {
        if (g_originalShowStartView)
            return g_originalShowStartView(thisPtr, showMethod, showFlags);
        return 0;
    }

    if (IsReboundShellRunning())
    {
        OutputDebugStringW(L"[StartMenuHook] Redirecting to Rebound Shell\n");
        SendMessageToApp(L"Shell::ShowStartMenu");
        return 0; // Success - don't show Windows Start Menu
    }
    else
    {
        MessageBoxW(NULL,
            L"Rebound Shell is not running. Start it or disable the hook in settings.",
            L"Rebound Shell Not Running",
            MB_OK | MB_ICONWARNING);

        // Fall back to original
        if (g_originalShowStartView)
            return g_originalShowStartView(thisPtr, showMethod, showFlags);
        return 0;
    }
}

// --------------------------------------------
// Install hook - with detailed logging
// --------------------------------------------
bool InstallShowStartViewHook()
{
    OutputDebugStringW(L"[StartMenuHook] Installing ShowStartView hook\n");

    // Get twinui.pcshell.dll module
    HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!twinui) {
        OutputDebugStringW(L"[StartMenuHook] twinui.pcshell.dll not loaded yet, waiting...\n");

        // Wait for it to load (up to 5 seconds)
        for (int i = 0; i < 50; i++) {
            Sleep(100);
            twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui) break;
        }

        if (!twinui) {
            OutputDebugStringW(L"[StartMenuHook] twinui.pcshell.dll never loaded\n");
            return false;
        }
    }

    // ShowStartView offset: 0x1D2320 (from Ghidra address 18011D2320 - base 180000000)
    void* targetAddress = ResolveShowStartView();

    wchar_t msg[256];
    swprintf_s(msg, L"[StartMenuHook] twinui.pcshell.dll base: 0x%p\n", twinui);
    OutputDebugStringW(msg);
    swprintf_s(msg, L"[StartMenuHook] Target address: 0x%p\n", targetAddress);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_CreateHook(
        targetAddress,
        &ShowStartView_Hook,
        reinterpret_cast<LPVOID*>(&g_originalShowStartView)
    );

    if (status == MH_ERROR_ALREADY_CREATED) {
        status = MH_OK;
    }

    if (status != MH_OK) {
        wchar_t errorMsg[256];
        swprintf_s(errorMsg, L"MH_CreateHook failed with status: %d", status);
        MessageBoxW(NULL, errorMsg, L"Hook Failed", MB_OK);
        return false;
    }

    status = MH_EnableHook(targetAddress);
    if (status != MH_OK) {
        wchar_t errorMsg[256];
        swprintf_s(errorMsg, L"MH_EnableHook failed with status: %d", status);
        MessageBoxW(NULL, errorMsg, L"Hook Failed", MB_OK);
        return false;
    }

    return true;
}

// --------------------------------------------
// Deferred initialization thread
// --------------------------------------------
DWORD WINAPI DeferredInitThread(LPVOID lpParam) {
    Sleep(300);

    wchar_t processName[MAX_PATH];
    GetModuleFileNameW(nullptr, processName, MAX_PATH);

    wchar_t msg[512];
    swprintf_s(msg, L"[Hook] Process: %s\n", processName);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_Initialize();
    swprintf_s(msg, L"[Hook] MH_Initialize status: %d\n", status);
    OutputDebugStringW(msg);

    if (status == MH_ERROR_ALREADY_INITIALIZED) status = MH_OK;
    if (status != MH_OK) {
        OutputDebugStringW(L"[Hook] MH_Initialize failed\n");
        return 1;
    }

    // Install hook in explorer.exe
    wchar_t processNameLower[MAX_PATH];
    wcscpy_s(processNameLower, processName);
    _wcslwr_s(processNameLower);

    // Install hook in explorer.exe
    if (wcsstr(processNameLower, L"explorer.exe")) {
        OutputDebugStringW(L"[Hook] Detected explorer.exe - installing Start Menu hook\n");

        if (InstallShowStartViewHook()) {
            g_hooksInstalled = true;
            OutputDebugStringW(L"[Hook] Start Menu hook installed!\n");
        }
        else {
            OutputDebugStringW(L"[Hook] Hook installation FAILED\n");
        }
    }
    else {
        swprintf_s(msg, L"[Hook] NOT explorer.exe, skipping hook\n");
        OutputDebugStringW(msg);
    }

    return 0;
}

// --------------------------------------------
// DLL entry point
// --------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID) {
    static HANDLE g_initMutex = nullptr;

    if (fdwReason == DLL_PROCESS_ATTACH) {
        
        DisableThreadLibraryCalls(hinstDLL);

        wchar_t mutexName[256];
        swprintf_s(mutexName, L"ReboundShell_StartHook_%lu", GetCurrentProcessId());
        g_initMutex = CreateMutexW(nullptr, FALSE, mutexName);
        if (GetLastError() == ERROR_ALREADY_EXISTS) {
            if (g_initMutex) CloseHandle(g_initMutex);
            return FALSE;
        }

        g_moduleHandle = hinstDLL;

        // Start deferred initialization thread (avoids loader lock issues)
        HANDLE hInitThread = CreateThread(nullptr, 0, DeferredInitThread, nullptr, 0, nullptr);
        if (hInitThread) {
            CloseHandle(hInitThread);
        }
        else {

        }

        // Start monitor thread
        HANDLE hMonitorThread = CreateThread(nullptr, 0, MonitorInjectorThread, nullptr, 0, nullptr);
        if (hMonitorThread) {
            CloseHandle(hMonitorThread);
        }
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        OutputDebugStringW(L"[Hook] DllMain: DLL_PROCESS_DETACH\n");
        g_running = false;

        if (g_hooksInstalled) {
            OutputDebugStringW(L"[Hook] Disabling hooks...\n");

            // Unhook ShowStartView in twinui.pcshell.dll
            HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui && g_originalShowStartView) {
                void* target = ResolveShowStartView();
                MH_STATUS status = MH_DisableHook(target);

                wchar_t msg[256];
                swprintf_s(msg, L"[Hook] MH_DisableHook status: %d\n", status);
                OutputDebugStringW(msg);
            }

            // Uninitialize MinHook
            MH_Uninitialize();
        }

        if (g_initMutex) {
            CloseHandle(g_initMutex);
            g_initMutex = nullptr;
        }
    }

    return TRUE;
}