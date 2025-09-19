#pragma once
#include <windows.h>
#include <shellapi.h>
#include <commctrl.h>
#include <gdiplus.h>
#include <string>
#include <vector>
#include <windowsx.h>

#pragma comment(lib, "Comctl32.lib")
#pragma comment(lib, "gdiplus.lib")

using namespace Gdiplus;

const wchar_t CLASS_NAME[] = L"ReboundWrapperClass";
HFONT g_hFont;

// ----- THEMING LOGIC -----
bool IsDarkMode() {
    HKEY hKey;
    DWORD value = 1; // default to light
    if (RegOpenKeyEx(HKEY_CURRENT_USER,
        L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
        0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        DWORD dataSize = sizeof(value);
        RegQueryValueEx(hKey, L"AppsUseLightTheme", NULL, NULL, (LPBYTE)&value, &dataSize);
        RegCloseKey(hKey);
    }
    return value == 0;
}

Color HslaToColor(float h, float s, float l, float a) {
    BYTE alpha = (BYTE)(a * 255.0f);
    BYTE lum = (BYTE)(l * 255.0f);
    return Color(alpha, lum, lum, lum);
}

struct ButtonColors {
    Color bgNormal;
    Color bgHover;
    Color bgPressed;
    Color border;
    Color textNormal;
    Color textHover;
    Color textPressed;
};

ButtonColors GetButtonColors(bool darkMode) {
    if (darkMode) {
        return {
            HslaToColor(0,0,1.0f,0.061f),
            HslaToColor(0,0,1.0f,0.084f),
            HslaToColor(0,0,1.0f,0.033f),
            HslaToColor(0,0,1.0f,0.0698f),
            Color(255,255,255,255),
            Color(255,255,255,255),
            Color(192,255,255,255)
        };
    }
    else {
        return {
            HslaToColor(0,0,1.0f,0.7f),
            HslaToColor(0,0,0.98f,0.5f),
            HslaToColor(0,0,0.98f,0.3f),
            HslaToColor(0,0,0.0f,0.0578f),
            Color(255,0,0,0),
            Color(255,0,0,0),
            Color(192,0,0,0)
        };
    }
}

// ----- BUTTON STRUCTURE -----
struct Button {
    std::wstring text;
    RECT rect;
    bool hovered = false;
    bool pressed = false;

    bool HitTest(POINT pt) const {
        return PtInRect(&rect, pt) != FALSE;
    }
};

static std::vector<Button> g_buttons;

// ----- DRAWING LOGIC -----
void DrawButton(Graphics& g, const Button& btn, REAL radius = 4.0f) {
    bool darkMode = IsDarkMode();
    ButtonColors colors = GetButtonColors(darkMode);

    Color bg, text;
    if (btn.pressed) {
        bg = colors.bgPressed;
        text = colors.textPressed;
    }
    else if (btn.hovered) {
        bg = colors.bgHover;
        text = colors.textHover;
    }
    else {
        bg = colors.bgNormal;
        text = colors.textNormal;
    }

    RectF rectF((REAL)btn.rect.left + 1, (REAL)btn.rect.top + 1,
        (REAL)(btn.rect.right - btn.rect.left - 2),
        (REAL)(btn.rect.bottom - btn.rect.top - 2));

    GraphicsPath path;
    path.AddArc(rectF.X, rectF.Y, radius * 2, radius * 2, 180, 90);
    path.AddArc(rectF.GetRight() - radius * 2, rectF.Y, radius * 2, radius * 2, 270, 90);
    path.AddArc(rectF.GetRight() - radius * 2, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 0, 90);
    path.AddArc(rectF.X, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 90, 90);
    path.CloseFigure();

    SolidBrush brush(bg);
    g.FillPath(&brush, &path);

    Pen pen(colors.border, 1.0f);
    g.DrawPath(&pen, &path);

    FontFamily fontFamily(L"Segoe UI");
    Font font(&fontFamily, 12.0f, FontStyleRegular, UnitPixel);
    StringFormat sf;
    sf.SetAlignment(StringAlignmentCenter);
    sf.SetLineAlignment(StringAlignmentCenter);
    SolidBrush textBrush(text);
    g.DrawString(btn.text.c_str(), -1, &font, rectF, &sf, &textBrush);
}

// ----- WINDOW PROC -----
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    static bool mouseDown = false;

    switch (msg) {
    case WM_CREATE: {
        // Define buttons
        g_buttons = {
            { L"Open Rebound", {12, 12, 132, 44} },
            { L"Open Original", {140, 12, 260, 44} }
        };
        return 0;
    }

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);

        RECT rc;
        GetClientRect(hWnd, &rc);
        int width = rc.right - rc.left;
        int height = rc.bottom - rc.top;

        // Create a compatible DC and bitmap
        HDC memDC = CreateCompatibleDC(hdc);
        HBITMAP memBmp = CreateCompatibleBitmap(hdc, width, height);
        SelectObject(memDC, memBmp);

        Graphics g(memDC);
        g.SetSmoothingMode(SmoothingModeAntiAlias);

        // Clear background
        g.Clear(Color(0, 0, 0, 0));

        // Draw bottom bar
        ButtonColors colors = GetButtonColors(IsDarkMode());
        RectF barRect(0, (REAL)(height - 80), (REAL)width, 80.0f);
        SolidBrush brush(colors.bgNormal);
        g.FillRectangle(&brush, barRect);

        // Draw buttons
        for (const Button& btn : g_buttons) {
            DrawButton(g, btn);
        }

        // Blit to screen
        BitBlt(hdc, 0, 0, width, height, memDC, 0, 0, SRCCOPY);

        // Cleanup
        DeleteObject(memBmp);
        DeleteDC(memDC);

        EndPaint(hWnd, &ps);
        return 0;
    }

    case WM_MOUSEMOVE: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        bool needRedraw = false;
        for (auto& btn : g_buttons) {
            bool wasHover = btn.hovered;
            btn.hovered = btn.HitTest(pt);
            if (btn.hovered != wasHover) needRedraw = true;
        }
        if (needRedraw) InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_LBUTTONDOWN: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        for (auto& btn : g_buttons) {
            btn.pressed = btn.HitTest(pt);
        }
        mouseDown = true;
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_LBUTTONUP: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        for (auto& btn : g_buttons) {
            if (btn.pressed && btn.HitTest(pt)) {
                // Button clicked
                MessageBox(hWnd, btn.text.c_str(), L"Clicked", MB_OK);
            }
            btn.pressed = false;
        }
        mouseDown = false;
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_MOUSELEAVE: {
        for (auto& btn : g_buttons) btn.hovered = false;
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_SIZE:
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_ERASEBKGND:
        return 1; // avoid flicker

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProc(hWnd, msg, wParam, lParam);
}

// ----- ENTRY POINT -----
int WINAPI WinMain(HINSTANCE hInst, HINSTANCE, LPSTR, int) {
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    GdiplusStartupInput gdiplusStartupInput;
    ULONG_PTR gdiplusToken;
    GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

    NONCLIENTMETRICS ncm = { sizeof(ncm) };
    SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(ncm), &ncm, 0);
    g_hFont = CreateFontIndirectW(&ncm.lfMessageFont);

    INITCOMMONCONTROLSEX icex = { sizeof(icex), ICC_STANDARD_CLASSES };
    InitCommonControlsEx(&icex);

    WNDCLASSEX wc = { sizeof(wc) };
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInst;
    wc.lpszClassName = CLASS_NAME;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    RegisterClassEx(&wc);

    HWND hwnd = CreateWindowEx(0, CLASS_NAME, L"Rebound — selector",
        WS_OVERLAPPEDWINDOW & ~WS_MAXIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT, 300, 110,
        NULL, NULL, hInst, NULL);

    ShowWindow(hwnd, SW_SHOW);
    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    GdiplusShutdown(gdiplusToken);
    return 0;
}
