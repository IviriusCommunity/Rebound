// LauncherWithIFEOHandling.cpp
// Build: link with Shcore.lib (for DPI helpers) and ole32.lib (for COM)
#include <windows.h>
#include <shellscalingapi.h>
#include <shobjidl.h> // IApplicationActivationManager
#include <atlbase.h>  // CComPtr helpers (optional)
#include <string>
#include <vector>
#include <optional>
#include <algorithm>
#include <iostream>
#include <codecvt>
#include <locale>
#pragma comment(lib, "Shcore.lib")
#pragma comment(lib, "Ole32.lib")

const wchar_t CLASS_NAME[] = L"ReboundLauncher";

// Control IDs
enum ControlIDs {
    IDC_BTN_OPEN = 1001,
    IDC_BTN_CLOSE = 1002,
    IDC_LBL_HEADER = 1003,
    IDC_LBL_SUBTEXT = 1004
};

// Named pipe server name & message template
constexpr char PIPE_NAME[] = R"(\\.\pipe\REBOUND_SERVICE_HOST)";
constexpr char IFEO_PAUSE_PREFIX[] = "IFEOEngine::Pause#";
constexpr char IFEO_RESUME_PREFIX[] = "IFEOEngine::Resume#";

// Placeholder package family name line to include when the service host is not found.
// Replace this with the real package family name of Rebound Service Host (or AUMID) as required.
const wchar_t* PLACEHOLDER_PFN = L"Rebound.ServiceHost_rcz2tbwv5qzb8";

// DPI scaling helper
int Scale(HWND hwnd, int value) {
    UINT dpi = GetDpiForWindow(hwnd);
    return MulDiv(value, dpi, 96);
}

LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Utility: split a wide command line into argv using CommandLineToArgvW
std::vector<std::wstring> GetCommandLineArgsW() {
    LPWSTR* argv;
    int argc;
    argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    std::vector<std::wstring> out;
    if (argv) {
        for (int i = 0; i < argc; ++i) {
            out.emplace_back(argv[i]);
        }
        LocalFree(argv);
    }
    return out;
}

// Utility: extract filename (without path) from full path
std::wstring FilenameFromPath(const std::wstring& path) {
    size_t pos = path.find_last_of(L"\\/");
    if (pos == std::wstring::npos) return path;
    return path.substr(pos + 1);
}

// Convert UTF-16 wide string to UTF-8 std::string
std::string WideToUtf8(const std::wstring& w) {
    if (w.empty()) return {};
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), NULL, 0, NULL, NULL);
    std::string strTo(size_needed, 0);
    WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), &strTo[0], size_needed, NULL, NULL);
    return strTo;
}

// Send a UTF-8 message to the named pipe (write then optionally read a short reply). Returns true on success.
bool SendPipeMessage(const std::string& msg, std::string* outReply = nullptr, DWORD timeoutMs = 1500) {
    // Wait for pipe (short)
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

    // Optionally read reply (non-blocking short)
    if (outReply) {
        char buffer[512];
        DWORD bytesRead = 0;
        // small wait for reply
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

// Launch UWP app by AppUserModelID (AUMID) with args using IApplicationActivationManager
// Returns true on success (HRESULT success)
bool LaunchUWPByAumid(const std::wstring& appUserModelId, const std::wstring& arguments) {
    HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
    bool coInit = SUCCEEDED(hr) || hr == RPC_E_CHANGED_MODE;
    if (FAILED(hr) && hr != RPC_E_CHANGED_MODE) {
        // failed to initialize COM
        return false;
    }

    IApplicationActivationManager* pAppActMgr = nullptr;
    hr = CoCreateInstance(CLSID_ApplicationActivationManager, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pAppActMgr));
    if (FAILED(hr) || !pAppActMgr) {
        if (coInit) CoUninitialize();
        return false;
    }

    // COM requires the caller to be in an elevated process to activate on behalf of another user,
    // but for desktop activation this should work if run in the user session.
    DWORD pid = 0;
    hr = pAppActMgr->ActivateApplication(appUserModelId.c_str(), arguments.empty() ? nullptr : arguments.c_str(), AO_NONE, &pid);

    pAppActMgr->Release();
    if (coInit) CoUninitialize();

    return SUCCEEDED(hr);
}

// Launch a normal Win32 process with full command line; returns PROCESS_INFORMATION on success via outProc (caller closes handles)
bool LaunchProcess(const std::wstring& exePath, const std::wstring& args, PROCESS_INFORMATION& outProc) {
    std::wstring cmdLine = L"\"" + exePath + L"\"";
    if (!args.empty()) {
        cmdLine += L" " + args;
    }

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    ZeroMemory(&outProc, sizeof(outProc));

    BOOL ok = CreateProcessW(
        exePath.c_str(),
        cmdLine.data(), // CreateProcess may modify the buffer
        NULL, NULL, FALSE,
        0, NULL, NULL,
        &si, &outProc);

    return ok == TRUE;
}

// Data model: mapping from executable filename -> known argument sets -> AppUserModelID (AUMID) to launch
struct KnownArgMapping {
    std::wstring Arg;                // exact argument string to match
    std::wstring AppUserModelId;     // full AUMID (PackageFamilyName!AppId or AUMID)
};

struct LaunchEntry {
    std::wstring ExecutableName;             // filename, e.g. "example.exe"
    std::vector<KnownArgMapping> KnownArgs;  // known argument -> AUMID
    // Optionally a default AUMID if you want
    std::optional<std::wstring> DefaultAumid;
};

// Populate your known mappings here. Fill with real AppUserModelIDs (PFN!AppId) and arg strings.
// EXAMPLE: replace the demo values with real executable names, known args, and AUMIDs.
std::vector<LaunchEntry> KnownLaunchEntries = {
    // Example entry -- REPLACE with real values
    {
        L"someapp.exe",
        {
            { L"--open-foo", L"58027.265370AB8DB33_fjemmk5ta3a5g!App" }, // example AUMID
            { L"--open-bar", L"58027.265370AB8DB33_fjemmk5ta3a5g!App" }
        },
        std::nullopt
    },

    // Character Map example (you can replace with real AUMID)
    {
        L"charmap.exe",
        {
            { L"--some-known-arg", L"Microsoft.Windows.CharacterMap_8wekyb3d8bbwe!App" }
        },
        std::nullopt
    }
};

// Try to find a LaunchEntry by executable name (case-insensitive)
std::optional<LaunchEntry> FindLaunchEntry(const std::wstring& exeName) {
    for (const auto& e : KnownLaunchEntries) {
        std::wstring a = e.ExecutableName;
        std::wstring b = exeName;
        std::transform(a.begin(), a.end(), a.begin(), ::towlower);
        std::transform(b.begin(), b.end(), b.begin(), ::towlower);
        if (a == b) return e;
    }
    return std::nullopt;
}

// Process incoming request: return true on success/handled, false on fatal error
bool ProcessRequest(const std::wstring& exePath, const std::wstring& argString) {
    std::wstring exeName = FilenameFromPath(exePath);

    auto entryOpt = FindLaunchEntry(exeName);
    if (entryOpt) {
        // Check known args for exact match
        for (const auto& km : entryOpt->KnownArgs) {
            if (km.Arg == argString) {
                // Launch the UWP with exactly these args
                bool ok = LaunchUWPByAumid(km.AppUserModelId, km.Arg);
                if (!ok) {
                    std::wstring msg = L"Failed to launch UWP app for " + exeName + L" with args: " + argString;
                    MessageBoxW(NULL, msg.c_str(), L"Launch failed", MB_ICONERROR);
                }
                return ok;
            }
        }

        // No known args matched -- use IFEO pause/resume path
        // First check the presence of Rebound Service Host via named pipe
        // If pipe server not available, show messagebox and cancel (as requested)
        std::string reply;
        std::string pauseMsg = std::string(IFEO_PAUSE_PREFIX) + std::string(WideToUtf8(exeName));
        bool pipeAvailable = SendPipeMessage(pauseMsg, &reply, 1000);

        if (!pipeAvailable) {
            // Service host not found: inform the user and cancel.
            MessageBoxW(NULL, L"Rebound Service Host not found. Operation cancelled.", L"Rebound Launcher", MB_ICONERROR);

            // Placeholder package family name line for you to replace later:
            // (user asked to include this line after cancelling)
            // Replace PLACEHOLDER_PFN with the real package family name / AUMID for Rebound Service Host.
            const wchar_t* placeholder = PLACEHOLDER_PFN;
            (void)placeholder; // keep the variable present so you can easily find & replace it

            return false;
        }

        // If available, proceed to launch the exe and later resume IFEO.
        PROCESS_INFORMATION pi{};
        bool launched = LaunchProcess(exePath, argString, pi);
        if (!launched) {
            std::wstring msg = L"Failed to launch target executable: " + exePath;
            MessageBoxW(NULL, msg.c_str(), L"Launch failed", MB_ICONERROR);
            // try to resume IFEO just in case
            std::string resumeMsg = std::string(IFEO_RESUME_PREFIX) + std::string(WideToUtf8(exeName));
            SendPipeMessage(resumeMsg, nullptr, 500);
            return false;
        }

        // Optionally wait a little bit for the launched process to start
        // (we don't wait for it to exit)
        Sleep(200);

        // Resume IFEO entry
        std::string resumeMsg = std::string(IFEO_RESUME_PREFIX) + std::string(WideToUtf8(exeName));
        SendPipeMessage(resumeMsg, nullptr, 500);

        // Close handles from CreateProcess
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        return true;
    }

    // No entry found: fallback behavior: just launch exe with args
    PROCESS_INFORMATION pi{};
    if (!LaunchProcess(exePath, argString, pi)) {
        std::wstring msg = L"Unknown executable mapping and failed to launch: " + exePath;
        MessageBoxW(NULL, msg.c_str(), L"Launch failed", MB_ICONERROR);
        return false;
    }
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return true;
}

// UI code unchanged, minimal window creation and layout
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_CREATE:
    {
        // Header label
        CreateWindowExW(0, L"STATIC", L"Welcome to the Rebound Launcher!",
            WS_CHILD | WS_VISIBLE | SS_LEFT,
            0, 0, 0, 0, hWnd, (HMENU)IDC_LBL_HEADER, nullptr, nullptr);

        // Subtext label
        CreateWindowExW(0, L"STATIC",
            L"This tool helps you safely manage and launch the Rebound modding layer.\n"
            L"It's intended to be launched by other executables, not directly by users.",
            WS_CHILD | WS_VISIBLE | SS_LEFT,
            0, 0, 0, 0, hWnd, (HMENU)IDC_LBL_SUBTEXT, nullptr, nullptr);

        // Open button
        CreateWindowExW(0, L"BUTTON", L"Open Rebound Hub",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON,
            0, 0, 0, 0, hWnd, (HMENU)IDC_BTN_OPEN, nullptr, nullptr);

        // Close button
        CreateWindowExW(0, L"BUTTON", L"Close",
            WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON,
            0, 0, 0, 0, hWnd, (HMENU)IDC_BTN_CLOSE, nullptr, nullptr);

        // Apply fonts
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
            MessageBoxW(hWnd, L"Open Rebound Hub clicked!", L"Action", MB_OK);
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
    // Make the process per-monitor DPI aware
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Parse command line
    auto args = GetCommandLineArgsW();
    // args[0] is the launcher exe itself. If greater than 1 => we have a target exe to handle.
    if (args.size() >= 2) {
        // The contract you promised: launched as "<executable_path> <arguments...>"
        // So args[1] is the target executable path, and rest are the arguments to it.
        std::wstring targetExe = args[1];
        std::wstring argString;
        if (args.size() > 2) {
            // Join the remaining args with spaces (preserve quoting by using original args)
            for (size_t i = 2; i < args.size(); ++i) {
                if (i > 2) argString += L" ";
                argString += args[i];
            }
        }

        bool handled = ProcessRequest(targetExe, argString);
        // We exit after handling as this run was specifically invoked by IFEO.
        return handled ? 0 : 1;
    }

    // Otherwise run UI (normal launcher mode)
    // Register window class
    WNDCLASSEX wc = { sizeof(wc) };
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInst;
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    RegisterClassExW(&wc);

    // Get DPI of the primary monitor
    UINT dpiX = 96, dpiY = 96;
    HMONITOR hMonitor = MonitorFromPoint({ 0,0 }, MONITOR_DEFAULTTOPRIMARY);
    GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY);

    // Scale desired width/height
    int width = MulDiv(520, dpiX, 96);
    int height = MulDiv(252, dpiY, 96);

    // Create window
    HWND hwnd = CreateWindowExW(
        0, CLASS_NAME, L"Rebound Launcher",
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT,
        width, height,
        nullptr, nullptr, hInst, nullptr
    );

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    MSG msg{};
    while (GetMessageW(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    return 0;
}
