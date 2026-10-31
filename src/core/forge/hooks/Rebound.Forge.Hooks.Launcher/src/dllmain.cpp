// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

#include "pch.h"
#include <windows.h>
#include <appmodel.h>
#include <tlhelp32.h>
#include <stdio.h>
#include <shlobj.h>
#include <string>
#include <atomic>
#include <shellapi.h>
#include <shobjidl.h>
#include <objbase.h>
#include <cwchar>
#include <cstring>
#include <vector>
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

// Number of hook detours currently executing (including the time spent inside
// the original function they forwarded to).
static std::atomic<unsigned long> g_activeCallbacks{ 0 };

// Modules we pinned with LoadLibraryW so hook targets stay mapped for the
// whole lifetime of the hooks. Touched only by the init thread and, after it
// has been joined, by the shutdown coordinator.
static std::vector<HMODULE> g_loadedModules;

// ---------------------------------------------------------------------------
// Hooked function signatures
// ---------------------------------------------------------------------------

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
static SHCreateProcessAsUserW_t g_originalSHCreateProcessAsUserW = nullptr;

// Set while this thread is inside one of our own redirected launches, so that
// anything that launch triggers further down the stack is never redirected
// a second time.
static thread_local bool t_inRedirect = false;

class RedirectGuard
{
public:
    RedirectGuard() : previous(t_inRedirect)
    {
        t_inRedirect = true;
    }

    ~RedirectGuard()
    {
        t_inRedirect = previous;
    }

    RedirectGuard(const RedirectGuard&) = delete;
    RedirectGuard& operator=(const RedirectGuard&) = delete;

private:
    bool previous;
};

/// <summary>
/// Counts a detour as "in flight". It is the first object constructed in
/// every hook, so the shutdown coordinator can tell when no thread is still
/// running (or about to return into) this DLL's code.
/// </summary>
class HookCallGuard
{
public:
    HookCallGuard()
    {
        g_activeCallbacks.fetch_add(1, std::memory_order_acq_rel);
    }

    ~HookCallGuard()
    {
        g_activeCallbacks.fetch_sub(1, std::memory_order_acq_rel);
    }

    HookCallGuard(const HookCallGuard&) = delete;
    HookCallGuard& operator=(const HookCallGuard&) = delete;
};

static bool IsRunning()
{
    return g_state.load(std::memory_order_acquire) ==
        LifecycleState::Running;
}

static bool ShouldIntercept()
{
    return IsRunning() && !t_inRedirect;
}

enum class RedirectResult
{
    NotHandled, // Nothing happened; call the original with the caller's arguments.
    Succeeded,  // Redirected and the caller's out parameters are filled in.
    Failed      // Redirect had side effects but could not honour the contract.
};

// ---------------------------------------------------------------------------
// String helpers
// ---------------------------------------------------------------------------

static bool AnsiToWide(LPCSTR input, std::wstring& output)
{
    output.clear();

    if (!input)
        return true;

    if (!*input)
        return true;

    const int length = MultiByteToWideChar(CP_ACP, 0, input, -1, nullptr, 0);

    if (length <= 0)
        return false;

    output.resize(static_cast<size_t>(length));

    if (!MultiByteToWideChar(CP_ACP, 0, input, -1, output.data(), length))
    {
        output.clear();
        return false;
    }

    output.resize(static_cast<size_t>(length) - 1);
    return true;
}

/// <summary>
/// Converts to the active ANSI code page without loss. Fails (so the caller
/// falls back to the original call) if any character cannot be represented.
/// </summary>
static bool WideToAnsi(
    const std::wstring& input,
    std::string& output)
{
    output.clear();

    if (input.empty())
        return false;

    // With a UTF-8 active code page the flags must be 0 and the
    // used-default-char pointer must be null, otherwise the call fails.
    const bool utf8 = GetACP() == CP_UTF8;
    const DWORD flags = utf8 ? 0 : WC_NO_BEST_FIT_CHARS;

    BOOL used = FALSE;
    BOOL* usedPtr = utf8 ? nullptr : &used;

    const int length = WideCharToMultiByte(
        CP_ACP, flags,
        input.c_str(), static_cast<int>(input.size()),
        nullptr, 0, nullptr, usedPtr);

    if (length <= 0 || used)
        return false;

    std::string buffer(static_cast<size_t>(length), '\0');

    used = FALSE;

    const int result = WideCharToMultiByte(
        CP_ACP, flags,
        input.c_str(), static_cast<int>(input.size()),
        buffer.data(), length, nullptr, usedPtr);

    if (result != length || used)
        return false;

    output = std::move(buffer);
    return true;
}

static std::wstring Trim(const std::wstring& value)
{
    const size_t first = value.find_first_not_of(L" \t\r\n");

    if (first == std::wstring::npos)
        return std::wstring();

    const size_t last = value.find_last_not_of(L" \t\r\n");
    return value.substr(first, last - first + 1);
}

static std::wstring TrimLeading(const wchar_t* text)
{
    if (!text)
        return std::wstring();

    while (*text == L' ' || *text == L'\t')
        ++text;

    return std::wstring(text);
}

static bool IsAsciiAlpha(wchar_t c)
{
    return (c >= L'a' && c <= L'z') || (c >= L'A' && c <= L'Z');
}

static bool IsAsciiDigit(wchar_t c)
{
    return c >= L'0' && c <= L'9';
}

/// <summary>
/// True for "scheme:..." strings with a scheme of at least two characters
/// (a single letter is a drive letter, not a scheme).
/// </summary>
static bool LooksLikeUri(const std::wstring& value)
{
    const size_t colon = value.find(L':');

    if (colon == std::wstring::npos || colon < 2)
        return false;

    if (!IsAsciiAlpha(value[0]))
        return false;

    for (size_t i = 1; i < colon; ++i)
    {
        const wchar_t c = value[i];

        if (!IsAsciiAlpha(c) && !IsAsciiDigit(c) &&
            c != L'+' && c != L'-' && c != L'.')
        {
            return false;
        }
    }

    return true;
}

static bool IsReservedDeviceName(const std::wstring& name)
{
    std::wstring stem = name.substr(0, name.find(L'.'));

    while (!stem.empty() && stem.back() == L' ')
        stem.pop_back();

    static const wchar_t* const reserved[] =
    {
        L"CON", L"PRN", L"AUX", L"NUL"
    };

    for (const wchar_t* candidate : reserved)
    {
        if (_wcsicmp(stem.c_str(), candidate) == 0)
            return true;
    }

    return stem.size() == 4 &&
        (_wcsnicmp(stem.c_str(), L"COM", 3) == 0 ||
            _wcsnicmp(stem.c_str(), L"LPT", 3) == 0) &&
        stem[3] >= L'1' && stem[3] <= L'9';
}

// ---------------------------------------------------------------------------
// Program associations
// ---------------------------------------------------------------------------

enum class TargetKind
{
    Path,
    Uri,
    Aumid
};

struct Target
{
    TargetKind kind = TargetKind::Path;
    std::wstring value;
};

/// <summary>
/// Maps a requested program (path or bare name) to its .prassoc file.
/// Only the final path component is used, a missing extension gets ".exe"
/// (as CreateProcess does), and anything that is not a plain file name is
/// rejected so a crafted name can never address a stream, a device or a
/// location outside the associations folder.
/// </summary>
static bool BuildAssociationFilePath(
    const std::wstring& requested,
    std::wstring& associationPath)
{
    associationPath.clear();

    if (requested.empty() || LooksLikeUri(requested))
        return false;

    const size_t separator = requested.find_last_of(L"\\/");

    std::wstring name = Trim(
        separator == std::wstring::npos
        ? requested
        : requested.substr(separator + 1));

    if (name.empty() || name.size() > 200 ||
        name == L"." || name == L".." || name.back() == L'.')
    {
        return false;
    }

    for (const wchar_t c : name)
    {
        if (c < 0x20 || wcschr(L"<>:\"|?*", c))
            return false;
    }

    if (name.find(L'.') == std::wstring::npos)
        name += L".exe";

    if (IsReservedDeviceName(name))
        return false;

    const DWORD required = GetEnvironmentVariableW(L"LOCALAPPDATA", nullptr, 0);

    if (required == 0)
        return false;

    std::vector<wchar_t> buffer(required);

    const DWORD length = GetEnvironmentVariableW(
        L"LOCALAPPDATA", buffer.data(), required);

    if (length == 0 || length >= required)
        return false;

    associationPath.assign(buffer.data(), length);

    if (associationPath.back() != L'\\')
        associationPath += L'\\';

    associationPath += L"Rebound\\ProgramAssociations\\";
    associationPath += name;
    associationPath += L".prassoc";

    return true;
}

/// <summary>
/// Decodes UTF-8 (with or without BOM) or UTF-16LE with BOM.
/// </summary>
static bool DecodeAssociationText(const std::string& raw, std::wstring& text)
{
    text.clear();

    if (raw.size() >= 2 &&
        static_cast<unsigned char>(raw[0]) == 0xFF &&
        static_cast<unsigned char>(raw[1]) == 0xFE)
    {
        const size_t characters = (raw.size() - 2) / 2;

        if (characters == 0)
            return false;

        text.resize(characters);
        memcpy(text.data(), raw.data() + 2, characters * sizeof(wchar_t));
        return true;
    }

    size_t offset = 0;

    if (raw.size() >= 3 &&
        static_cast<unsigned char>(raw[0]) == 0xEF &&
        static_cast<unsigned char>(raw[1]) == 0xBB &&
        static_cast<unsigned char>(raw[2]) == 0xBF)
    {
        offset = 3;
    }

    if (raw.size() == offset)
        return false;

    const int length = MultiByteToWideChar(
        CP_UTF8, MB_ERR_INVALID_CHARS,
        raw.data() + offset, static_cast<int>(raw.size() - offset),
        nullptr, 0);

    if (length <= 0)
        return false;

    text.resize(static_cast<size_t>(length));

    if (!MultiByteToWideChar(
        CP_UTF8, MB_ERR_INVALID_CHARS,
        raw.data() + offset, static_cast<int>(raw.size() - offset),
        text.data(), length))
    {
        text.clear();
        return false;
    }

    return true;
}

/// <summary>
/// Parses the first recognised PATH= / URI= / AUMID= header line.
/// </summary>
static bool ParseAssociation(const std::wstring& text, Target& target)
{
    size_t position = 0;

    while (position <= text.size())
    {
        size_t eol = text.find(L'\n', position);

        if (eol == std::wstring::npos)
            eol = text.size();

        const std::wstring line = Trim(text.substr(position, eol - position));
        position = eol + 1;

        if (line.empty())
            continue;

        const size_t equals = line.find(L'=');

        if (equals == std::wstring::npos)
            continue;

        const std::wstring key = Trim(line.substr(0, equals));
        std::wstring value = Trim(line.substr(equals + 1));

        if (_wcsicmp(key.c_str(), L"PATH") == 0)
        {
            if (value.size() >= 2 && value.front() == L'"' && value.back() == L'"')
                value = Trim(value.substr(1, value.size() - 2));

            if (value.empty() || value.find(L'"') != std::wstring::npos)
                return false;

            target.kind = TargetKind::Path;
            target.value = std::move(value);
            return true;
        }

        if (_wcsicmp(key.c_str(), L"URI") == 0)
        {
            if (!LooksLikeUri(value))
                return false;

            for (const wchar_t c : value)
            {
                if (c <= 0x20 || c == 0x7F)
                    return false;
            }

            target.kind = TargetKind::Uri;
            target.value = std::move(value);
            return true;
        }

        if (_wcsicmp(key.c_str(), L"AUMID") == 0)
        {
            if (value.empty() ||
                VerifyApplicationUserModelId(value.c_str()) != ERROR_SUCCESS)
            {
                return false;
            }

            target.kind = TargetKind::Aumid;
            target.value = std::move(value);
            return true;
        }
    }

    return false;
}

static bool LookupAssociation(const std::wstring& requested, Target& target)
{
    std::wstring associationPath;

    if (!BuildAssociationFilePath(requested, associationPath))
        return false;

    HANDLE file = CreateFileW(
        associationPath.c_str(),
        GENERIC_READ,
        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
        nullptr,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    if (file == INVALID_HANDLE_VALUE)
        return false;

    LARGE_INTEGER fileSize{};

    if (!GetFileSizeEx(file, &fileSize) ||
        fileSize.QuadPart <= 0 ||
        fileSize.QuadPart > 32768)
    {
        CloseHandle(file);
        return false;
    }

    std::string contents(static_cast<size_t>(fileSize.QuadPart), '\0');

    DWORD bytesRead = 0;

    const BOOL readResult = ReadFile(
        file,
        contents.data(),
        static_cast<DWORD>(contents.size()),
        &bytesRead,
        nullptr);

    CloseHandle(file);

    if (!readResult || bytesRead == 0)
        return false;

    contents.resize(bytesRead);

    std::wstring text;

    if (!DecodeAssociationText(contents, text))
        return false;

    return ParseAssociation(text, target);
}

static bool IsPathTargetUsable(const std::wstring& path)
{
    const bool rooted =
        path.find_first_of(L"\\/") != std::wstring::npos ||
        (path.size() > 1 && path[1] == L':');

    // A bare name is resolved by the system (PATH, App Paths); we can't
    // pre-check it. Rooted paths must exist, so a stale association never
    // breaks the program it was meant to replace.
    if (!rooted)
        return true;

    const DWORD attributes = GetFileAttributesW(path.c_str());

    return attributes != INVALID_FILE_ATTRIBUTES &&
        !(attributes & FILE_ATTRIBUTE_DIRECTORY);
}

/// <summary>
/// Looks up an association for the requested program and rejects the ones
/// that would be pointless or unsafe to follow.
/// </summary>
static bool ResolveRedirect(const std::wstring& requested, Target& target)
{
    if (!LookupAssociation(requested, target))
        return false;

    if (target.kind == TargetKind::Path)
    {
        // Redirecting a program to itself is not a redirect.
        if (_wcsicmp(target.value.c_str(), requested.c_str()) == 0)
            return false;

        if (!IsPathTargetUsable(target.value))
            return false;
    }

    return true;
}

// ---------------------------------------------------------------------------
// Packaged app activation
// ---------------------------------------------------------------------------

static HRESULT ActivateApp(
    LPCWSTR aumid,
    LPCWSTR arguments,
    DWORD* processIdOut)
{
    if (processIdOut)
        *processIdOut = 0;

    if (!aumid || !*aumid)
        return E_INVALIDARG;

    const HRESULT initResult =
        CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

    // S_OK and S_FALSE both need a matching CoUninitialize.
    const bool mustUninitialize = SUCCEEDED(initResult);

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
        CoAllowSetForegroundWindow(manager, nullptr);

        DWORD processId = 0;

        hr = manager->ActivateApplication(
            aumid,
            (arguments && *arguments) ? arguments : nullptr,
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

static HANDLE OpenActivatedProcess(DWORD processId)
{
    if (processId == 0)
        return nullptr;

    // CreateProcess hands out a full-access handle; get as close as the
    // target's security descriptor allows.
    static const DWORD rights[] =
    {
        PROCESS_ALL_ACCESS,
        PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE,
        PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE
    };

    for (const DWORD access : rights)
    {
        HANDLE process = OpenProcess(access, FALSE, processId);

        if (process)
            return process;
    }

    return nullptr;
}

static bool ActivateUri(
    LPCWSTR uri,
    PROCESS_INFORMATION& pi)
{
    pi = {};

    if (!uri || !*uri)
        return false;

    SHELLEXECUTEINFOW info{};
    info.cbSize = sizeof(info);
    info.fMask = SEE_MASK_NOCLOSEPROCESS;
    info.lpVerb = L"open";
    info.lpFile = uri;
    info.nShow = SW_SHOWNORMAL;

    BOOL ok;

    {
        RedirectGuard redirectGuard;
        ok = ShellExecuteExW(&info);
    }

    if (!ok)
        return false;

    // ShellExecuteEx is allowed to succeed without returning a process
    // handle, for example when an existing application handles the URI.
    pi.hProcess = info.hProcess;

    if (pi.hProcess)
        pi.dwProcessId = GetProcessId(pi.hProcess);

    return true;
}

static void CloseProcessInformation(PROCESS_INFORMATION& pi)
{
    if (pi.hThread)
        CloseHandle(pi.hThread);

    if (pi.hProcess)
        CloseHandle(pi.hProcess);

    pi = {};
}

static void FillProcessInformation(
    DWORD processId,
    PROCESS_INFORMATION& pi)
{
    pi = {};
    pi.dwProcessId = processId;

    if (processId)
        pi.hProcess = OpenActivatedProcess(processId);

    // An activated app does not expose its primary thread.
    // hThread and dwThreadId intentionally remain zero.
}

// ---------------------------------------------------------------------------
// Command line helpers
// ---------------------------------------------------------------------------

struct FirstToken
{
    size_t spanStart = 0; // Index of the opening quote or first character.
    size_t spanEnd = 0;   // Index just past the closing quote or last character.
    std::wstring text;    // The program token without quotes.
};

/// <summary>
/// Finds the program token of a command line: the quoted string, or the text
/// up to the first space/tab. (CreateProcess's probing of unquoted paths that
/// contain spaces is intentionally not emulated.)
/// </summary>
static bool ParseFirstToken(const wchar_t* commandLine, FirstToken& token)
{
    if (!commandLine)
        return false;

    size_t i = 0;

    while (commandLine[i] == L' ' || commandLine[i] == L'\t')
        ++i;

    if (!commandLine[i])
        return false;

    token.spanStart = i;

    if (commandLine[i] == L'"')
    {
        const size_t start = i + 1;
        size_t end = start;

        while (commandLine[end] && commandLine[end] != L'"')
            ++end;

        if (!commandLine[end])
            return false;

        token.text.assign(commandLine + start, end - start);
        token.spanEnd = end + 1;
    }
    else
    {
        size_t end = i;

        while (commandLine[end] &&
            commandLine[end] != L' ' &&
            commandLine[end] != L'\t')
            ++end;

        token.text.assign(commandLine + i, end - i);
        token.spanEnd = end;
    }

    return !token.text.empty();
}

// ---------------------------------------------------------------------------
// CreateProcess-family redirection
// ---------------------------------------------------------------------------

/// <summary>
/// Shared redirect logic for every CreateProcess-style entry point.
///
/// PATH:  the program (and only the program) is replaced; every other
///        argument, flag, token and handle is passed through to the real
///        function, so the call keeps its exact contract.
/// AUMID: the app is activated with the caller's arguments and a real
///        PROCESS_INFORMATION is produced. This is only done when the call
///        does not depend on things activation cannot provide (a suspended
///        start, a different user token, redirected standard handles, a
///        debugger, an extended attribute list).
/// URI:   cannot satisfy CreateProcess's contract (no process comes back),
///        so it is left to the shell-level hooks.
/// </summary>
template<typename Launch>
static RedirectResult TryRedirectCreateW(
    LPCWSTR applicationName,
    LPWSTR commandLine,
    DWORD creationFlags,
    DWORD startupFlags,
    LPPROCESS_INFORMATION processInformation,
    bool allowActivation,
    Launch&& launch)
{
    // lpProcessInformation is mandatory for CreateProcess; let the real
    // function report a missing one.
    if (!processInformation)
        return RedirectResult::NotHandled;

    if (creationFlags & (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS))
        return RedirectResult::NotHandled;

    const bool haveApplication = applicationName && *applicationName;

    FirstToken token;
    const bool haveToken = ParseFirstToken(commandLine, token);

    std::wstring requested;

    if (haveApplication)
        requested = applicationName;
    else if (haveToken)
        requested = token.text;
    else
        return RedirectResult::NotHandled;

    Target target;

    if (!ResolveRedirect(requested, target))
        return RedirectResult::NotHandled;

    switch (target.kind)
    {
    case TargetKind::Path:
    {
        std::wstring redirectedCommandLine;
        LPWSTR commandLinePtr = nullptr;

        if (commandLine)
        {
            if (haveApplication)
            {
                // The command line is just argv for the program; keep it.
                redirectedCommandLine = commandLine;
            }
            else
            {
                if (!haveToken ||
                    target.value.find(L'"') != std::wstring::npos)
                {
                    return RedirectResult::NotHandled;
                }

                redirectedCommandLine.append(commandLine, token.spanStart);
                redirectedCommandLine.push_back(L'"');
                redirectedCommandLine.append(target.value);
                redirectedCommandLine.push_back(L'"');
                redirectedCommandLine.append(commandLine + token.spanEnd);
            }

            // CreateProcessW may write to the buffer, so it must be ours.
            commandLinePtr = redirectedCommandLine.data();
        }

        PROCESS_INFORMATION redirectedInfo{};
        BOOL ok;

        {
            RedirectGuard redirectGuard;

            ok = launch(
                haveApplication ? target.value.c_str() : nullptr,
                commandLinePtr,
                &redirectedInfo);
        }

        if (!ok)
        {
            // Nothing was created; fall back to the original program.
            CloseProcessInformation(redirectedInfo);
            return RedirectResult::NotHandled;
        }

        *processInformation = redirectedInfo;
        return RedirectResult::Succeeded;
    }

    case TargetKind::Aumid:
    {
        if (!allowActivation)
            return RedirectResult::NotHandled;

        // Best effort: activation cannot honour CreateProcess-specific
        // flags such as CREATE_SUSPENDED or STARTF_USESTDHANDLES.
        (void)startupFlags;

        std::wstring arguments;

        if (haveToken)
            arguments = TrimLeading(commandLine + token.spanEnd);

        DWORD processId = 0;

        HRESULT hr;

        {
            RedirectGuard redirectGuard;

            hr = ActivateApp(
                target.value.c_str(),
                arguments.c_str(),
                &processId);
        }

        if (FAILED(hr))
            return RedirectResult::NotHandled;

        PROCESS_INFORMATION activatedInfo{};
        FillProcessInformation(processId, activatedInfo);

        *processInformation = activatedInfo;
        return RedirectResult::Succeeded;
    }

    case TargetKind::Uri:
    {
        PROCESS_INFORMATION activatedInfo{};

        if (!ActivateUri(target.value.c_str(), activatedInfo))
            return RedirectResult::NotHandled;

        *processInformation = activatedInfo;
        return RedirectResult::Succeeded;
    }
    default:
        return RedirectResult::NotHandled;
    }
}

template<typename Launch>
static bool RunCreateRedirectW(
    LPCWSTR applicationName,
    LPWSTR commandLine,
    DWORD creationFlags,
    DWORD startupFlags,
    LPPROCESS_INFORMATION processInformation,
    bool allowActivation,
    BOOL& result,
    Launch&& launch)
{
    if (!ShouldIntercept())
        return false;

    RedirectResult outcome = RedirectResult::NotHandled;

    try
    {
        outcome = TryRedirectCreateW(
            applicationName,
            commandLine,
            creationFlags,
            startupFlags,
            processInformation,
            allowActivation,
            launch);
    }
    catch (...)
    {
        return false;
    }

    if (outcome == RedirectResult::NotHandled)
        return false;

    result = outcome == RedirectResult::Succeeded ? TRUE : FALSE;
    return true;
}

/// <summary>
/// ANSI entry points reuse the wide logic. A redirected launch is converted
/// back to ANSI without loss, or not performed at all.
/// </summary>
template<typename LaunchA>
static bool RunCreateRedirectA(
    LPCSTR applicationName,
    LPSTR commandLine,
    DWORD creationFlags,
    DWORD startupFlags,
    LPPROCESS_INFORMATION processInformation,
    bool allowActivation,
    BOOL& result,
    LaunchA&& launchA)
{
    if (!ShouldIntercept())
        return false;

    RedirectResult outcome = RedirectResult::NotHandled;

    try
    {
        std::wstring wideApplication;
        std::wstring wideCommandLine;

        if (!AnsiToWide(applicationName, wideApplication) ||
            !AnsiToWide(commandLine, wideCommandLine))
        {
            return false;
        }

        auto launchW = [&](
            LPCWSTR application,
            LPWSTR command,
            LPPROCESS_INFORMATION info) -> BOOL
            {
                std::string ansiApplication;
                std::string ansiCommand;

                if (application && !WideToAnsi(application, ansiApplication))
                    return FALSE;

                if (command && *command && !WideToAnsi(command, ansiCommand))
                    return FALSE;

                return launchA(
                    application ? ansiApplication.c_str() : nullptr,
                    command ? ansiCommand.data() : nullptr,
                    info);
            };

        outcome = TryRedirectCreateW(
            applicationName ? wideApplication.c_str() : nullptr,
            commandLine ? wideCommandLine.data() : nullptr,
            creationFlags,
            startupFlags,
            processInformation,
            allowActivation,
            launchW);
    }
    catch (...)
    {
        return false;
    }

    if (outcome == RedirectResult::NotHandled)
        return false;

    result = outcome == RedirectResult::Succeeded ? TRUE : FALSE;
    return true;
}

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

    BOOL result = FALSE;

    if (RunCreateRedirectW(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCWSTR application, LPWSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessW(
                application, command,
                processAttributes, threadAttributes,
                inheritHandles, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    if (RunCreateRedirectA(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCSTR application, LPSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessA(
                application, command,
                processAttributes, threadAttributes,
                inheritHandles, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    // A launch under another user's token must run under that token, which
    // app activation can't do: only PATH redirection is allowed here.
    if (RunCreateRedirectW(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCWSTR application, LPWSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessAsUserW(
                token, application, command,
                processAttributes, threadAttributes,
                inheritHandles, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    if (RunCreateRedirectA(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCSTR application, LPSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessAsUserA(
                token, application, command,
                processAttributes, threadAttributes,
                inheritHandles, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    // CreateProcessW arrives here with a null token; anything else is a
    // launch under an explicit token and must stay a real CreateProcess.
    if (RunCreateRedirectW(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCWSTR application, LPWSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessInternalW(
                token, application, command,
                processAttributes, threadAttributes,
                inheritHandles, creationFlags,
                environment, currentDirectory,
                startupInfo, info, restrictedToken);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    if (RunCreateRedirectW(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCWSTR application, LPWSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessWithLogonW(
                userName, domain, password, logonFlags,
                application, command, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
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

    BOOL result = FALSE;

    if (RunCreateRedirectW(
        applicationName,
        commandLine,
        creationFlags,
        startupInfo ? startupInfo->dwFlags : 0,
        processInformation,
        true,
        result,
        [&](LPCWSTR application, LPWSTR command, LPPROCESS_INFORMATION info)
        {
            return g_originalCreateProcessWithTokenW(
                token, logonFlags,
                application, command, creationFlags,
                environment, currentDirectory,
                startupInfo, info);
        }))
    {
        return result;
    }

    return g_originalCreateProcessWithTokenW(
        token, logonFlags,
        applicationName, commandLine, creationFlags,
        environment, currentDirectory,
        startupInfo, processInformation);
}

/// <summary>
/// The shell's internal launcher: ShellExecute, RunFileDlg and Explorer end
/// up here before reaching CreateProcess*. Only PATH redirection applies at
/// this level; the request is modified in place (and always restored) so the
/// caller's structure is never copied or truncated.
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

    if (ShouldIntercept() &&
        info &&
        info->cbSize >= sizeof(ReboundShCreateProcessInfoW) &&
        info->pszFile &&
        !(info->dwCreationFlags &
            (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS)))
    {
        try
        {
            Target target;

            if (ResolveRedirect(info->pszFile, target) &&
                target.kind == TargetKind::Path)
            {
                struct FileRestore
                {
                    ReboundShCreateProcessInfoW* request;
                    PCWSTR file;

                    ~FileRestore()
                    {
                        request->pszFile = file;
                    }
                };

                BOOL ok;

                {
                    FileRestore restore{ info, info->pszFile };
                    RedirectGuard redirectGuard;

                    info->pszFile = target.value.c_str();
                    ok = g_originalSHCreateProcessAsUserW(info);
                }

                if (ok)
                    return TRUE;
            }
        }
        catch (...)
        {
        }
    }

    return g_originalSHCreateProcessAsUserW(info);
}

// ---------------------------------------------------------------------------
// ShellExecute-family redirection
// ---------------------------------------------------------------------------

enum class VerbClass
{
    Open,
    Elevate,
    Other
};

static VerbClass ClassifyVerb(LPCWSTR verb)
{
    if (!verb || !*verb || _wcsicmp(verb, L"open") == 0)
        return VerbClass::Open;

    if (_wcsicmp(verb, L"runas") == 0)
        return VerbClass::Elevate;

    // properties, edit, print, explore, find...: these act on the file the
    // caller named, not on the program that file would launch.
    return VerbClass::Other;
}

/// <summary>
/// Decides whether a ShellExecute request is a plain "launch this program"
/// request that an association may replace.
/// </summary>
static bool LookupShellTarget(LPCWSTR file, LPCWSTR verb, Target& target)
{
    if (!file || !*file)
        return false;

    const VerbClass verbClass = ClassifyVerb(verb);

    if (verbClass == VerbClass::Other)
        return false;

    if (!ResolveRedirect(file, target))
        return false;

    // Elevation can only be honoured by a real executable launch.
    if (verbClass == VerbClass::Elevate && target.kind != TargetKind::Path)
        return false;

    return true;
}

static RedirectResult TryRedirectShellExecuteExW(SHELLEXECUTEINFOW* info)
{
    if (!g_originalShellExecuteExW ||
        !info ||
        info->cbSize < sizeof(SHELLEXECUTEINFOW) ||
        !info->lpFile)
    {
        return RedirectResult::NotHandled;
    }

    // These make lpFile irrelevant or change how it is interpreted.
    constexpr ULONG unsupportedMask =
        SEE_MASK_CLASSNAME |
        SEE_MASK_CLASSKEY |
        SEE_MASK_IDLIST |
        SEE_MASK_INVOKEIDLIST;

    if (info->fMask & unsupportedMask)
        return RedirectResult::NotHandled;

    Target target;

    if (!LookupShellTarget(info->lpFile, info->lpVerb, target))
        return RedirectResult::NotHandled;

    const bool hInstIsSite =
        (info->fMask & SEE_MASK_FLAG_HINST_IS_SITE) != 0;

    switch (target.kind)
    {
    case TargetKind::Path:
    case TargetKind::Uri:
    {
        SHELLEXECUTEINFOW redirected = *info;
        redirected.lpFile = target.value.c_str();

        // URI targets never receive the caller's arguments.
        if (target.kind == TargetKind::Uri)
            redirected.lpParameters = nullptr;

        BOOL ok;

        {
            RedirectGuard redirectGuard;
            ok = g_originalShellExecuteExW(&redirected);
        }

        if (!ok)
            return RedirectResult::NotHandled;

        if (!hInstIsSite)
            info->hInstApp = redirected.hInstApp;

        info->hProcess = redirected.hProcess;
        return RedirectResult::Succeeded;
    }

    case TargetKind::Aumid:
    {
        DWORD processId = 0;
        HRESULT hr;

        {
            RedirectGuard redirectGuard;

            hr = ActivateApp(
                target.value.c_str(),
                info->lpParameters,
                &processId);
        }

        if (FAILED(hr))
            return RedirectResult::NotHandled;

        HANDLE process = nullptr;

        if (info->fMask & SEE_MASK_NOCLOSEPROCESS)
        {
            process = OpenActivatedProcess(processId);

            if (!process)
            {
                SetLastError(ERROR_ACCESS_DENIED);

                if (!hInstIsSite)
                {
                    info->hInstApp = reinterpret_cast<HINSTANCE>(
                        static_cast<INT_PTR>(SE_ERR_ACCESSDENIED));
                }

                return RedirectResult::Failed;
            }
        }

        if (!hInstIsSite)
        {
            info->hInstApp = reinterpret_cast<HINSTANCE>(
                static_cast<INT_PTR>(33));
        }

        info->hProcess = process;
        return RedirectResult::Succeeded;
    }
    }

    return RedirectResult::NotHandled;
}

static RedirectResult TryRedirectShellExecuteExA(SHELLEXECUTEINFOA* info)
{
    if (!g_originalShellExecuteExW ||
        !info ||
        info->cbSize < sizeof(SHELLEXECUTEINFOA) ||
        !info->lpFile)
    {
        return RedirectResult::NotHandled;
    }

    std::wstring file;
    std::wstring verb;
    std::wstring parameters;
    std::wstring directory;

    if (!AnsiToWide(info->lpFile, file) ||
        !AnsiToWide(info->lpVerb, verb) ||
        !AnsiToWide(info->lpParameters, parameters) ||
        !AnsiToWide(info->lpDirectory, directory))
    {
        return RedirectResult::NotHandled;
    }

    // Class name / class key / ID list requests are rejected by the wide
    // logic; lpClass is not carried over because CLASSNAME is unsupported.
    SHELLEXECUTEINFOW wide{};
    wide.cbSize = sizeof(wide);
    wide.fMask = info->fMask;
    wide.hwnd = info->hwnd;
    wide.lpVerb = info->lpVerb ? verb.c_str() : nullptr;
    wide.lpFile = file.c_str();
    wide.lpParameters = info->lpParameters ? parameters.c_str() : nullptr;
    wide.lpDirectory = info->lpDirectory ? directory.c_str() : nullptr;
    wide.nShow = info->nShow;
    wide.hInstApp = info->hInstApp;
    wide.lpIDList = info->lpIDList;
    wide.hkeyClass = info->hkeyClass;
    wide.dwHotKey = info->dwHotKey;
    wide.hIcon = info->hIcon;

    const RedirectResult result = TryRedirectShellExecuteExW(&wide);

    if (result != RedirectResult::NotHandled)
    {
        if (!(info->fMask & SEE_MASK_FLAG_HINST_IS_SITE))
            info->hInstApp = wide.hInstApp;

        info->hProcess = wide.hProcess;
    }

    return result;
}

static BOOL WINAPI ShellExecuteExW_Hook(
    SHELLEXECUTEINFOW* info)
{
    HookCallGuard guard;

    if (!g_originalShellExecuteExW)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (ShouldIntercept())
    {
        RedirectResult result = RedirectResult::NotHandled;

        try
        {
            result = TryRedirectShellExecuteExW(info);
        }
        catch (...)
        {
            result = RedirectResult::NotHandled;
        }

        if (result == RedirectResult::Succeeded)
            return TRUE;

        if (result == RedirectResult::Failed)
            return FALSE;
    }

    return g_originalShellExecuteExW(info);
}

static BOOL WINAPI ShellExecuteExA_Hook(
    SHELLEXECUTEINFOA* info)
{
    HookCallGuard guard;

    if (!g_originalShellExecuteExA)
    {
        SetLastError(ERROR_PROC_NOT_FOUND);
        return FALSE;
    }

    if (ShouldIntercept())
    {
        RedirectResult result = RedirectResult::NotHandled;

        try
        {
            result = TryRedirectShellExecuteExA(info);
        }
        catch (...)
        {
            result = RedirectResult::NotHandled;
        }

        if (result == RedirectResult::Succeeded)
            return TRUE;

        if (result == RedirectResult::Failed)
            return FALSE;
    }

    return g_originalShellExecuteExA(info);
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

    if (!g_originalShellExecuteW)
        return reinterpret_cast<HINSTANCE>(
            static_cast<INT_PTR>(SE_ERR_ACCESSDENIED));

    if (ShouldIntercept() && file && *file)
    {
        try
        {
            Target target;

            if (LookupShellTarget(file, operation, target))
            {
                RedirectGuard redirectGuard;

                if (target.kind == TargetKind::Aumid)
                {
                    if (SUCCEEDED(ActivateApp(
                        target.value.c_str(), parameters, nullptr)))
                    {
                        return reinterpret_cast<HINSTANCE>(
                            static_cast<INT_PTR>(33));
                    }
                }
                else
                {
                    const HINSTANCE result = g_originalShellExecuteW(
                        hwnd, operation, target.value.c_str(),
                        target.kind == TargetKind::Uri ? nullptr : parameters,
                        directory, showCommand);

                    if (reinterpret_cast<INT_PTR>(result) > 32)
                        return result;
                }
            }
        }
        catch (...)
        {
        }
    }

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

    if (!g_originalShellExecuteA)
        return reinterpret_cast<HINSTANCE>(
            static_cast<INT_PTR>(SE_ERR_ACCESSDENIED));

    if (ShouldIntercept() && file && *file)
    {
        try
        {
            std::wstring wideFile;
            std::wstring wideVerb;
            std::wstring wideParameters;
            Target target;

            if (AnsiToWide(file, wideFile) &&
                AnsiToWide(operation, wideVerb) &&
                AnsiToWide(parameters, wideParameters) &&
                LookupShellTarget(
                    wideFile.c_str(),
                    operation ? wideVerb.c_str() : nullptr,
                    target))
            {
                RedirectGuard redirectGuard;

                if (target.kind == TargetKind::Aumid)
                {
                    if (SUCCEEDED(ActivateApp(
                        target.value.c_str(),
                        parameters ? wideParameters.c_str() : nullptr,
                        nullptr)))
                    {
                        return reinterpret_cast<HINSTANCE>(
                            static_cast<INT_PTR>(33));
                    }
                }
                else
                {
                    std::string ansiTarget;

                    if (WideToAnsi(target.value, ansiTarget))
                    {
                        const HINSTANCE result = g_originalShellExecuteA(
                            hwnd, operation, ansiTarget.c_str(),
                            target.kind == TargetKind::Uri ? nullptr : parameters,
                            directory, showCommand);

                        if (reinterpret_cast<INT_PTR>(result) > 32)
                            return result;
                    }
                }
            }
        }
        catch (...)
        {
        }
    }

    return g_originalShellExecuteA(
        hwnd, operation, file, parameters, directory, showCommand);
}

// ---------------------------------------------------------------------------
// Lifecycle
// ---------------------------------------------------------------------------

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

enum class ProcessPresence
{
    Present,
    Absent,
    Unknown
};

/// <summary>
/// Finds a process by name. A failed snapshot is reported as Unknown so a
/// transient error is never mistaken for "the process is gone".
/// </summary>
static ProcessPresence FindProcessByName(const wchar_t* processName) {
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    if (snapshot == INVALID_HANDLE_VALUE)
        return ProcessPresence::Unknown;

    PROCESSENTRY32W entry{};
    entry.dwSize = sizeof(PROCESSENTRY32W);

    if (!Process32FirstW(snapshot, &entry)) {
        CloseHandle(snapshot);
        return ProcessPresence::Unknown;
    }

    ProcessPresence presence = ProcessPresence::Absent;

    do {
        if (_wcsicmp(processName, entry.szExeFile) == 0) {
            presence = ProcessPresence::Present;
            break;
        }
    } while (Process32NextW(snapshot, &entry));

    CloseHandle(snapshot);
    return presence;
}

/// <summary>
/// Continuously check if Rebound Service Host is alive.
/// </summary>
static DWORD WINAPI MonitorInjectorThread(LPVOID)
{
    constexpr int checkIntervalMs = 1000;
    constexpr int missingThreshold = 3;

    int missingCount = 0;

    while (WaitForSingleObject(g_shutdownEvent, checkIntervalMs)
        == WAIT_TIMEOUT)
    {
        const ProcessPresence presence =
            FindProcessByName(REBOUND_SERVICE_HOST_EXE);

        if (presence == ProcessPresence::Present)
        {
            missingCount = 0;
        }
        else if (presence == ProcessPresence::Absent &&
            ++missingCount >= missingThreshold)
        {
            RequestShutdown();
            break;
        }
    }

    return 0;
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

static LPVOID TargetOf(const HookEntry& hook)
{
    return reinterpret_cast<LPVOID>(hook.target);
}

// Required hooks keep the previous behaviour (any failure aborts and shuts
// the DLL down cleanly). Optional ones are skipped if the export is missing
// or cannot be hooked on this Windows build. WinExec is not hooked on its
// own: it ends in CreateProcess, which is.
static HookEntry g_hooks[] =
{
    { L"shell32.dll",   "ShellExecuteExW", reinterpret_cast<void*>(&ShellExecuteExW_Hook), reinterpret_cast<void**>(&g_originalShellExecuteExW), true, nullptr, false },
    { L"shell32.dll",   "ShellExecuteExA", reinterpret_cast<void*>(&ShellExecuteExA_Hook), reinterpret_cast<void**>(&g_originalShellExecuteExA), false, nullptr, false },
    { L"shell32.dll",   "ShellExecuteW", reinterpret_cast<void*>(&ShellExecuteW_Hook), reinterpret_cast<void**>(&g_originalShellExecuteW), false, nullptr, false },
    { L"shell32.dll",   "ShellExecuteA", reinterpret_cast<void*>(&ShellExecuteA_Hook), reinterpret_cast<void**>(&g_originalShellExecuteA), false, nullptr, false },

    { L"KernelBase.dll", "CreateProcessW", reinterpret_cast<void*>(&CreateProcessW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessW), true, nullptr, false },
    { L"KernelBase.dll", "CreateProcessA", reinterpret_cast<void*>(&CreateProcessA_Hook), reinterpret_cast<void**>(&g_originalCreateProcessA), true, nullptr, false },
    { L"KernelBase.dll", "CreateProcessAsUserW", reinterpret_cast<void*>(&CreateProcessAsUserW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessAsUserW), false, nullptr, false },
    { L"KernelBase.dll", "CreateProcessAsUserA", reinterpret_cast<void*>(&CreateProcessAsUserA_Hook), reinterpret_cast<void**>(&g_originalCreateProcessAsUserA), false, nullptr, false },
    { L"KernelBase.dll", "CreateProcessInternalW", reinterpret_cast<void*>(&CreateProcessInternalW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessInternalW), false, nullptr, false },
    { L"shell32.dll",    "SHCreateProcessAsUserW", reinterpret_cast<void*>(&SHCreateProcessAsUserW_Hook), reinterpret_cast<void**>(&g_originalSHCreateProcessAsUserW), false, nullptr, false },

    { L"advapi32.dll",  "CreateProcessWithLogonW", reinterpret_cast<void*>(&CreateProcessWithLogonW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessWithLogonW), false, nullptr, false },
    { L"advapi32.dll",  "CreateProcessWithTokenW", reinterpret_cast<void*>(&CreateProcessWithTokenW_Hook), reinterpret_cast<void**>(&g_originalCreateProcessWithTokenW), false, nullptr, false },
};

static bool InstallLauncherHooks()
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
        // owns. Hook each address once.
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

        if (MH_CreateHook(TargetOf(hook), hook.detour, hook.original) != MH_OK)
        {
            hook.target = nullptr;

            if (hook.required)
                return false;
            continue;
        }

        hook.created = true;
    }

    // Enable everything in one batch: threads are frozen once, and either
    // all hooks go live or none does.
    for (const HookEntry& hook : g_hooks)
    {
        if (hook.created &&
            MH_QueueEnableHook(TargetOf(hook)) != MH_OK)
        {
            return false;
        }
    }

    return MH_ApplyQueued() == MH_OK;
}

/// <summary>
/// Initialize API hooking in a separate thread.
/// </summary>
static DWORD WINAPI DeferredInitThread(LPVOID)
{
    if (g_state.load(std::memory_order_acquire) != LifecycleState::Starting)
        return 0;

    const MH_STATUS status = MH_Initialize();

    if (status == MH_OK)
    {
        g_ownsMinHookInitialization = true;
    }
    else if (status != MH_ERROR_ALREADY_INITIALIZED)
    {
        RequestShutdown();
        return 1;
    }

    // Shutdown may have been requested while initialization was running.
    if (g_state.load(std::memory_order_acquire) != LifecycleState::Starting)
        return 0;

    if (!InstallLauncherHooks())
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
    while (WaitForSingleObject(g_shutdownEvent, 0) == WAIT_TIMEOUT)
    {
        HANDLE pipe = CreateFileW(
            REBOUND_DLL_INJECTION_PIPE,
            GENERIC_READ,
            0,
            nullptr,
            OPEN_EXISTING,
            FILE_FLAG_OVERLAPPED,
            nullptr);

        if (pipe == INVALID_HANDLE_VALUE)
        {
            if (GetLastError() == ERROR_PIPE_BUSY)
                WaitNamedPipeW(REBOUND_DLL_INJECTION_PIPE, 250);
            else
                WaitForSingleObject(g_shutdownEvent, 250);

            continue;
        }

        HANDLE readEvent = CreateEventW(nullptr, TRUE, FALSE, nullptr);

        if (!readEvent)
        {
            CloseHandle(pipe);
            WaitForSingleObject(g_shutdownEvent, 250);
            continue;
        }

        char buffer[512];

        while (WaitForSingleObject(g_shutdownEvent, 0) == WAIT_TIMEOUT)
        {
            DWORD bytesRead = 0;

            OVERLAPPED ov{};
            ov.hEvent = readEvent;

            ResetEvent(readEvent);

            const BOOL immediate = ReadFile(
                pipe, buffer, sizeof(buffer) - 1, nullptr, &ov);

            if (!immediate)
            {
                if (GetLastError() != ERROR_IO_PENDING)
                    break;

                HANDLE waits[] = { g_shutdownEvent, readEvent };

                const DWORD result = WaitForMultipleObjects(
                    2, waits, FALSE, INFINITE);

                if (result != WAIT_OBJECT_0 + 1)
                {
                    // Shutdown (or a wait failure) with the read still
                    // pending: cancel it and wait until the kernel is done
                    // with the buffer and OVERLAPPED before leaving scope.
                    CancelIoEx(pipe, &ov);
                    GetOverlappedResult(pipe, &ov, &bytesRead, TRUE);
                    break;
                }
            }

            if (!GetOverlappedResult(pipe, &ov, &bytesRead, FALSE) ||
                bytesRead == 0)
            {
                break;
            }

            buffer[bytesRead] = '\0';
            std::string msg(buffer);

            const size_t lastNonWs =
                msg.find_last_not_of("\r\n \t");

            if (lastNonWs == std::string::npos)
                msg.clear();
            else
                msg.erase(lastNonWs + 1);

            if (msg.find("UNINJECT") != std::string::npos &&
                msg.find("Rebound.Forge.Hooks.Launcher.dll") != std::string::npos)
            {
                RequestShutdown();
                break;
            }
        }

        CloseHandle(readEvent);
        CloseHandle(pipe);
    }

    return 0;
}

static void WaitAndCloseThread(HANDLE& thread)
{
    if (!thread)
        return;

    WaitForSingleObject(thread, INFINITE);
    CloseHandle(thread);
    thread = nullptr;
}

/// <summary>
/// True if any other thread's instruction pointer is inside this DLL's image.
/// Used after the hooks are disabled to catch a thread that has just entered
/// a detour (before it counted itself) or is executing a detour's epilogue
/// (after it stopped counting itself). Nothing allocates while a thread is
/// suspended, so a suspended thread can never be holding a lock we need.
/// </summary>
static bool IsAnyThreadExecutingInModule()
{
    const ULONG_PTR base = reinterpret_cast<ULONG_PTR>(g_moduleHandle);

    const IMAGE_DOS_HEADER* dos =
        reinterpret_cast<const IMAGE_DOS_HEADER*>(g_moduleHandle);

    const IMAGE_NT_HEADERS* nt =
        reinterpret_cast<const IMAGE_NT_HEADERS*>(base + dos->e_lfanew);

    const ULONG_PTR end = base + nt->OptionalHeader.SizeOfImage;

    const DWORD processId = GetCurrentProcessId();
    const DWORD currentThreadId = GetCurrentThreadId();

    std::vector<DWORD> threadIds;

    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);

    if (snapshot == INVALID_HANDLE_VALUE)
        return false;

    THREADENTRY32 entry{};
    entry.dwSize = sizeof(entry);

    if (Thread32First(snapshot, &entry))
    {
        do
        {
            if (entry.th32OwnerProcessID == processId &&
                entry.th32ThreadID != currentThreadId)
            {
                threadIds.push_back(entry.th32ThreadID);
            }
        } while (Thread32Next(snapshot, &entry));
    }

    CloseHandle(snapshot);

    for (const DWORD threadId : threadIds)
    {
        HANDLE thread = OpenThread(
            THREAD_SUSPEND_RESUME | THREAD_GET_CONTEXT, FALSE, threadId);

        if (!thread)
            continue;

        bool inside = false;

        CONTEXT context;
        ZeroMemory(&context, sizeof(context));
        context.ContextFlags = CONTEXT_CONTROL;

        if (SuspendThread(thread) != static_cast<DWORD>(-1))
        {
            if (GetThreadContext(thread, &context))
            {
#if defined(_M_X64)
                const ULONG_PTR ip = static_cast<ULONG_PTR>(context.Rip);
#elif defined(_M_IX86)
                const ULONG_PTR ip = static_cast<ULONG_PTR>(context.Eip);
#elif defined(_M_ARM64)
                const ULONG_PTR ip = static_cast<ULONG_PTR>(context.Pc);
#else
#error Unsupported architecture
#endif
                inside = ip >= base && ip < end;
            }

            ResumeThread(thread);
        }

        CloseHandle(thread);

        if (inside)
            return true;
    }

    return false;
}

/// <summary>
/// Blocks until no detour is in flight and no thread is executing this DLL,
/// and that has held for several consecutive checks. Never gives up: a thread
/// that is still inside our code means the DLL must not be unmapped.
/// </summary>
static void WaitForQuiescence()
{
    int stableRounds = 0;

    while (stableRounds < 3)
    {
        if (g_activeCallbacks.load(std::memory_order_acquire) == 0 &&
            !IsAnyThreadExecutingInModule())
        {
            ++stableRounds;
        }
        else
        {
            stableRounds = 0;
        }

        Sleep(10);
    }
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

    // From here on every detour only forwards to the original function.
    // Stop and join every worker before touching the hooks.
    WaitAndCloseThread(g_initThread);
    WaitAndCloseThread(g_monitorThread);
    WaitAndCloseThread(g_pipeThread);

    if (g_hooksInstalled.load(std::memory_order_acquire))
    {
        // 1. Unpatch every hook in one batch (threads are frozen once and
        //    any thread caught inside a trampoline is moved back).
        for (const HookEntry& hook : g_hooks)
        {
            if (hook.created)
                MH_QueueDisableHook(TargetOf(hook));
        }

        if (MH_ApplyQueued() != MH_OK)
        {
            // Hooks are still patched. Detours stay valid and forward
            // calls, so keep the DLL mapped rather than risk a crash.
            OutputDebugStringW(
                L"Launcher hook: failed to disable; retaining DLL.\n");
            return 1;
        }

        // 2. Wait until nobody can still be in, or on the way into or out
        //    of, a detour or a trampoline.
        WaitForQuiescence();

        // 3. Only now free the trampolines.
        for (HookEntry& hook : g_hooks)
        {
            if (!hook.created)
                continue;

            if (MH_RemoveHook(TargetOf(hook)) != MH_OK)
            {
                OutputDebugStringW(
                    L"Launcher hook: failed to remove; retaining DLL.\n");
                return 1;
            }

            hook.created = false;
        }

        // 4. Release MinHook's heap and trampoline memory.
        if (g_ownsMinHookInitialization)
        {
            if (MH_Uninitialize() != MH_OK)
            {
                OutputDebugStringW(
                    L"Launcher hook: failed to uninitialize; retaining DLL.\n");
                return 1;
            }

            g_ownsMinHookInitialization = false;
        }

        g_hooksInstalled.store(false, std::memory_order_release);

        for (HookEntry& hook : g_hooks)
        {
            if (hook.original)
                *hook.original = nullptr;

            hook.target = nullptr;
        }
    }

    // Release the references InstallLauncherHooks took on the hooked modules.
    for (HMODULE module : g_loadedModules)
        FreeLibrary(module);

    g_loadedModules.clear();

    g_state.store(LifecycleState::Stopped, std::memory_order_release);

    CloseHandle(g_shutdownEvent);
    g_shutdownEvent = nullptr;

    if (g_shutdownThread)
    {
        CloseHandle(g_shutdownThread);
        g_shutdownThread = nullptr;
    }

    // Release the single-instance mutex last, right before the module goes.
    if (g_initMutex)
    {
        CloseHandle(g_initMutex);
        g_initMutex = nullptr;
    }

    // Drops the reference the injector took and ends this thread without
    // ever returning into code that is about to be unmapped.
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
        L"Rebound.Launcher_LaunchHook_%lu",
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

    if (!g_shutdownEvent)
    {
        CloseHandle(g_initMutex);
        g_initMutex = nullptr;
        return FALSE;
    }

    g_shutdownThread = CreateThread(
        nullptr, 0, ShutdownCoordinatorThread, nullptr, 0, nullptr);

    if (!g_shutdownThread)
    {
        CloseHandle(g_shutdownEvent);
        CloseHandle(g_initMutex);

        g_shutdownEvent = nullptr;
        g_initMutex = nullptr;
        return FALSE;
    }

    return TRUE;
}