#include <windows.h>
#include <shellscalingapi.h>
#pragma comment(lib, "Shcore.lib")

const wchar_t CLASS_NAME[] = L"ReboundLauncher";

// Control IDs
enum ControlIDs {
    IDC_BTN_OPEN = 1001,
    IDC_BTN_CLOSE = 1002,
    IDC_LBL_HEADER = 1003,
    IDC_LBL_SUBTEXT = 1004
};

// DPI scaling helper
int Scale(HWND hwnd, int value) {
    UINT dpi = GetDpiForWindow(hwnd);
    return MulDiv(value, dpi, 96);
}

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