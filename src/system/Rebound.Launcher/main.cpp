#include <windows.h>
#include "resource.h"
#include <shellscalingapi.h>
#include <shobjidl.h>
#include <wtsapi32.h>
#include <userenv.h>
#include <string>
#include <vector>
#include <optional>
#include <algorithm>
#include <iostream>
#include <codecvt>
#include <locale>
#include <filesystem>

// Manifest dependency ensures modern visual styles (Windows Common Controls v6)
#pragma comment(linker,"\"/manifestdependency:type='win32' \
name='Microsoft.Windows.Common-Controls' version='6.0.0.0' \
processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#pragma comment(lib, "Shcore.lib")
#pragma comment(lib, "Ole32.lib")
#pragma comment(lib, "Wtsapi32.lib")
#pragma comment(lib, "Advapi32.lib")
#pragma comment(lib, "Userenv.lib")
#pragma comment(lib, "Comctl32.lib")

const wchar_t CLASS_NAME[] = L"ReboundLauncher";

// UI control identifiers for window message routing
enum ControlIDs {
    IDC_BTN_OPEN = 1001,
    IDC_BTN_CLOSE = 1002,
    IDC_LBL_HEADER = 1003,
    IDC_LBL_SUBTEXT = 1004
};

// Named pipe used for inter-process communication with Rebound service host
constexpr char PIPE_NAME[] = R"(\\.\pipe\REBOUND_SERVICE_HOST)";
constexpr char IFEO_PAUSE_PREFIX[] = "IFEOEngine::Pause#";
constexpr char IFEO_RESUME_PREFIX[] = "IFEOEngine::Resume#";

// Package Family Name for the Rebound ServiceHost UWP application
const wchar_t* PLACEHOLDER_PFN = L"Rebound.ServiceHost_rcz2tbwv5qzb8";

// Converts logical pixel values to physical pixels based on window's DPI scaling
int Scale(HWND hwnd, int value) {
    UINT dpi = GetDpiForWindow(hwnd);
    return MulDiv(value, dpi, 96);
}

LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Character encoding conversion utilities for cross-platform string handling
std::string WideToUtf8(const std::wstring& w) {
    if (w.empty()) return {};
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), NULL, 0, NULL, NULL);
    std::string s(size_needed, 0);
    WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), &s[0], size_needed, NULL, NULL);
    return s;
}

std::wstring Utf8ToWide(const std::string& s) {
    if (s.empty()) return {};
    int n = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), NULL, 0);
    std::wstring out(n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), &out[0], n);
    return out;
}

// Determines whether the current process is running with administrator privileges.
// This affects how we handle IFEO modifications and process launching.
bool IsProcessElevated() {
    BOOL isElev = FALSE;
    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
        TOKEN_ELEVATION te;
        DWORD ret;
        if (GetTokenInformation(hToken, TokenElevation, &te, sizeof(te), &ret)) {
            isElev = te.TokenIsElevated;
        }
        CloseHandle(hToken);
    }
    return isElev == TRUE;
}

// Launches a standard Win32 executable with the current user's privileges.
// Used for non-elevated process creation when we don't need to bypass IFEO.
bool LaunchProcessSimple(const std::wstring& exePath, const std::wstring& args, PROCESS_INFORMATION& outProc) {
    // Command line must be quoted to handle paths with spaces
    std::wstring cmdLine = L"\"" + exePath + L"\"";
    if (!args.empty()) {
        cmdLine += L" " + args;
    }

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    ZeroMemory(&outProc, sizeof(outProc));

    // CreateProcessW requires writable buffer; we can't pass string literals
    std::vector<wchar_t> buf(cmdLine.begin(), cmdLine.end());
    buf.push_back(0);

    BOOL ok = CreateProcessW(
        exePath.c_str(),
        buf.data(),
        NULL, NULL, FALSE,
        0, NULL, NULL,
        &si, &outProc);

    return ok == TRUE;
}

// Launches a process with the security token of the currently logged-in interactive user.
// This is critical when running elevated: we need to drop privileges to avoid launching
// the target application with unintended admin rights, which could cause security issues
// or unexpected behavior in applications that check their privilege level.
bool LaunchProcessAsInteractiveUser(const std::wstring& exePath, const std::wstring& args, PROCESS_INFORMATION& outProc) {
    // Retrieve the active console session to get the correct user's token
    DWORD sessionId = WTSGetActiveConsoleSessionId();

    HANDLE hUserToken = NULL;
    if (!WTSQueryUserToken(sessionId, &hUserToken)) {
        return false;
    }

    // Duplicate token with appropriate access rights for process creation
    HANDLE hUserTokenDup = NULL;
    if (!DuplicateTokenEx(hUserToken, MAXIMUM_ALLOWED, NULL, SecurityIdentification, TokenPrimary, &hUserTokenDup)) {
        CloseHandle(hUserToken);
        return false;
    }

    // Build environment block for the user to ensure proper environment variables
    LPVOID env = NULL;
    if (!CreateEnvironmentBlock(&env, hUserTokenDup, FALSE)) {
        env = NULL;
    }

    // Prepare command line with proper quoting
    std::wstring cmdLine = L"\"" + exePath + L"\"";
    if (!args.empty()) cmdLine += L" " + args;
    std::vector<wchar_t> cmdBuf(cmdLine.begin(), cmdLine.end()); cmdBuf.push_back(0);

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    si.lpDesktop = const_cast<wchar_t*>(L"winsta0\\default");

    BOOL ok = CreateProcessAsUserW(hUserTokenDup, nullptr, cmdBuf.data(),
        NULL, NULL, FALSE, CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_CONSOLE,
        env, NULL, &si, &outProc);

    if (env) DestroyEnvironmentBlock(env);
    CloseHandle(hUserTokenDup);
    CloseHandle(hUserToken);
    return ok == TRUE;
}

// Re-launches this launcher executable with administrator privileges via UAC prompt.
// Required when we need to temporarily modify IFEO registry keys (which requires admin).
bool RelaunchAsAdminWithArgs(const std::wstring& args) {
    wchar_t exePath[MAX_PATH];
    if (!GetModuleFileNameW(NULL, exePath, MAX_PATH)) return false;

    SHELLEXECUTEINFOW sei{};
    sei.cbSize = sizeof(sei);
    sei.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_DDEWAIT;
    sei.lpVerb = L"runas";
    sei.lpFile = exePath;
    sei.lpParameters = args.c_str();
    sei.nShow = SW_SHOWNORMAL;
    return ShellExecuteExW(&sei) == TRUE;
}

// Sends a command to the Rebound service host via named pipe with optional reply.
// Used for coordinating with the background service for advanced IFEO management.
bool SendPipeMessage(const std::string& msg, std::string* outReply = nullptr, DWORD timeoutMs = 1500) {
    if (!WaitNamedPipeA(PIPE_NAME, timeoutMs)) {
        return false;
    }
    HANDLE hPipe = CreateFileA(PIPE_NAME, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (hPipe == INVALID_HANDLE_VALUE) {
        return false;
    }
    DWORD bytesWritten = 0;
    BOOL ok = WriteFile(hPipe, msg.c_str(), (DWORD)msg.size(), &bytesWritten, NULL);
    if (!ok) { CloseHandle(hPipe); return false; }

    if (outReply) {
        char buffer[512];
        DWORD bytesRead = 0;
        DWORD start = GetTickCount();
        while (GetTickCount() - start < timeoutMs) {
            BOOL r = ReadFile(hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL);
            if (r && bytesRead > 0) {
                buffer[bytesRead] = '\0';
                *outReply = std::string(buffer, bytesRead);
                break;
            }
            Sleep(10);
        }
    }

    CloseHandle(hPipe);
    return true;
}

// Activates a UWP (Universal Windows Platform) application using its Application User Model ID.
// This is the proper way to launch modern Windows Store apps programmatically.
bool LaunchUWPByAumid(const std::wstring& appUserModelId, const std::wstring& arguments) {
    HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
    bool coInit = SUCCEEDED(hr) || hr == RPC_E_CHANGED_MODE;
    if (FAILED(hr) && hr != RPC_E_CHANGED_MODE) {
        return false;
    }

    IApplicationActivationManager* pAppActMgr = nullptr;
    hr = CoCreateInstance(CLSID_ApplicationActivationManager, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pAppActMgr));
    if (FAILED(hr) || !pAppActMgr) {
        if (coInit) CoUninitialize();
        return false;
    }

    DWORD pid = 0;
    hr = pAppActMgr->ActivateApplication(appUserModelId.c_str(), arguments.empty() ? nullptr : arguments.c_str(), AO_NONE, &pid);

    pAppActMgr->Release();
    if (coInit) CoUninitialize();

    return SUCCEEDED(hr);
}

// Temporarily disables an IFEO (Image File Execution Options) entry by renaming its registry key.
// This allows a single execution of the target executable without triggering the debugger redirect.
// The key is renamed to "INVALID<exename>" rather than deleted to preserve all settings for restoration.
bool PauseIFEOEntry(const std::wstring& executableName) {
    std::wstring basePath = L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options";
    std::wstring originalKey = basePath + L"\\" + executableName;
    std::wstring newKey = basePath + L"\\INVALID" + executableName;

    HKEY hOriginal = nullptr;
    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, originalKey.c_str(), 0, KEY_READ | KEY_WRITE, &hOriginal) == ERROR_SUCCESS) {
        HKEY hNew = nullptr;
        if (RegCreateKeyExW(HKEY_LOCAL_MACHINE, newKey.c_str(), 0, nullptr, 0, KEY_WRITE, nullptr, &hNew, nullptr) == ERROR_SUCCESS) {
            // Copy all registry values from original key to new location
            DWORD index = 0;
            WCHAR valueName[256];
            BYTE valueData[1024];
            DWORD valueNameSize, valueDataSize, type;

            while (true) {
                valueNameSize = sizeof(valueName) / sizeof(WCHAR);
                valueDataSize = sizeof(valueData);

                LONG result = RegEnumValueW(hOriginal, index, valueName, &valueNameSize, nullptr, &type, valueData, &valueDataSize);
                if (result == ERROR_NO_MORE_ITEMS) break;
                if (result != ERROR_SUCCESS) { ++index; continue; }

                RegSetValueExW(hNew, valueName, 0, type, valueData, valueDataSize);
                ++index;
            }
            RegCloseKey(hNew);
        }

        RegCloseKey(hOriginal);
        RegDeleteTreeW(HKEY_LOCAL_MACHINE, originalKey.c_str());
        return true;
    }
    return false;
}

// Re-enables an IFEO entry by moving the registry key back from "INVALID<exename>" to its original location.
// This restores the debugger redirect after the target executable has been launched.
bool ResumeIFEOEntry(const std::wstring& executableName) {
    std::wstring basePath = L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options";
    std::wstring originalKey = basePath + L"\\" + executableName;
    std::wstring invalidKey = basePath + L"\\INVALID" + executableName;

    HKEY hInvalid = nullptr;
    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, invalidKey.c_str(), 0, KEY_READ | KEY_WRITE, &hInvalid) == ERROR_SUCCESS) {
        HKEY hOriginal = nullptr;
        if (RegCreateKeyExW(HKEY_LOCAL_MACHINE, originalKey.c_str(), 0, nullptr, 0, KEY_WRITE, nullptr, &hOriginal, nullptr) == ERROR_SUCCESS) {
            // Restore all registry values from temporary location back to original key
            DWORD index = 0;
            WCHAR valueName[256];
            BYTE valueData[1024];
            DWORD valueNameSize, valueDataSize, type;

            while (true) {
                valueNameSize = sizeof(valueName) / sizeof(WCHAR);
                valueDataSize = sizeof(valueData);

                LONG result = RegEnumValueW(hInvalid, index, valueName, &valueNameSize, nullptr, &type, valueData, &valueDataSize);
                if (result == ERROR_NO_MORE_ITEMS) break;
                if (result != ERROR_SUCCESS) { ++index; continue; }

                RegSetValueExW(hOriginal, valueName, 0, type, valueData, valueDataSize);
                ++index;
            }
            RegCloseKey(hOriginal);
        }

        RegCloseKey(hInvalid);
        RegDeleteTreeW(HKEY_LOCAL_MACHINE, invalidKey.c_str());
        return true;
    }
    return false;
}

// Defines a mapping between a legacy Win32 executable and its modern replacement.
// LaunchTarget can be either a UWP Application User Model ID or a path to an alternative executable.
// Empty LaunchTarget means launch the original executable directly (useful for testing).
struct LaunchEntry {
    std::wstring ExecutableName;
    std::optional<std::wstring> LaunchTarget;
};

// Registry of known executable replacements. When IFEO redirects to this launcher,
// we check this table to determine which modern app should be launched instead.
// Example: charmap.exe (legacy Character Map) -> modern UWP Character Map app
std::vector<LaunchEntry> KnownLaunchEntries = {
    { L"charmap.exe", std::optional<std::wstring>(L"58027.265370AB8DB33_fjemmk5ta3a5g!App") },
    { L"winver.exe", std::optional<std::wstring>(L"Rebound.About_rcz2tbwv5qzb8!App") },
};

// Searches the launch mapping table for a given executable name (case-insensitive).
// Returns the mapping entry if found, allowing us to redirect to the appropriate modern app.
std::optional<LaunchEntry> FindLaunchEntry(const std::wstring& exeName) {
    std::wstring b = exeName;
    std::transform(b.begin(), b.end(), b.begin(), towlower);
    for (const auto& e : KnownLaunchEntries) {
        std::wstring a = e.ExecutableName;
        std::transform(a.begin(), a.end(), a.begin(), towlower);
        if (a == b) return e;
    }
    return std::nullopt;
}

// Launches the appropriate application based on the mapping entry.
// Handles three scenarios:
// 1. Target is a .exe file path -> launch as Win32 app
// 2. Target is a UWP AUMID -> launch as UWP app
// 3. No target specified -> launch original executable
bool LaunchMappedEntry(const LaunchEntry& entry, const std::wstring& args) {
    if (entry.LaunchTarget) {
        const std::wstring& target = *entry.LaunchTarget;
        // Check if target is a file path to a Win32 executable
        if (target.size() >= 4 && target.substr(target.size() - 4) == L".exe" &&
            std::filesystem::exists(target)) {
            PROCESS_INFORMATION pi{};
            return LaunchProcessSimple(target, args, pi);
        }
        else {
            // Assume target is a UWP Application User Model ID
            return LaunchUWPByAumid(target, args);
        }
    }
    else {
        // No replacement defined; launch the original executable
        PROCESS_INFORMATION pi{};
        return LaunchProcessSimple(entry.ExecutableName, args, pi);
    }
}

// Extracts the filename component from a full path.
// Used to normalize executable names for lookup in the mapping table.
std::wstring FilenameFromPath(const std::wstring& path) {
    size_t pos = path.find_last_of(L"\\/");
    if (pos == std::wstring::npos) return path;
    return path.substr(pos + 1);
}

// Intelligently parses a command line string to separate the executable path from its arguments.
// Handles three common scenarios:
// 1. Quoted paths: "C:\Program Files\app.exe" arg1 arg2
// 2. Unquoted paths with .exe extension: C:\app.exe arg1 arg2
// 3. Simple tokens: app.exe arg1 arg2
// Returns pair of (executable path, remaining arguments)
std::pair<std::wstring, std::wstring> SmartSplitExeAndArgs(const std::wstring& joinedArgs) {
    std::wstring s = joinedArgs;
    if (s.empty()) return { L"", L"" };

    // Trim leading whitespace
    size_t start = s.find_first_not_of(L" \t");
    if (start == std::wstring::npos) return { L"", L"" };
    s = s.substr(start);

    if (s.front() == L'"') {
        // Handle quoted path: extract everything between quotes
        size_t endq = s.find(L'"', 1);
        if (endq == std::wstring::npos) return { L"", L"" };
        std::wstring exe = s.substr(1, endq - 1);
        std::wstring rest = L"";
        if (endq + 1 < s.size()) rest = s.substr(endq + 1);
        size_t rstart = rest.find_first_not_of(L" \t");
        if (rstart != std::wstring::npos) rest = rest.substr(rstart); else rest.clear();
        return { exe, rest };
    }
    else {
        // Handle unquoted path: find .exe extension marker
        std::wstring lower = s;
        std::transform(lower.begin(), lower.end(), lower.begin(), towlower);
        size_t pos = lower.find(L".exe");
        if (pos != std::wstring::npos) {
            // Extract up to and including .exe extension
            size_t exeEnd = pos + 4;
            std::wstring exe = s.substr(0, exeEnd);
            std::wstring rest = L"";
            if (exeEnd < s.size()) rest = s.substr(exeEnd);
            size_t rstart = rest.find_first_not_of(L" \t");
            if (rstart != std::wstring::npos) rest = rest.substr(rstart); else rest.clear();
            return { exe, rest };
        }
        else {
            // Fallback: treat first whitespace-delimited token as executable
            size_t sp = s.find_first_of(L" \t");
            if (sp == std::wstring::npos) return { s, L"" };
            std::wstring exe = s.substr(0, sp);
            std::wstring rest = s.substr(sp + 1);
            size_t rstart = rest.find_first_not_of(L" \t");
            if (rstart != std::wstring::npos) rest = rest.substr(rstart); else rest.clear();
            return { exe, rest };
        }
    }
}

// Main entry point for handling IFEO-redirected executable launches.
// When Windows redirects an executable to this launcher via IFEO, this function:
// 1. Checks if a modern replacement app exists in the mapping table
// 2. If found and it's a UWP app, launches it directly
// 3. If not found or launch fails, elevates to admin to temporarily disable IFEO,
//    launches the original executable, then re-enables IFEO
bool ProcessIFEORequest(const std::wstring& targetExePath, const std::wstring& argString) {
    // Combine and parse the full command line to extract executable and arguments
    std::wstring joined = targetExePath;
    if (!argString.empty()) {
        joined += L" ";
        joined += argString;
    }

    auto [exePath, exeArgs] = SmartSplitExeAndArgs(joined);
    if (exePath.empty()) {
        MessageBoxW(NULL, L"Failed to parse target executable from arguments.", L"Rebound Launcher", MB_ICONERROR);
        return false;
    }

    std::wstring exeNameOnly = FilenameFromPath(exePath);

    // Check if we have a modern replacement for this executable
    auto entOpt = FindLaunchEntry(exeNameOnly);
    if (entOpt) {
        bool ok = LaunchMappedEntry(*entOpt, exeArgs);
        if (ok) return true;
    }

    // No mapping found or launch failed; need to launch original executable.
    // This requires temporarily disabling the IFEO entry (needs admin rights).
    // Build command line for elevated relaunch with special --pauseIFEO mode.
    bool originallyElevated = IsProcessElevated();
    std::wstring adminArgs = L"--pauseIFEO \"" + exeNameOnly + L"\" ";
    adminArgs += (originallyElevated ? L"1 " : L"0 ");

    // Escape quotes in arguments for safe passing through command line
    std::wstring quotedArgs = L"\"";
    std::wstring tmp = exeArgs;
    size_t pos = 0;
    while ((pos = tmp.find(L'"', pos)) != std::wstring::npos) {
        tmp.insert(pos, L"\\");
        pos += 2;
    }
    quotedArgs += tmp;
    quotedArgs += L"\"";
    adminArgs += quotedArgs;

    bool relaunched = RelaunchAsAdminWithArgs(adminArgs);
    if (!relaunched) {
        MessageBoxW(NULL, L"Failed to elevate for IFEO handling.", L"Rebound Launcher", MB_ICONERROR);
        return false;
    }
    return true;
}

// Elevated handler for temporarily bypassing IFEO and launching the original executable.
// This function runs with admin rights after UAC prompt:
// 1. Pauses the IFEO entry (renames registry key)
// 2. Launches the original executable with appropriate privileges
// 3. Resumes the IFEO entry (restores registry key)
// Expected arguments: --pauseIFEO "<exeNameOnly>" <wasElevated:0|1> "<args...>"
int HandlePauseIFEOMode(int argc, wchar_t** argv) {
    if (argc < 4) {
        MessageBoxW(NULL, L"--pauseIFEO missing parameters.", L"Rebound Launcher", MB_ICONERROR);
        return 1;
    }

    std::wstring exeName = argv[2];
    bool wasElevated = (wcscmp(argv[3], L"1") == 0);
    std::wstring args;
    if (argc >= 5) {
        args = argv[4];
    }

    // Disable IFEO entry temporarily
    PauseIFEOEntry(exeName);

    PROCESS_INFORMATION pi{};
    bool launched = false;

    if (!wasElevated) {
        // Original process was not elevated; attempt to launch with user's normal privileges.
        // This prevents the target app from unexpectedly gaining admin rights.
        std::wstring exeAttempt = exeName;
        launched = LaunchProcessAsInteractiveUser(exeAttempt, args, pi);
        if (!launched) {
            // Fallback to simple CreateProcess if we can't get user token
            launched = LaunchProcessSimple(exeAttempt, args, pi);
        }
    }
    else {
        // Original was elevated; launch with current elevated privileges
        launched = LaunchProcessSimple(exeName, args, pi);
    }

    if (!launched) {
        std::wstring msg = L"Failed to launch target executable: " + exeName;
        MessageBoxW(NULL, msg.c_str(), L"Launch failed", MB_ICONERROR);
        // Ensure IFEO is restored even on failure
        ResumeIFEOEntry(exeName);
        return 1;
    }

    // Brief delay to ensure target process initializes before we restore IFEO
    Sleep(200);

    // Re-enable IFEO entry for future launches
    ResumeIFEOEntry(exeName);

    if (pi.hProcess) CloseHandle(pi.hProcess);
    if (pi.hThread) CloseHandle(pi.hThread);

    return 0;
}

// Launches a UWP application by its Package Family Name with optional arguments.
// Used when explicitly launching UWP apps rather than redirecting from Win32 executables.
// Command line: --launchPackage <package_family_name> <args...>
int HandleLaunchPackageMode(int argc, wchar_t** argv) {
    if (argc < 3) {
        MessageBoxW(NULL, L"No package family name specified.", L"Launch failed", MB_ICONERROR);
        return 1;
    }
    std::wstring packageFamily = argv[2];

    // Concatenate remaining arguments to pass to UWP app
    std::wstring args;
    for (int i = 3; i < argc; ++i) {
        if (i > 3) args += L" ";
        args += argv[i];
    }

    bool ok = LaunchUWPByAumid(packageFamily, args);
    if (!ok) {
        std::wstring msg = L"Failed to launch UWP app for package: " + packageFamily;
        MessageBoxW(NULL, msg.c_str(), L"Launch failed", MB_ICONERROR);
        return 1;
    }
    return 0;
}

// Window procedure for handling UI messages in the launcher's information window.
// Displays help text and provides actions for opening Rebound Hub or closing the window.
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_CREATE:
    {
        // Create informational UI elements
        CreateWindowExW(0, L"STATIC", L"Welcome to the Rebound Launcher!",
            WS_CHILD | WS_VISIBLE | SS_LEFT,
            0, 0, 0, 0, hWnd, (HMENU)IDC_LBL_HEADER, nullptr, nullptr);

        CreateWindowExW(0, L"STATIC",
            L"This tool helps you safely manage and launch the Rebound modding layer.\n"
            L"It's intended to be launched by other executables, not directly by users.",
            WS_CHILD | WS_VISIBLE | SS_LEFT,
            0, 0, 0, 0, hWnd, (HMENU)IDC_LBL_SUBTEXT, nullptr, nullptr);

        CreateWindowExW(0, L"BUTTON", L"Open Rebound Hub",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON,
            0, 0, 0, 0, hWnd, (HMENU)IDC_BTN_OPEN, nullptr, nullptr);

        CreateWindowExW(0, L"BUTTON", L"Close",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON,
            0, 0, 0, 0, hWnd, (HMENU)IDC_BTN_CLOSE, nullptr, nullptr);

        // Apply modern fonts scaled for current DPI
        UINT dpi = GetDpiForWindow(hWnd);
        HFONT hFontHeader = CreateFontW(-MulDiv(18, dpi, 72), 0, 0, 0, FW_SEMIBOLD,
            FALSE, FALSE, FALSE, DEFAULT_CHARSET,
            OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            CLEARTYPE_QUALITY, DEFAULT_PITCH | FF_DONTCARE, L"Segoe UI");
        HFONT hFontNormal = CreateFontW(-MulDiv(10, dpi, 72), 0, 0, 0, FW_NORMAL,
            FALSE, FALSE, FALSE, DEFAULT_CHARSET,
            OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
            CLEARTYPE_QUALITY, DEFAULT_PITCH | FF_DONTCARE, L"Segoe UI");

        SendMessageW(GetDlgItem(hWnd, IDC_LBL_HEADER), WM_SETFONT, (WPARAM)hFontHeader, TRUE);
        SendMessageW(GetDlgItem(hWnd, IDC_LBL_SUBTEXT), WM_SETFONT, (WPARAM)hFontNormal, TRUE);
        SendMessageW(GetDlgItem(hWnd, IDC_BTN_OPEN), WM_SETFONT, (WPARAM)hFontNormal, TRUE);
        SendMessageW(GetDlgItem(hWnd, IDC_BTN_CLOSE), WM_SETFONT, (WPARAM)hFontNormal, TRUE);
        return 0;
    }

    case WM_SIZE:
    {
        // Reposition UI elements based on new window dimensions with DPI-aware spacing
        RECT rc; GetClientRect(hWnd, &rc);
        int width = rc.right - rc.left;
        int height = rc.bottom - rc.top;

        int margin = Scale(hWnd, 24);
        int spacing = Scale(hWnd, 12);
        int buttonWidth = Scale(hWnd, 160);
        int buttonHeight = Scale(hWnd, 32);

        SetWindowPos(GetDlgItem(hWnd, IDC_LBL_HEADER), nullptr,
            margin, margin, width - 2 * margin, Scale(hWnd, 28), SWP_NOZORDER);
        SetWindowPos(GetDlgItem(hWnd, IDC_LBL_SUBTEXT), nullptr,
            margin, margin + Scale(hWnd, 40), width - 2 * margin, Scale(hWnd, 60), SWP_NOZORDER);
        SetWindowPos(GetDlgItem(hWnd, IDC_BTN_CLOSE), nullptr,
            width - margin - buttonWidth, height - margin - buttonHeight, buttonWidth, buttonHeight, SWP_NOZORDER);
        SetWindowPos(GetDlgItem(hWnd, IDC_BTN_OPEN), nullptr,
            width - margin - buttonWidth * 2 - spacing, height - margin - buttonHeight, buttonWidth, buttonHeight, SWP_NOZORDER);
        break;
    }

    case WM_COMMAND:
        switch (LOWORD(wParam)) {
        case IDC_BTN_OPEN:
            LaunchUWPByAumid(L"Rebound.Hub_rcz2tbwv5qzb8!App", L"");
            break;
        case IDC_BTN_CLOSE:
            PostQuitMessage(0);
            break;
        }
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProcW(hWnd, msg, wParam, lParam);
}

int WINAPI WinMain(HINSTANCE hInst, HINSTANCE, LPSTR, int) {
    // Enable per-monitor DPI awareness for crisp rendering on high-DPI displays
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Initialize common controls for modern Windows UI elements
    INITCOMMONCONTROLSEX icc{};
    icc.dwSize = sizeof(icc);
    icc.dwICC = ICC_STANDARD_CLASSES | ICC_WIN95_CLASSES;
    InitCommonControlsEx(&icc);

    // Parse command line arguments
    int argc;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (!argv) {
        MessageBoxW(NULL, L"Failed to parse command line.", L"Rebound Launcher", MB_ICONERROR);
        return 1;
    }

    // Determine operating mode based on command line arguments:
    // - No arguments: Display informational UI
    // - --launchPackage: Direct UWP app launch mode
    // - --pauseIFEO: Elevated mode for temporarily bypassing IFEO (called after UAC prompt)
    // - Other arguments: IFEO redirection mode (launched by Windows when user runs a redirected executable)

    if (argc >= 2) {
        if (wcscmp(argv[1], L"--launchPackage") == 0) {
            int ret = HandleLaunchPackageMode(argc, argv);
            LocalFree(argv);
            return ret;
        }
        else if (wcscmp(argv[1], L"--pauseIFEO") == 0) {
            // Elevated handler invoked after UAC prompt
            int ret = HandlePauseIFEOMode(argc, argv);
            LocalFree(argv);
            return ret;
        }
        else {
            // IFEO invocation: Windows has redirected an executable to this launcher.
            // Join all arguments and parse to determine target executable and its arguments.
            std::wstring joined;
            for (int i = 1; i < argc; ++i) {
                if (i > 1) joined += L" ";
                joined += argv[i];
            }

            bool handled = ProcessIFEORequest(joined, L"");
            LocalFree(argv);
            return handled ? 0 : 1;
        }
    }

    // UI mode: Show informational window when launched directly by user
    WNDCLASSEX wc = { sizeof(wc) };
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInst;
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    RegisterClassExW(&wc);

    // Create window with DPI-aware dimensions
    UINT dpiX = 96, dpiY = 96;
    HMONITOR hMonitor = MonitorFromPoint({ 0,0 }, MONITOR_DEFAULTTOPRIMARY);
    GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY);

    int width = MulDiv(520, dpiX, 96);
    int height = MulDiv(252, dpiY, 96);

    HWND hwnd = CreateWindowExW(
        0, CLASS_NAME, L"Rebound Launcher",
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT,
        width, height,
        nullptr, nullptr, hInst, nullptr
    );

    HICON hIcon = LoadIcon(hInst, MAKEINTRESOURCE(IDI_APP_ICON));
    SendMessage(hwnd, WM_SETICON, ICON_BIG, (LPARAM)hIcon);
    SendMessage(hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hIcon);

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    // Standard Windows message pump
    MSG msg{};
    while (GetMessageW(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    LocalFree(argv);
    return 0;
}