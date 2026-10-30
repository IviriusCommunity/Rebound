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
#include <shellapi.h>
#include <shobjidl.h>
#include <objbase.h>
#include <cwctype>
#include <appmodel.h>
#include <vector>
#include <cstdarg>
#include "MinHook.h"

static const wchar_t* REBOUND_SERVICE_HOST_EXE = L"Rebound.ServiceHost.exe";
static const wchar_t* REBOUND_DLL_INJECTION_PIPE = L"\\\\.\\pipe\\REBOUND_DLL_INJECTION";

enum class LifecycleState
{
    Starting,
    Running,
    Stopping,
    Stopped
};

static HMODULE g_moduleHandle = nullptr;

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

// Modules we pinned with LoadLibraryW so hook targets stay mapped for the
// whole lifetime of the hooks. Touched only by the init thread and, after it
// has been joined, by the shutdown coordinator.
static std::vector<HMODULE> g_loadedModules;

// ---------------------------------------------------------------------------
// Hooked function signatures
// ---------------------------------------------------------------------------

using ShellAboutW_t = BOOL(WINAPI*)(
    HWND, LPCWSTR, LPCWSTR, HICON);

using ShellAboutA_t = BOOL(WINAPI*)(
    HWND, LPCSTR, LPCSTR, HICON);

using ShellExecuteW_t = HINSTANCE(WINAPI*)(
    HWND, LPCWSTR, LPCWSTR, LPCWSTR, LPCWSTR, INT);

using ShellExecuteA_t = HINSTANCE(WINAPI*)(
    HWND, LPCSTR, LPCSTR, LPCSTR, LPCSTR, INT);

using ShellExecuteExW_t = BOOL(WINAPI*)(
    SHELLEXECUTEINFOW*);

using ShellExecuteExA_t = BOOL(WINAPI*)(
    SHELLEXECUTEINFOA*);

using CreateProcessW_t = BOOL(WINAPI*)(
    LPCWSTR, LPWSTR,
    LPSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
    BOOL, DWORD, LPVOID, LPCWSTR,
    LPSTARTUPINFOW, LPPROCESS_INFORMATION);

using CreateProcessA_t = BOOL(WINAPI*)(
    LPCSTR, LPSTR,
    LPSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
    BOOL, DWORD, LPVOID, LPCSTR,
    LPSTARTUPINFOA, LPPROCESS_INFORMATION);

using CreateProcessAsUserW_t = BOOL(WINAPI*)(
    HANDLE, LPCWSTR, LPWSTR,
    LPSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
    BOOL, DWORD, LPVOID, LPCWSTR,
    LPSTARTUPINFOW, LPPROCESS_INFORMATION);

using CreateProcessAsUserA_t = BOOL(WINAPI*)(
    HANDLE, LPCSTR, LPSTR,
    LPSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
    BOOL, DWORD, LPVOID, LPCSTR,
    LPSTARTUPINFOA, LPPROCESS_INFORMATION);

// Undocumented, but the single choke point under CreateProcess{A,W},
// CreateProcessAsUser{A,W} and WinExec in KernelBase. Optional hook.
using CreateProcessInternalW_t = BOOL(WINAPI*)(
    HANDLE, LPCWSTR, LPWSTR,
    LPSECURITY_ATTRIBUTES, LPSECURITY_ATTRIBUTES,
    BOOL, DWORD, LPVOID, LPCWSTR,
    LPSTARTUPINFOW, LPPROCESS_INFORMATION, PHANDLE);

using CreateProcessWithLogonW_t = BOOL(WINAPI*)(
    LPCWSTR, LPCWSTR, LPCWSTR, DWORD,
    LPCWSTR, LPWSTR, DWORD, LPVOID, LPCWSTR,
    LPSTARTUPINFOW, LPPROCESS_INFORMATION);

using CreateProcessWithTokenW_t = BOOL(WINAPI*)(
    HANDLE, DWORD, LPCWSTR, LPWSTR, DWORD, LPVOID, LPCWSTR,
    LPSTARTUPINFOW, LPPROCESS_INFORMATION);

using WinExec_t = UINT(WINAPI*)(LPCSTR, UINT);

// Layout of the shell's own process-launch request (SHCREATEPROCESSINFOW).
// Declared locally under a private name to avoid clashing with SDK headers.
struct ReboundShCreateProcessInfoW
{
    DWORD cbSize;
    ULONG fMask;
    HWND hwnd;
    PCWSTR pszFile;
    PCWSTR pszParameters;
    PCWSTR pszCurrentDirectory;
    HANDLE hUserToken;
    LPSECURITY_ATTRIBUTES lpProcessAttributes;
    LPSECURITY_ATTRIBUTES lpThreadAttributes;
    BOOL bInheritHandles;
    DWORD dwCreationFlags;
    LPSTARTUPINFOW lpStartupInfo;
    LPPROCESS_INFORMATION lpProcessInformation;
};

using SHCreateProcessAsUserW_t = BOOL(WINAPI*)(ReboundShCreateProcessInfoW*);

static ShellAboutW_t g_originalShellAboutW = nullptr;
static ShellAboutA_t g_originalShellAboutA = nullptr;
static ShellExecuteW_t g_originalShellExecuteW = nullptr;
static ShellExecuteA_t g_originalShellExecuteA = nullptr;
static ShellExecuteExW_t g_originalShellExecuteExW = nullptr;
static ShellExecuteExA_t g_originalShellExecuteExA = nullptr;
static CreateProcessW_t g_originalCreateProcessW = nullptr;
static CreateProcessA_t g_originalCreateProcessA = nullptr;
static CreateProcessAsUserW_t g_originalCreateProcessAsUserW = nullptr;
static CreateProcessAsUserA_t g_originalCreateProcessAsUserA = nullptr;
static CreateProcessInternalW_t g_originalCreateProcessInternalW = nullptr;
static CreateProcessWithLogonW_t g_originalCreateProcessWithLogonW = nullptr;
static CreateProcessWithTokenW_t g_originalCreateProcessWithTokenW = nullptr;
static WinExec_t g_originalWinExec = nullptr;
static SHCreateProcessAsUserW_t g_originalSHCreateProcessAsUserW = nullptr;

static constexpr wchar_t REBOUND_ABOUT_AUMID[] = L"Rebound.About_rcz2tbwv5qzb8!App";

// Set while this thread is inside our own activation call, so that anything
// the activation path launches is never redirected again.
static thread_local bool t_inRedirect = false;

// ---------------------------------------------------------------------------
// Diagnostics (view with DebugView, filter on "RebAbout")
// ---------------------------------------------------------------------------

static bool ContainsWinver(const wchar_t* text)
{
    if (!text || !*text)
        return false;

    std::wstring lowered(text);

    for (wchar_t& c : lowered)
        c = static_cast<wchar_t>(towlower(c));

    return lowered.find(L"winver") != std::wstring::npos;
}

// ---------------------------------------------------------------------------
// Activation
// ---------------------------------------------------------------------------

/// <summary>
/// Activates Rebound About through its package identity.
/// A packaged WinUI 3 app must be activated, never started with a plain
/// CreateProcess on its exe, or it runs without identity and exits.
/// </summary>
static HRESULT ActivateReboundAbout(DWORD* processIdOut = nullptr)
{
    if (processIdOut)
        *processIdOut = 0;

    const HRESULT initResult =
        CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

    const bool mustUninitialize =
        SUCCEEDED(initResult);

    if (FAILED(initResult) &&
        initResult != RPC_E_CHANGED_MODE)
    {
        return initResult;
    }

    IApplicationActivationManager* manager = nullptr;

    HRESULT hr = CoCreateInstance(
        CLSID_ApplicationActivationManager,
        nullptr,
        CLSCTX_LOCAL_SERVER,
        IID_PPV_ARGS(&manager));

    if (SUCCEEDED(hr))
    {
        // Transfer foreground activation permission to the
        // Application Activation Manager's COM server.
        const HRESULT foregroundHr =
            CoAllowSetForegroundWindow(manager, nullptr);

        DWORD processId = 0;

        hr = manager->ActivateApplication(
            REBOUND_ABOUT_AUMID,
            nullptr,
            AO_NONE,
            &processId);

        if (SUCCEEDED(hr) && processIdOut)
            *processIdOut = processId;

        manager->Release();
    }

    if (mustUninitialize)
        CoUninitialize();

    return hr;
}

/// <summary>
/// Opens a handle to some thread of the given process (for callers that
/// expect a valid PROCESS_INFORMATION.hThread).
/// </summary>
static HANDLE OpenAnyThreadOfProcess(DWORD processId, DWORD* threadIdOut)
{
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);

    if (snapshot == INVALID_HANDLE_VALUE)
        return nullptr;

    HANDLE thread = nullptr;

    THREADENTRY32 entry{};
    entry.dwSize = sizeof(entry);

    if (Thread32First(snapshot, &entry))
    {
        do
        {
            if (entry.th32OwnerProcessID != processId)
                continue;

            thread = OpenThread(
                THREAD_QUERY_LIMITED_INFORMATION |
                SYNCHRONIZE |
                THREAD_SUSPEND_RESUME,
                FALSE,
                entry.th32ThreadID);

            if (thread)
            {
                if (threadIdOut)
                    *threadIdOut = entry.th32ThreadID;
                break;
            }
        } while (Thread32Next(snapshot, &entry));
    }

    CloseHandle(snapshot);
    return thread;
}

static bool IsRunning()
{
    return g_state.load(std::memory_order_acquire) ==
        LifecycleState::Running;
}

/// <summary>
/// Activates Rebound About in place of a winver launch. When the caller needs
/// a PROCESS_INFORMATION, it is filled with real handles to the activated
/// process so waiting/closing behaves like a normal launch.
/// </summary>
static bool RedirectWinverLaunch(PROCESS_INFORMATION* pi)
{
    if (t_inRedirect || !IsRunning())
        return false;

    t_inRedirect = true;

    DWORD processId = 0;
    const HRESULT hr = ActivateReboundAbout(&processId);

    if (SUCCEEDED(hr) && pi)
    {
        *pi = PROCESS_INFORMATION{};
        pi->dwProcessId = processId;

        if (processId != 0)
        {
            pi->hProcess = OpenProcess(
                PROCESS_QUERY_LIMITED_INFORMATION |
                SYNCHRONIZE |
                PROCESS_TERMINATE,
                FALSE,
                processId);

            pi->hThread = OpenAnyThreadOfProcess(
                processId, &pi->dwThreadId);
        }
    }

    t_inRedirect = false;
    return SUCCEEDED(hr);
}

// ---------------------------------------------------------------------------
// Winver detection
// ---------------------------------------------------------------------------

static bool IsTrustedWinverDirectory(std::wstring dir)
{
    for (wchar_t& c : dir)
        if (c == L'/')
            c = L'\\';

    while (!dir.empty() && dir.back() == L'\\')
        dir.pop_back();

    if (dir.empty())
        return false;

    wchar_t buffer[MAX_PATH]{};
    UINT length = 0;

    length = GetSystemDirectoryW(buffer, MAX_PATH);
    if (length && length < MAX_PATH && _wcsicmp(dir.c_str(), buffer) == 0)
        return true;

    length = GetSystemWow64DirectoryW(buffer, MAX_PATH);
    if (length && length < MAX_PATH && _wcsicmp(dir.c_str(), buffer) == 0)
        return true;

    length = GetWindowsDirectoryW(buffer, MAX_PATH);
    if (length && length < MAX_PATH)
    {
        std::wstring windows(buffer);

        if (_wcsicmp(dir.c_str(), windows.c_str()) == 0)
            return true;

        windows += L"\\Sysnative";
        if (_wcsicmp(dir.c_str(), windows.c_str()) == 0)
            return true;
    }

    return false;
}

/// <summary>
/// True for "winver", "winver.exe" or a system-directory path to either.
/// Extensionless names are matched because CreateProcess and ShellExecute
/// both resolve them (the Start menu launches plain "winver").
/// </summary>
static bool IsWinverPathW(LPCWSTR path)
{
    if (!path || !*path)
        return false;

    std::wstring value(path);

    if (value.size() >= 2 &&
        value.front() == L'"' &&
        value.back() == L'"')
        value = value.substr(1, value.size() - 2);

    if (value.compare(0, 4, L"\\\\?\\") == 0)
        value.erase(0, 4);

    const size_t separator = value.find_last_of(L"\\/");
    const wchar_t* filename = value.c_str() +
        (separator == std::wstring::npos ? 0 : separator + 1);

    if (_wcsicmp(filename, L"winver.exe") != 0 &&
        _wcsicmp(filename, L"winver") != 0)
        return false;

    if (separator == std::wstring::npos)
        return true;

    return IsTrustedWinverDirectory(value.substr(0, separator));
}

static bool AnsiToWide(LPCSTR input, std::wstring& output)
{
    output.clear();

    if (!input)
        return true;

    const int length = MultiByteToWideChar(CP_ACP, 0, input, -1, nullptr, 0);

    if (length <= 0)
        return false;

    output.resize(static_cast<size_t>(length));

    if (!MultiByteToWideChar(CP_ACP, 0, input, -1, output.data(), length))
        return false;

    output.resize(static_cast<size_t>(length) - 1);
    return true;
}

static bool IsWinverPathA(LPCSTR path)
{
    if (!path || !*path)
        return false;

    std::wstring wide;
    return AnsiToWide(path, wide) && IsWinverPathW(wide.c_str());
}

static size_t FirstArgumentEndW(const wchar_t* commandLine)
{
    bool quoted = false;
    size_t i = 0;

    while (commandLine && commandLine[i])
    {
        if (commandLine[i] == L'\\')
        {
            size_t j = i;
            while (commandLine[j] == L'\\')
                ++j;

            if (commandLine[j] == L'"')
            {
                if ((j - i) % 2 == 0)
                    quoted = !quoted;

                i = j + 1;
                continue;
            }

            i = j;
            continue;
        }

        if (commandLine[i] == L'"')
        {
            quoted = !quoted;
            ++i;
            continue;
        }

        if (!quoted && iswspace(commandLine[i]))
            break;

        ++i;
    }

    return i;
}

/// <summary>
/// Decides whether a CreateProcess-style launch targets winver.
/// Debugger launches are left alone. CREATE_SUSPENDED is still redirected:
/// the shell uses it routinely, and the thread handle we return accepts
/// ResumeThread (a no-op on the already-running activated app).
/// </summary>
static bool ShouldRedirectWImpl(LPCWSTR applicationName, LPCWSTR commandLine, DWORD creationFlags)
{
    if (creationFlags &
        (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS))
        return false;

    if (applicationName && *applicationName)
        return IsWinverPathW(applicationName);

    if (commandLine && *commandLine)
    {
        const size_t end = FirstArgumentEndW(commandLine);
        std::wstring firstArgument(commandLine, end);
        return IsWinverPathW(firstArgument.c_str());
    }

    return false;
}

static bool ShouldRedirectW(LPCWSTR applicationName, LPCWSTR commandLine, DWORD creationFlags)
{
    const bool result =
        ShouldRedirectWImpl(applicationName, commandLine, creationFlags);

    return result;
}

static bool ShouldRedirectA(LPCSTR applicationName, LPCSTR commandLine, DWORD creationFlags)
{
    if (creationFlags &
        (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS))
        return false;

    if (applicationName && *applicationName)
        return IsWinverPathA(applicationName);

    if (commandLine && *commandLine)
    {
        std::wstring wide;

        if (!AnsiToWide(commandLine, wide))
            return false;

        return ShouldRedirectW(nullptr, wide.c_str(), 0);
    }

    return false;
}

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

// ---------------------------------------------------------------------------
// ShellAbout hooks
// ---------------------------------------------------------------------------

static BOOL WINAPI ShellAboutW_Hook(
    HWND hwnd,
    LPCWSTR app,
    LPCWSTR other,
    HICON icon)
{
    HookCallGuard guard;

    if (IsRunning() && !t_inRedirect && RedirectWinverLaunch(nullptr))
        return TRUE;

    return g_originalShellAboutW
        ? g_originalShellAboutW(hwnd, app, other, icon)
        : FALSE;
}

static BOOL WINAPI ShellAboutA_Hook(
    HWND hwnd,
    LPCSTR app,
    LPCSTR other,
    HICON icon)
{
    HookCallGuard guard;

    if (IsRunning() && !t_inRedirect && RedirectWinverLaunch(nullptr))
        return TRUE;

    return g_originalShellAboutA
        ? g_originalShellAboutA(hwnd, app, other, icon)
        : FALSE;
}

// ---------------------------------------------------------------------------
// ShellExecute hooks
// ---------------------------------------------------------------------------

/// <summary>
/// Shared success path for SHELLEXECUTEINFOA/W: honours
/// SEE_MASK_NOCLOSEPROCESS by handing back a real process handle.
/// </summary>
template <typename TInfo>
static bool TryRedirectShellExecuteInfo(TInfo* info)
{
    const bool wantsProcess =
        (info->fMask & SEE_MASK_NOCLOSEPROCESS) != 0;

    PROCESS_INFORMATION pi{};

    if (!RedirectWinverLaunch(wantsProcess ? &pi : nullptr))
        return false;

    if (wantsProcess)
        info->hProcess = pi.hProcess;

    if (pi.hThread)
        CloseHandle(pi.hThread);

    info->hInstApp = reinterpret_cast<HINSTANCE>(static_cast<INT_PTR>(33));
    return true;
}

/// <summary>
/// Explorer, Search and file dialogs launch items by PIDL and leave lpFile
/// empty, so the target has to be resolved from lpIDList in that case.
/// </summary>
static bool IsWinverIdList(LPVOID idList)
{
    wchar_t path[MAX_PATH]{};

    const bool resolved = SHGetPathFromIDListW(
        static_cast<PCIDLIST_ABSOLUTE>(idList), path) != FALSE;

    const bool result = resolved && IsWinverPathW(path);

    return result;
}

static bool IsWinverShellExecuteW(const SHELLEXECUTEINFOW* info)
{
    if ((info->fMask & SEE_MASK_IDLIST) && info->lpIDList)
        return IsWinverIdList(info->lpIDList);

    const bool result = IsWinverPathW(info->lpFile);

    return result;
}

static bool IsWinverShellExecuteA(const SHELLEXECUTEINFOA* info)
{
    if ((info->fMask & SEE_MASK_IDLIST) && info->lpIDList)
        return IsWinverIdList(info->lpIDList);

    return IsWinverPathA(info->lpFile);
}

static HINSTANCE WINAPI ShellExecuteW_Hook(
    HWND hwnd,
    LPCWSTR operation,
    LPCWSTR file,
    LPCWSTR parameters,
    LPCWSTR directory,
    INT showCommand)
{
    HookCallGuard guard;

    if (IsRunning() && IsWinverPathW(file) && RedirectWinverLaunch(nullptr))
        return reinterpret_cast<HINSTANCE>(static_cast<INT_PTR>(33));

    if (!g_originalShellExecuteW)
        return reinterpret_cast<HINSTANCE>(static_cast<INT_PTR>(SE_ERR_ACCESSDENIED));

    return g_originalShellExecuteW(
        hwnd, operation, file, parameters, directory, showCommand);
}

static HINSTANCE WINAPI ShellExecuteA_Hook(
    HWND hwnd,
    LPCSTR operation,
    LPCSTR file,
    LPCSTR parameters,
    LPCSTR directory,
    INT showCommand)
{
    HookCallGuard guard;

    if (IsRunning() && IsWinverPathA(file) && RedirectWinverLaunch(nullptr))
        return reinterpret_cast<HINSTANCE>(static_cast<INT_PTR>(33));

    if (!g_originalShellExecuteA)
        return reinterpret_cast<HINSTANCE>(static_cast<INT_PTR>(SE_ERR_ACCESSDENIED));

    return g_originalShellExecuteA(
        hwnd, operation, file, parameters, directory, showCommand);
}

static BOOL WINAPI ShellExecuteExW_Hook(
    SHELLEXECUTEINFOW* info)
{
    HookCallGuard guard;

    if (IsRunning() &&
        info &&
        IsWinverShellExecuteW(info) &&
        TryRedirectShellExecuteInfo(info))
    {
        return TRUE;
    }

    return g_originalShellExecuteExW
        ? g_originalShellExecuteExW(info)
        : FALSE;
}

static BOOL WINAPI ShellExecuteExA_Hook(
    SHELLEXECUTEINFOA* info)
{
    HookCallGuard guard;

    if (IsRunning() &&
        info &&
        IsWinverShellExecuteA(info) &&
        TryRedirectShellExecuteInfo(info))
    {
        return TRUE;
    }

    return g_originalShellExecuteExA
        ? g_originalShellExecuteExA(info)
        : FALSE;
}

// ---------------------------------------------------------------------------
// CreateProcess-family hooks
// ---------------------------------------------------------------------------

static BOOL WINAPI CreateProcessW_Hook(
    LPCWSTR applicationName,
    LPWSTR commandLine,
    LPSECURITY_ATTRIBUTES processAttributes,
    LPSECURITY_ATTRIBUTES threadAttributes,
    BOOL inheritHandles,
    DWORD creationFlags,
    LPVOID environment,
    LPCWSTR currentDirectory,
    LPSTARTUPINFOW startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectW(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessW(
        applicationName, commandLine,
        processAttributes, threadAttributes,
        inheritHandles, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

static BOOL WINAPI CreateProcessA_Hook(
    LPCSTR applicationName,
    LPSTR commandLine,
    LPSECURITY_ATTRIBUTES processAttributes,
    LPSECURITY_ATTRIBUTES threadAttributes,
    BOOL inheritHandles,
    DWORD creationFlags,
    LPVOID environment,
    LPCSTR currentDirectory,
    LPSTARTUPINFOA startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessA)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectA(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessA(
        applicationName, commandLine,
        processAttributes, threadAttributes,
        inheritHandles, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

static BOOL WINAPI CreateProcessAsUserW_Hook(
    HANDLE token,
    LPCWSTR applicationName,
    LPWSTR commandLine,
    LPSECURITY_ATTRIBUTES processAttributes,
    LPSECURITY_ATTRIBUTES threadAttributes,
    BOOL inheritHandles,
    DWORD creationFlags,
    LPVOID environment,
    LPCWSTR currentDirectory,
    LPSTARTUPINFOW startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessAsUserW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectW(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessAsUserW(
        token, applicationName, commandLine,
        processAttributes, threadAttributes,
        inheritHandles, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

static BOOL WINAPI CreateProcessAsUserA_Hook(
    HANDLE token,
    LPCSTR applicationName,
    LPSTR commandLine,
    LPSECURITY_ATTRIBUTES processAttributes,
    LPSECURITY_ATTRIBUTES threadAttributes,
    BOOL inheritHandles,
    DWORD creationFlags,
    LPVOID environment,
    LPCSTR currentDirectory,
    LPSTARTUPINFOA startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessAsUserA)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectA(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessAsUserA(
        token, applicationName, commandLine,
        processAttributes, threadAttributes,
        inheritHandles, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

static BOOL WINAPI CreateProcessInternalW_Hook(
    HANDLE token,
    LPCWSTR applicationName,
    LPWSTR commandLine,
    LPSECURITY_ATTRIBUTES processAttributes,
    LPSECURITY_ATTRIBUTES threadAttributes,
    BOOL inheritHandles,
    DWORD creationFlags,
    LPVOID environment,
    LPCWSTR currentDirectory,
    LPSTARTUPINFOW startupInfo,
    LPPROCESS_INFORMATION processInformation,
    PHANDLE restrictedToken)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessInternalW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectW(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        if (restrictedToken)
            *restrictedToken = nullptr;

        return TRUE;
    }

    return g_originalCreateProcessInternalW(
        token, applicationName, commandLine,
        processAttributes, threadAttributes,
        inheritHandles, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation, restrictedToken);
}

static BOOL WINAPI CreateProcessWithLogonW_Hook(
    LPCWSTR userName,
    LPCWSTR domain,
    LPCWSTR password,
    DWORD logonFlags,
    LPCWSTR applicationName,
    LPWSTR commandLine,
    DWORD creationFlags,
    LPVOID environment,
    LPCWSTR currentDirectory,
    LPSTARTUPINFOW startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessWithLogonW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectW(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessWithLogonW(
        userName, domain, password, logonFlags,
        applicationName, commandLine, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

static BOOL WINAPI CreateProcessWithTokenW_Hook(
    HANDLE token,
    DWORD logonFlags,
    LPCWSTR applicationName,
    LPWSTR commandLine,
    DWORD creationFlags,
    LPVOID environment,
    LPCWSTR currentDirectory,
    LPSTARTUPINFOW startupInfo,
    LPPROCESS_INFORMATION processInformation)
{
    HookCallGuard guard;

    if (!g_originalCreateProcessWithTokenW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectW(applicationName, commandLine, creationFlags) &&
        RedirectWinverLaunch(processInformation))
    {
        return TRUE;
    }

    return g_originalCreateProcessWithTokenW(
        token, logonFlags,
        applicationName, commandLine, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

/// <summary>
/// The shell's internal launcher: ShellExecute, RunFileDlg and Explorer end
/// up here before reaching CreateProcess*.
/// </summary>
static BOOL WINAPI SHCreateProcessAsUserW_Hook(
    ReboundShCreateProcessInfoW* info)
{
    HookCallGuard guard;

    if (!g_originalSHCreateProcessAsUserW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (IsRunning() &&
        !t_inRedirect &&
        info &&
        ShouldRedirectW(info->pszFile, nullptr, info->dwCreationFlags) &&
        RedirectWinverLaunch(info->lpProcessInformation))
    {
        return TRUE;
    }

    return g_originalSHCreateProcessAsUserW(info);
}

static UINT WINAPI WinExec_Hook(
    LPCSTR commandLine,
    UINT showCommand)
{
    HookCallGuard guard;

    if (IsRunning() &&
        !t_inRedirect &&
        ShouldRedirectA(nullptr, commandLine, 0) &&
        RedirectWinverLaunch(nullptr))
    {
        return 33; // Anything above 31 means success.
    }

    return g_originalWinExec
        ? g_originalWinExec(commandLine, showCommand)
        : 0;
}

// ---------------------------------------------------------------------------
// Hook table
// ---------------------------------------------------------------------------

struct HookEntry
{
    const wchar_t* module;
    const char* name;
    void* detour;
    void** original;
    bool required;

    FARPROC target;
    bool created;
};

// Required hooks keep the previous behaviour (any failure aborts and shuts
// the DLL down cleanly). Optional ones are skipped if the export is missing
// or cannot be hooked on this Windows build.
static HookEntry g_hooks[] =
{
    { L"shell32.dll",   "ShellAboutW",   reinterpret_cast<void*>(&ShellAboutW_Hook),   reinterpret_cast<void**>(&g_originalShellAboutW),   true,  nullptr, false },
    { L"shell32.dll",   "ShellAboutA",   reinterpret_cast<void*>(&ShellAboutA_Hook),   reinterpret_cast<void**>(&g_originalShellAboutA),   true,  nullptr, false },
    { L"shell32.dll",   "ShellExecuteExW", reinterpret_cast<void*>(&ShellExecuteExW_Hook), reinterpret_cast<void**>(&g_originalShellExecuteExW), true, nullptr, false },
    { L"shell32.dll",   "ShellExecuteExA", reinterpret_cast<void*>(&ShellExecuteExA_Hook), reinterpret_cast<void**>(&g_originalShellExecuteExA), false, nullptr, false },
    { L"shell32.dll",   "ShellExecuteW", reinterpret_cast<void*>(&ShellExecuteW_Hook), reinterpret_cast<void**>(&g_originalShellExecuteW), false, nullptr, false },
    { L"shell32.dll",   "ShellExecuteA", reinterpret_cast<void*>(&ShellExecuteA_Hook), reinterpret_cast<void**>(&g_originalShellExecuteA), false, nullptr, false },

    { L"KernelBase.dll", "CreateProcessW", reinterpret_cast<void*>(&CreateProcessW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessW), true, nullptr, false },
    { L"KernelBase.dll", "CreateProcessA", reinterpret_cast<void*>(&CreateProcessA_Hook), reinterpret_cast<void**>(&g_originalCreateProcessA), true, nullptr, false },
    { L"KernelBase.dll", "CreateProcessAsUserW", reinterpret_cast<void*>(&CreateProcessAsUserW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessAsUserW), false, nullptr, false },
    { L"KernelBase.dll", "CreateProcessAsUserA", reinterpret_cast<void*>(&CreateProcessAsUserA_Hook), reinterpret_cast<void**>(&g_originalCreateProcessAsUserA), false, nullptr, false },
    { L"KernelBase.dll", "CreateProcessInternalW", reinterpret_cast<void*>(&CreateProcessInternalW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessInternalW), false, nullptr, false },
    { L"kernel32.dll",   "WinExec", reinterpret_cast<void*>(&WinExec_Hook), reinterpret_cast<void**>(&g_originalWinExec), false, nullptr, false },
    { L"shell32.dll",    "SHCreateProcessAsUserW", reinterpret_cast<void*>(&SHCreateProcessAsUserW_Hook), reinterpret_cast<void**>(&g_originalSHCreateProcessAsUserW), false, nullptr, false },

    { L"advapi32.dll",  "CreateProcessWithLogonW", reinterpret_cast<void*>(&CreateProcessWithLogonW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessWithLogonW), false, nullptr, false },
    { L"advapi32.dll",  "CreateProcessWithTokenW", reinterpret_cast<void*>(&CreateProcessWithTokenW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessWithTokenW), false, nullptr, false },
};

static bool InstallAboutHooks()
{
    // Mark early: if installation fails half-way, the shutdown coordinator
    // must still disable and remove whatever was already created.
    g_hooksInstalled.store(true, std::memory_order_release);

    for (HookEntry& hook : g_hooks)
    {
        HMODULE module = LoadLibraryW(hook.module);

        if (!module)
        {
            if (hook.required)
                return false;
            continue;
        }

        g_loadedModules.push_back(module);

        hook.target = GetProcAddress(module, hook.name);

        if (!hook.target)
        {
            if (hook.required)
                return false;
            continue;
        }

        // Forwarded exports can resolve to a target another entry already
        // owns (for example WinExec). Hook each address once.
        bool duplicate = false;

        for (const HookEntry& other : g_hooks)
        {
            if (&other == &hook)
                break;

            if (other.created && other.target == hook.target)
            {
                duplicate = true;
                break;
            }
        }

        if (duplicate)
        {
            hook.target = nullptr;
            continue;
        }

        if (MH_CreateHook(hook.target, hook.detour, hook.original) != MH_OK)
        {
            hook.target = nullptr;

            if (hook.required)
                return false;
            continue;
        }

        hook.created = true;
    }

    for (HookEntry& hook : g_hooks)
    {
        if (!hook.created)
            continue;

        const MH_STATUS enableStatus = MH_EnableHook(hook.target);

        if (enableStatus != MH_OK && hook.required)
            return false;
    }

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

    if (!InstallAboutHooks())
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
                msg.find("Rebound.Forge.Hooks.About.dll") != std::string::npos)
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
        // Disable every hook before waiting for active callbacks.
        for (HookEntry& hook : g_hooks)
        {
            if (!hook.created)
                continue;

            MH_STATUS status = MH_DisableHook(hook.target);

            if (status != MH_OK && status != MH_ERROR_DISABLED)
            {
                OutputDebugStringW(
                    L"Rebound About hook: failed to disable; retaining DLL.\n");
                return 1;
            }
        }

        if (!WaitForCallbacksToDrain())
            return 1;

        // Remove hooks only after all callbacks have drained.
        for (HookEntry& hook : g_hooks)
        {
            if (!hook.created)
                continue;

            MH_STATUS status = MH_RemoveHook(hook.target);

            if (status != MH_OK)
            {
                OutputDebugStringW(
                    L"Rebound About hook: failed to remove; retaining DLL.\n");
                return 1;
            }

            hook.created = false;
        }

        g_hooksInstalled.store(false, std::memory_order_release);

        for (HookEntry& hook : g_hooks)
        {
            if (hook.original)
                *hook.original = nullptr;

            hook.target = nullptr;
        }
    }

    for (HMODULE module : g_loadedModules)
        FreeLibrary(module);

    g_loadedModules.clear();

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
    if (reason != DLL_PROCESS_ATTACH)
        return TRUE;

    DisableThreadLibraryCalls(hinstDLL);
    g_moduleHandle = hinstDLL;

    wchar_t mutexName[256]{};
    swprintf_s(
        mutexName,
        L"ReboundAbout_LaunchHook_%lu",
        GetCurrentProcessId());

    SetLastError(ERROR_SUCCESS);
    g_initMutex = CreateMutexW(nullptr, FALSE, mutexName);
    DWORD mutexError = GetLastError();

    if (!g_initMutex)
        return FALSE;

    if (mutexError == ERROR_ALREADY_EXISTS)
    {
        CloseHandle(g_initMutex);
        g_initMutex = nullptr;
        return FALSE;
    }

    g_shutdownEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    DWORD shutdownEventError =
        g_shutdownEvent ? ERROR_SUCCESS : GetLastError();

    g_callbacksDrainedEvent = CreateEventW(nullptr, TRUE, TRUE, nullptr);
    DWORD callbacksEventError =
        g_callbacksDrainedEvent ? ERROR_SUCCESS : GetLastError();

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
        nullptr, 0, ShutdownCoordinatorThread, nullptr, 0, nullptr);

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

    return TRUE;
}
