#pragma once
#include <windows.h>
#include <shellapi.h>
#include <commctrl.h>
#pragma comment(lib, "Comctl32.lib")
#include <gdiplus.h>
#pragma comment(lib, "gdiplus.lib")
#include <string>
#include <windowsx.h>

using namespace Gdiplus;

const wchar_t CLASS_NAME[] = L"ReboundWrapperClass";
HFONT g_hFont;

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
    return value == 0; // 0 = dark, 1 = light
}

Color HslaToColor(float h, float s, float l, float a) {
    // Simplified for grayscale colors where s=0
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
            HslaToColor(0,0,1.0f,0.061f),  // bgNormal
            HslaToColor(0,0,1.0f,0.084f),  // bgHover
            HslaToColor(0,0,1.0f,0.033f),  // bgPressed
            HslaToColor(0,0,1.0f,0.0698f), // border
            Color(255,255,255,255),        // textNormal
            Color(255,255,255,255),        // textHover
            Color(192,255,255,255)         // textPressed ~ 75% opacity
        };
    }
    else {
        return {
            HslaToColor(0,0,1.0f,0.7f),    // bgNormal
            HslaToColor(0,0,0.98f,0.5f),   // bgHover
            HslaToColor(0,0,0.98f,0.3f),   // bgPressed
            HslaToColor(0,0,0.0f,0.0578f), // border
            Color(255,0,0,0),              // textNormal black
            Color(255,0,0,0),              // textHover black
            Color(192,0,0,0)               // textPressed black 75%
        };
    }
}

Color LerpColor(const Color& from, const Color& to, float t) {
    BYTE a = (BYTE)(from.GetA() + (to.GetA() - from.GetA()) * t);
    BYTE r = (BYTE)(from.GetR() + (to.GetR() - from.GetR()) * t);
    BYTE g = (BYTE)(from.GetG() + (to.GetG() - from.GetG()) * t);
    BYTE b = (BYTE)(from.GetB() + (to.GetB() - from.GetB()) * t);
    return Color(a, r, g, b);
}

static HWND g_buttons[2] = { nullptr, nullptr };
static HWND g_hoveredButton = nullptr;
WNDPROC g_origButtonProc[2] = { nullptr, nullptr };

LRESULT CALLBACK ButtonSubclassProc(HWND hButton, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_MOUSEMOVE: {
        if (g_hoveredButton != hButton) {
            // Invalidate previous hovered button
            if (g_hoveredButton) InvalidateRect(g_hoveredButton, NULL, FALSE);
            g_hoveredButton = hButton;
            InvalidateRect(g_hoveredButton, NULL, FALSE);

            // Track mouse leave for this button
            TRACKMOUSEEVENT tme = { sizeof(tme) };
            tme.dwFlags = TME_LEAVE;
            tme.hwndTrack = hButton;
            TrackMouseEvent(&tme);
        }
        break;
    }

    case WM_MOUSELEAVE: {
        if (g_hoveredButton == hButton) {
            InvalidateRect(g_hoveredButton, NULL, FALSE);
            g_hoveredButton = nullptr;
        }
        break;
    }
    }

    return CallWindowProc(
        g_origButtonProc[(hButton == g_buttons[0]) ? 0 : 1],
        hButton, msg, wParam, lParam
    );
}

class ThemeButtonDrawer {
public:
    static void Draw(LPDRAWITEMSTRUCT pdis, REAL radius = 4.0f) {
        bool darkMode = IsDarkMode();
        ButtonColors colors = GetButtonColors(darkMode);

        bool isHover = (pdis->hwndItem == g_hoveredButton);

        Color bg, text;
        if (pdis->itemState & ODS_DISABLED) {
            bg = colors.bgNormal; text = colors.textNormal;
        }
        else if (pdis->itemState & ODS_SELECTED) {
            bg = colors.bgPressed; text = colors.textPressed;
        }
        else if (isHover) {
            bg = colors.bgHover; text = colors.textHover;
        }
        else {
            bg = colors.bgNormal; text = colors.textNormal;
        }

        // Step 1: Create graphics for button
        Graphics graphics(pdis->hDC);
        graphics.SetSmoothingMode(SmoothingModeAntiAlias);
        graphics.Clear(Color(0, 0, 0, 0));

        // Step 2: BitBlt the parent background into the button DC
        {
            RECT rcParent;
            GetClientRect(GetParent(pdis->hwndItem), &rcParent);
            HDC hdcParent = GetDC(GetParent(pdis->hwndItem));

            RECT rcButton;
            GetWindowRect(pdis->hwndItem, &rcButton);
            MapWindowPoints(NULL, GetParent(pdis->hwndItem), (POINT*)&rcButton, 2);

            BitBlt(
                pdis->hDC,
                0, 0,                                   // dest is top-left of button DC
                rcButton.right - rcButton.left,
                rcButton.bottom - rcButton.top,
                hdcParent,
                rcButton.left, rcButton.top,             // source is parent-relative
                SRCCOPY
            );

            ReleaseDC(GetParent(pdis->hwndItem), hdcParent);
        }

        // Step 3: Draw button shape and text on top
        RectF rectF(
            (REAL)pdis->rcItem.left + 1,
            (REAL)pdis->rcItem.top + 1,
            (REAL)(pdis->rcItem.right - pdis->rcItem.left - 2),
            (REAL)(pdis->rcItem.bottom - pdis->rcItem.top - 2)
        );

        GraphicsPath path;
        path.AddArc(rectF.X, rectF.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rectF.GetRight() - radius * 2, rectF.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rectF.GetRight() - radius * 2, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rectF.X, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();

        SolidBrush brush(bg);
        graphics.FillPath(&brush, &path);

        Pen pen(colors.border, 1.0f);
        graphics.DrawPath(&pen, &path);

        wchar_t textBuf[128];
        GetWindowText(pdis->hwndItem, textBuf, 128);
        FontFamily fontFamily(L"Segoe UI");
        Font font(&fontFamily, 12.0f, FontStyleRegular, UnitPixel);
        StringFormat sf;
        sf.SetAlignment(StringAlignmentCenter);
        sf.SetLineAlignment(StringAlignmentCenter);
        SolidBrush textBrush(text);
        graphics.DrawString(textBuf, -1, &font, rectF, &sf, &textBrush);
    }
};

HWND g_hBottomBar = nullptr; // bottom bar window

LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {

    case WM_CREATE: {
        // Create owner-draw buttons
        auto createButton = [&](const wchar_t* text, int x, int y, int id, int index) {
            HWND hBtn = CreateWindow(L"BUTTON", text,
                WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                x, y, 120, 32, hWnd, (HMENU)id, GetModuleHandle(NULL), NULL);
            SendMessage(hBtn, WM_SETFONT, (WPARAM)g_hFont, TRUE);
            g_buttons[index] = hBtn;
            return hBtn;
            };

        g_buttons[0] = createButton(L"Open Rebound", 12, 12, 1001, 0);
        g_buttons[1] = createButton(L"Open Original", 140, 12, 1002, 1);

        // Subclass buttons for hover tracking
        for (int i = 0; i < 2; i++) {
            g_origButtonProc[i] = (WNDPROC)SetWindowLongPtr(
                g_buttons[i], GWLP_WNDPROC, (LONG_PTR)ButtonSubclassProc
            );
        }

        return 0;
    }

    case WM_COMMAND:
        if (LOWORD(wParam) == 1001) MessageBox(hWnd, L"Open Rebound clicked", L"Info", MB_OK);
        if (LOWORD(wParam) == 1002) MessageBox(hWnd, L"Open Original clicked", L"Info", MB_OK);
        return 0;

    case WM_DRAWITEM: {
        LPDRAWITEMSTRUCT pdis = (LPDRAWITEMSTRUCT)lParam;
        if (pdis->CtlType == ODT_BUTTON) {
            ThemeButtonDrawer::Draw(pdis); // Draw button with hover/pressed/disabled
        }
        return TRUE;
    }

    case WM_ERASEBKGND:
        return 1; // Prevent flicker, we paint everything in WM_PAINT

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);
        Graphics g(hdc);

        g.Clear(Color(0, 0, 0, 0));

        RECT rcClient;
        GetClientRect(hWnd, &rcClient);

        // Draw bottom bar first (behind buttons)
        ButtonColors colors = GetButtonColors(IsDarkMode());
        RectF barRect(0.0f, (REAL)(rcClient.bottom - 80), (REAL)rcClient.right, 80.0f);
        SolidBrush brush(colors.bgNormal); // includes alpha
        g.FillRectangle(&brush, barRect);

        EndPaint(hWnd, &ps);
        return 0;
    }

    case WM_SIZE:
        // Invalidate entire window (will redraw bar and buttons)
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProc(hWnd, msg, wParam, lParam);
}

int WINAPI WinMain(HINSTANCE hInst, HINSTANCE, LPSTR, int) {
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (!argv) return -1;

    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    GdiplusStartupInput gdiplusStartupInput;
    ULONG_PTR gdiplusToken;
    GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

    NONCLIENTMETRICS ncm = { sizeof(ncm) };
    SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(ncm), &ncm, 0);
    g_hFont = CreateFontIndirectW(&ncm.lfMessageFont);

    INITCOMMONCONTROLSEX icex = { sizeof(icex), ICC_STANDARD_CLASSES };
    InitCommonControlsEx(&icex);

    if (argc <= 1) { // no args -> show UI
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
    }
    else {
        // fast path: quick parse & forward (you mentioned you already handle this)
    }

    LocalFree(argv);
    return 0;
}