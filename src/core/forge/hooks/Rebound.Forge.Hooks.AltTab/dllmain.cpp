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
// XamlAltTabViewHost::Show
// Responsible for displaying the Alt+Tab task switcher

using DisplayAltTab_t = long(*)(void*, void** param1, int param2, void** param3);

static DisplayAltTab_t g_originalDisplayAltTab = nullptr;

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

void* ResolveDisplayAltTab()
{
    HMODULE mod = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!mod) return nullptr;

    PIMAGE_SECTION_HEADER textSection = FindSection(mod, ".text");
    if (!textSection) return nullptr;

    BYTE* sectionStart = (BYTE*)mod + textSection->VirtualAddress;
    BYTE* sectionEnd = sectionStart + textSection->Misc.VirtualSize;

    // XamlAltTabViewHost::Show
    static const BYTE sigBytes[] = { 0x48, 0x8b, 0xc4, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x81, 0xec, 0x80, 0x03, 0x00, 0x00, 0x0f, 0x29, 0x70, 0xb8, 0x0f, 0x29, 0x78, 0xa8 };

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
        OutputDebugStringW(L"[AltTabHook] Monitor: Disabling hooks due to service shutdown\n");

        HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
        if (twinui && g_originalDisplayAltTab) {
            void* target = ResolveDisplayAltTab();
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
long __fastcall DisplayAltTab_Hook(void* thisPtr, void** param1, int param2, void** param3)
{
    OutputDebugStringW(L"[AltTabHook] DisplayAltTab called!\n");

    bool hookEnabled = GetBoolSetting(L"InstallAltTab", L"rebound", true);

    if (!hookEnabled) {
        if (g_originalDisplayAltTab)
            g_originalDisplayAltTab(thisPtr, param1, param2, param3);
        return 0;
    }

    if (IsReboundShellRunning())
    {
        OutputDebugStringW(L"[AltTabHook] Redirecting to Rebound Shell\n");
        SendMessageToApp(L"Shell::ShowTaskSwitcher");
        return 0; // Success - don't show Windows Alt+Tab
    }
    else
    {
        MessageBoxW(NULL,
            L"Rebound Shell is not running. Start it or disable the hook in settings.",
            L"Rebound Shell Not Running",
            MB_OK | MB_ICONWARNING);

        // Fall back to original
        if (g_originalDisplayAltTab)
            g_originalDisplayAltTab(thisPtr, param1, param2, param3);
    }
    return 0;
}

// --------------------------------------------
// Install hook
// --------------------------------------------
bool InstallDisplayAltTabHook()
{
    OutputDebugStringW(L"[AltTabHook] Installing DisplayAltTab hook\n");

    HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
    if (!twinui) {
        OutputDebugStringW(L"[AltTabHook] twinui.pcshell.dll not loaded yet, waiting...\n");

        for (int i = 0; i < 50; i++) {
            Sleep(100);
            twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui) break;
        }

        if (!twinui) {
            OutputDebugStringW(L"[AltTabHook] twinui.pcshell.dll never loaded\n");
            return false;
        }
    }

    void* targetAddress = ResolveDisplayAltTab();
    if (!targetAddress) {
        OutputDebugStringW(L"[AltTabHook] Failed to resolve DisplayAltTab\n");
        return false;
    }

    wchar_t msg[256];
    swprintf_s(msg, L"[AltTabHook] twinui.pcshell.dll base: 0x%p\n", twinui);
    OutputDebugStringW(msg);
    swprintf_s(msg, L"[AltTabHook] Target address: 0x%p\n", targetAddress);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_CreateHook(
        targetAddress,
        &DisplayAltTab_Hook,
        reinterpret_cast<LPVOID*>(&g_originalDisplayAltTab)
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
    swprintf_s(msg, L"[AltTabHook] Process: %s\n", processName);
    OutputDebugStringW(msg);

    MH_STATUS status = MH_Initialize();
    swprintf_s(msg, L"[AltTabHook] MH_Initialize status: %d\n", status);
    OutputDebugStringW(msg);

    if (status == MH_ERROR_ALREADY_INITIALIZED) status = MH_OK;
    if (status != MH_OK) {
        OutputDebugStringW(L"[AltTabHook] MH_Initialize failed\n");
        return 1;
    }

    wchar_t processNameLower[MAX_PATH];
    wcscpy_s(processNameLower, processName);
    _wcslwr_s(processNameLower);

    if (wcsstr(processNameLower, L"explorer.exe")) {
        OutputDebugStringW(L"[AltTabHook] Detected explorer.exe - installing Alt+Tab hook\n");

        if (InstallDisplayAltTabHook()) {
			MessageBoxW(NULL, L"Alt+Tab hook installed successfully! You can now use Alt+Tab to switch to Rebound Shell when it's running.", L"Hook Installed", MB_OK | MB_ICONINFORMATION);
            g_hooksInstalled = true;
            OutputDebugStringW(L"[AltTabHook] Alt+Tab hook installed!\n");
        }
        else {
			MessageBoxW(NULL, L"Failed to install Alt+Tab hook. Make sure Rebound Shell is running and the hook is enabled in settings.", L"Hook Installation Failed", MB_OK | MB_ICONERROR);
            OutputDebugStringW(L"[AltTabHook] Hook installation FAILED\n");
        }
    }
    else {
        swprintf_s(msg, L"[AltTabHook] NOT explorer.exe, skipping hook\n");
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

		MessageBoxW(NULL, L"Rebound Shell Alt+Tab hook loaded! If you see this message, the DLL was injected successfully. You can now enable the hook in settings.", L"Hook Loaded", MB_OK | MB_ICONINFORMATION);

        wchar_t mutexName[256];
        swprintf_s(mutexName, L"ReboundShell_AltTabHook_%lu", GetCurrentProcessId());
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
        OutputDebugStringW(L"[AltTabHook] DllMain: DLL_PROCESS_DETACH\n");
        g_running = false;

        if (g_hooksInstalled) {
            OutputDebugStringW(L"[AltTabHook] Disabling hooks...\n");

            HMODULE twinui = GetModuleHandleW(L"twinui.pcshell.dll");
            if (twinui && g_originalDisplayAltTab) {
                void* target = ResolveDisplayAltTab();
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