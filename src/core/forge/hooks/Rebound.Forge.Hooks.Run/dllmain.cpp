#include "pch.h"
#include <windows.h>
#include <stdint.h>
#include <process.h>
#include <stdio.h>
#include "MinHook.h"
#include <string>
#include <tlhelp32.h>

// --------------------------------------------
// Typedefs
// --------------------------------------------
#ifdef _M_X64
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#else
typedef int (WINAPI* RunFileDlg_t)(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
#endif

// Original function pointer
static RunFileDlg_t g_originalRunFileDlg = nullptr;

// --------------------------------------------
// Forward declarations
// --------------------------------------------
DWORD WINAPI WorkerThreadStarter(LPVOID lpVoid);
DWORD WINAPI HookThread(LPVOID hModule);
bool InstallRunFileDlgHook();
int WINAPI RunFileDlg_Hook(HWND, HICON, LPCWSTR, LPCWSTR, LPCWSTR, UINT);
void RemoveRunFileDlgHook();

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

    while (true) {
        WaitForSingleObject(g_workerEvent, INFINITE);

        PSLIST_ENTRY entry;
        while ((entry = InterlockedPopEntrySList(&g_messageList)) != nullptr) {
            auto node = CONTAINING_RECORD(entry, MsgNode, link);

            // ensure UTF-8 conversion with newline
            std::string utf8 = WideToUtf8(node->msg);
            utf8 += "\n";

            // lazy pipe connect with retry
            while (pipe == INVALID_HANDLE_VALUE) {
                pipe = CreateFileA("\\\\.\\pipe\\REBOUND_SERVICE_HOST", GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
                if (pipe == INVALID_HANDLE_VALUE) Sleep(50);
            }

            DWORD written = 0;
            WriteFile(pipe, utf8.data(), (DWORD)utf8.size(), &written, nullptr);

            HeapFree(GetProcessHeap(), 0, node);
        }
    }

    if (pipe != INVALID_HANDLE_VALUE) CloseHandle(pipe);
    return 0;
}

DWORD WINAPI HookThread(LPVOID hModule) {
    if (MH_Initialize() != MH_OK) {
        OutputDebugStringW(L"[HookThread] MH_Initialize failed");
        return 1;
    }

    // Setup pipe worker
    g_workerEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (g_workerEvent) {
        InitializeSListHead(&g_messageList);
        HANDLE hWorker = CreateThread(nullptr, 0, WorkerThreadStarter, nullptr, 0, nullptr);
        if (hWorker) CloseHandle(hWorker);
    }

    // Install hook
    if (!InstallRunFileDlgHook()) {
        OutputDebugStringW(L"[HookThread] InstallRunFileDlgHook failed");
    }
    else {
        OutputDebugStringW(L"[HookThread] Hook installed successfully!");
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
    if (IsReboundShellRunning())
    {
        SendMessageToApp(L"Shell::SpawnRunWindow##" + std::wstring(title ? title : L""));
        return 0; // skip original function
    }

    // Failsafe: call the original if Rebound Shell isn't running
    if (g_originalRunFileDlg)
        return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);

    return 0;
}

DWORD WINAPI ProbeThread(LPVOID lpReserved)
{
    // Register a simple window class
    WNDCLASS wc = {};
    wc.lpfnWndProc = DefWindowProc;
    wc.hInstance = GetModuleHandle(nullptr);
    wc.lpszClassName = L"ProbeWindowClass";

    if (!RegisterClass(&wc))
        return 1;

    // Create the window
    HWND hwnd = CreateWindowEx(
        0,
        L"ProbeWindowClass",
        L"Rebound Shell Probe",
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU, // no maximize/minimize
        CW_USEDEFAULT, CW_USEDEFAULT, 300, 100,
        nullptr, nullptr, GetModuleHandle(nullptr), nullptr
    );

    if (!hwnd)
        return 1;

    // Create a simple static label
    CreateWindowEx(
        0, L"STATIC", L"Rebound Shell DLL injection successful!",
        WS_CHILD | WS_VISIBLE | SS_CENTER,
        10, 20, 280, 40,
        hwnd, nullptr, GetModuleHandle(nullptr), nullptr
    );

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    // Simple message loop
    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}

// --------------------------------------------
// DLL entry point
// --------------------------------------------
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID) {

    static HANDLE g_dllMutex = nullptr;

    if (fdwReason == DLL_PROCESS_ATTACH){
        g_dllMutex = CreateMutexW(nullptr, FALSE, L"Global\\ReboundShell_RunHook");
        if (GetLastError() == ERROR_ALREADY_EXISTS)
        {
            // DLL already injected in this process
            return FALSE;
        }

        DisableThreadLibraryCalls(hinstDLL);
        CreateThread(nullptr, 0, HookThread, hinstDLL, 0, nullptr);
        //CreateThread(nullptr, 0, ProbeThread, nullptr, 0, nullptr);
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        g_running = false;
        SetEvent(g_workerEvent); // wake worker so it can exit
        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
    }
    return TRUE;
}