#include "pch.h"
#include <windows.h>
#include <stdint.h>
#include <process.h>
#include <stdio.h>
#include "MinHook.h"
#include <string>
#include <combaseapi.h>
#include <comdef.h> // For _com_error

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

// {E7F6D0A3-1234-4567-89AB-1C2D3E4F5678}
static const GUID IID_IReboundShellServer =
{ 0xe7f6d0a3, 0x1234, 0x4567, {0x89, 0xab, 0x1c, 0x2d, 0x3e, 0x4f, 0x56, 0x78} };

// {A1B2C3D4-5678-1234-ABCD-9876543210FE}
static const GUID CLSID_ReboundShellServer =
{ 0xa1b2c3d4, 0x5678, 0x1234, {0xab, 0xcd, 0x98, 0x76, 0x54, 0x32, 0x10, 0xfe} };

struct IReboundShellServer : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE OpenRunBox() = 0;
};

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

void QueueMessage(const wchar_t* msg) {
    MsgNode* node = (MsgNode*)HeapAlloc(GetProcessHeap(), 0, sizeof(MsgNode));
    if (!node) return;

    node->link.Next = nullptr;
    wcsncpy_s(node->msg, msg, _TRUNCATE);

    InterlockedPushEntrySList(&g_messageList, (PSLIST_ENTRY)node);
    if (g_workerEvent) SetEvent(g_workerEvent);
}

void SendPipeMessage(const wchar_t* msg, HANDLE pipe) {
    auto utf8 = WideToUtf8(msg);
    DWORD written = 0;
    WriteFile(pipe, utf8.data(), (DWORD)utf8.size(), &written, nullptr);
}

const char* kMessage = "Shell::SpawnRunWindow\n"; // simple literal UTF-8

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

// --------------------------------------------
// MinHook hook for RunFileDlg
// --------------------------------------------
int WINAPI RunFileDlg_Hook(HWND hwnd, HICON icon, LPCWSTR path, LPCWSTR title, LPCWSTR prompt, UINT flags) {
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(hr) && hr != RPC_E_CHANGED_MODE) {
        OutputDebugStringA("CoInitializeEx failed");
        return 0;
    }

    IReboundShellServer* pServer = nullptr;
    hr = CoCreateInstance(CLSID_ReboundShellServer, nullptr, CLSCTX_LOCAL_SERVER, IID_IReboundShellServer, (void**)&pServer);
    if (SUCCEEDED(hr) && pServer) {
        pServer->OpenRunBox();
        pServer->Release();
    }
    else {
        OutputDebugStringA("Failed to connect to ReboundShellServer COM");
    }

    if (SUCCEEDED(hr)) CoUninitialize();

    //if (g_originalRunFileDlg) {
    //    return g_originalRunFileDlg(hwnd, icon, path, title, prompt, flags);
    //}

    return 0;
}

void RemoveRunFileDlgHook() {
    MH_DisableHook(MH_ALL_HOOKS);
    MH_Uninitialize();
}

// --------------------------------------------
// Injector thread
// --------------------------------------------
DWORD WINAPI InjectorThread(LPVOID) {
    g_workerEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (!g_workerEvent) return 1;

    InitializeSListHead(&g_messageList);

    HANDLE hWorker = CreateThread(nullptr, 0, WorkerThreadStarter, nullptr, 0, nullptr);
    if (hWorker) CloseHandle(hWorker);

    if (!InstallRunFileDlgHook())
        OutputDebugStringW(L"[Injector] Hook install failed");

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
        L"Hook Probe",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 300, 200,
        nullptr, nullptr, GetModuleHandle(nullptr), nullptr
    );

    if (!hwnd)
        return 1;

    ShowWindow(hwnd, SW_SHOW);

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
    if (fdwReason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinstDLL);
        CreateThread(nullptr, 0, HookThread, hinstDLL, 0, nullptr);
        CreateThread(nullptr, 0, ProbeThread, nullptr, 0, nullptr); // << your probe window
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        g_running = false;
        SetEvent(g_workerEvent); // wake worker so it can exit
        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
    }
    return TRUE;
}