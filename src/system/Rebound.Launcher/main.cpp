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

enum class HAlign {
    Left,
    Center,
    Right,
    Stretch
};

enum class VAlign {
    Top,
    Center,
    Bottom,
    Stretch
};

struct Margins {
    float left;
    float top;
    float right;
    float bottom;
};

struct Button {
    std::wstring text;
    RECT rect;
    float width;
    float height;
    HAlign hAlign;   // horizontal alignment
    VAlign vAlign;   // vertical alignment
    Margins margin;  // per-side spacing
    bool hovered = false;
    bool pressed = false;
    Color currentBg;
    Color targetBg;

    bool HitTest(POINT pt) const {
        return PtInRect(&rect, pt);
    }
};

Color LerpColor(const Color& a, const Color& b, float t) {
    BYTE r = (BYTE)(a.GetR() + (b.GetR() - a.GetR()) * t);
    BYTE g = (BYTE)(a.GetG() + (b.GetG() - a.GetG()) * t);
    BYTE b_ = (BYTE)(a.GetB() + (b.GetB() - a.GetB()) * t);
    BYTE a_ = (BYTE)(a.GetA() + (b.GetA() - a.GetA()) * t);
    return Color(a_, r, g, b_);
}

static std::vector<Button> g_buttons;
static int g_focusedButton = -1;
static int g_mouseDownButton = -1;

void UpdateButtonLayout(Button& btn, const Size& windowSize) {
    float x = 0, y = 0;
    float w = btn.width;
    float h = btn.height;

    // Horizontal alignment
    switch (btn.hAlign) {
    case HAlign::Left:
        x = btn.margin.left;
        break;
    case HAlign::Right:
        x = windowSize.Width - w - btn.margin.right;
        break;
    case HAlign::Center:
        x = (windowSize.Width - w) / 2.0f;
        break;
    case HAlign::Stretch:
        x = btn.margin.left;
        w = windowSize.Width - btn.margin.left - btn.margin.right;
        break;
    }

    // Vertical alignment
    switch (btn.vAlign) {
    case VAlign::Top:
        y = btn.margin.top;
        break;
    case VAlign::Bottom:
        y = windowSize.Height - h - btn.margin.bottom;
        break;
    case VAlign::Center:
        y = (windowSize.Height - h) / 2.0f;
        break;
    case VAlign::Stretch:
        y = btn.margin.top;
        h = windowSize.Height - btn.margin.top - btn.margin.bottom;
        break;
    }

    btn.rect.left = (LONG)x;
    btn.rect.top = (LONG)y;
    btn.rect.right = (LONG)(x + w);
    btn.rect.bottom = (LONG)(y + h);
}

// ----- DRAWING LOGIC -----
void DrawButton(Graphics& g, const Button& btn, int btnIndex, REAL radius = 4.0f) {
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

    SolidBrush brush(btn.currentBg);
    g.FillPath(&brush, &path);

    Pen pen(colors.border, 1.0f);
    g.DrawPath(&pen, &path);

    // Draw focus rectangle if this button has keyboard focus
    if ((int)btnIndex == g_focusedButton) {
        // Outer white border (2px)
        float outerPenWidth = 2.0f;
        float outerRadius = 4.0f; // corner radius
        RectF outerRect(
            (REAL)btn.rect.left - outerPenWidth + 0.5f,
            (REAL)btn.rect.top - outerPenWidth + 0.5f,
            (REAL)(btn.rect.right - btn.rect.left + 2 * outerPenWidth - 1),
            (REAL)(btn.rect.bottom - btn.rect.top + 2 * outerPenWidth - 1)
        );

        GraphicsPath outerPath;
        outerPath.AddArc(outerRect.X + outerPenWidth / 2, outerRect.Y + outerPenWidth / 2,
            outerRadius * 2, outerRadius * 2, 180.0f, 90.0f);
        outerPath.AddArc(outerRect.GetRight() - outerRadius * 2 - outerPenWidth / 2, outerRect.Y + outerPenWidth / 2,
            outerRadius * 2, outerRadius * 2, 270.0f, 90.0f);
        outerPath.AddArc(outerRect.GetRight() - outerRadius * 2 - outerPenWidth / 2, outerRect.GetBottom() - outerRadius * 2 - outerPenWidth / 2,
            outerRadius * 2, outerRadius * 2, 0.0f, 90.0f);
        outerPath.AddArc(outerRect.X + outerPenWidth / 2, outerRect.GetBottom() - outerRadius * 2 - outerPenWidth / 2,
            outerRadius * 2, outerRadius * 2, 90.0f, 90.0f);
        outerPath.CloseFigure();

        Pen whitePen(Color(255, 255, 255, 255), outerPenWidth);
        g.DrawPath(&whitePen, &outerPath);

        // Inner black border (1px)
        float innerPenWidth = 1.0f;
        float innerRadius = 2.0f; // corner radius
        RectF innerRect = outerRect;
        innerRect.Inflate(-outerPenWidth, -outerPenWidth);

        GraphicsPath innerPath;
        innerPath.AddArc(innerRect.X + innerPenWidth / 2, innerRect.Y + innerPenWidth / 2,
            innerRadius * 2, innerRadius * 2, 180.0f, 90.0f);
        innerPath.AddArc(innerRect.GetRight() - innerRadius * 2 - innerPenWidth / 2, innerRect.Y + innerPenWidth / 2,
            innerRadius * 2, innerRadius * 2, 270.0f, 90.0f);
        innerPath.AddArc(innerRect.GetRight() - innerRadius * 2 - innerPenWidth / 2, innerRect.GetBottom() - innerRadius * 2 - innerPenWidth / 2,
            innerRadius * 2, innerRadius * 2, 0.0f, 90.0f);
        innerPath.AddArc(innerRect.X + innerPenWidth / 2, innerRect.GetBottom() - innerRadius * 2 - innerPenWidth / 2,
            innerRadius * 2, innerRadius * 2, 90.0f, 90.0f);
        innerPath.CloseFigure();

        Pen blackPen(Color(255, 0, 0, 0), innerPenWidth);
        g.DrawPath(&blackPen, &innerPath);
    }

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
    switch (msg) {
    case WM_CREATE: {
        ButtonColors colors = GetButtonColors(IsDarkMode());

        g_buttons.emplace_back();
        auto& b1 = g_buttons.back();
        b1.text = L"Open Rebound Hub";
        b1.width = 160;
        b1.height = 32;
        b1.hAlign = HAlign::Right;
        b1.vAlign = VAlign::Bottom;
        b1.margin = { 0, 0, 190, 24 };
        b1.currentBg = colors.bgNormal;
        b1.targetBg = colors.bgNormal;

        g_buttons.emplace_back();
        auto& b2 = g_buttons.back();
        b2.text = L"Close";
        b2.width = 160;
        b2.height = 32;
        b2.hAlign = HAlign::Right;
        b2.vAlign = VAlign::Bottom;
        b2.margin = { 0, 0, 24, 24 }; 
        b2.currentBg = colors.bgNormal;
        b2.targetBg = colors.bgNormal;

        SetTimer(hWnd, 1, 16, NULL);
        return 0;
    }

    case WM_TIMER: {
        bool needRedraw = false;
        float step = 0.016f / 0.083f; // fraction per tick
        for (auto& btn : g_buttons) {
            ButtonColors colors = GetButtonColors(IsDarkMode());
            Color desiredBg;
            if (btn.pressed) desiredBg = colors.bgPressed;
            else if (btn.hovered) desiredBg = colors.bgHover;
            else desiredBg = colors.bgNormal;

            btn.targetBg = desiredBg;
            btn.currentBg = LerpColor(btn.currentBg, btn.targetBg, step);

            if (btn.currentBg.GetValue() != btn.targetBg.GetValue()) needRedraw = true;
        }
        if (needRedraw) InvalidateRect(hWnd, nullptr, FALSE);
        break;
    }

    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);

        RECT rc;
        GetClientRect(hWnd, &rc);
        int width = rc.right - rc.left;
        int height = rc.bottom - rc.top;

        // Double buffering
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
        for (size_t i = 0; i < g_buttons.size(); i++) {
            UpdateButtonLayout(g_buttons[i], Size(width, height));
            DrawButton(g, g_buttons[i], (int)i);
        }

        BitBlt(hdc, 0, 0, width, height, memDC, 0, 0, SRCCOPY);

        DeleteObject(memBmp);
        DeleteDC(memDC);

        EndPaint(hWnd, &ps);
        return 0;
    }

    case WM_MOUSEMOVE: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        bool needRedraw = false;
        for (size_t i = 0; i < g_buttons.size(); i++) {
            bool wasHover = g_buttons[i].hovered;
            g_buttons[i].hovered = g_buttons[i].HitTest(pt);

            // Only keep pressed if the mouse is over this button
            if (g_mouseDownButton == (int)i) {
                if (g_buttons[i].hovered) {
                    g_buttons[i].pressed = true;   // pressed if still inside
                }
                else {
                    g_buttons[i].pressed = false;  // back to normal when outside
                }
            }

            if (wasHover != g_buttons[i].hovered)
                needRedraw = true;
        }
        if (needRedraw) InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_MOUSELEAVE: {
        for (auto& btn : g_buttons) {
            btn.hovered = false;
            btn.pressed = false;
        }
        g_mouseDownButton = -1;
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_LBUTTONDOWN: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        g_mouseDownButton = -1;
        g_focusedButton = -1; // clear keyboard focus when mouse is used
        for (size_t i = 0; i < g_buttons.size(); i++) {
            if (g_buttons[i].HitTest(pt)) {
                g_buttons[i].pressed = true;
                g_mouseDownButton = (int)i;
                break;
            }
        }
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_LBUTTONUP: {
        POINT pt = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
        if (g_mouseDownButton >= 0 && g_buttons[g_mouseDownButton].HitTest(pt)) {
            MessageBox(hWnd, g_buttons[g_mouseDownButton].text.c_str(), L"Clicked", MB_OK);
        }

        for (auto& btn : g_buttons)
            btn.pressed = false;
        g_mouseDownButton = -1;
        InvalidateRect(hWnd, nullptr, TRUE);
        break;
    }

    case WM_KEYDOWN: {
        if (g_focusedButton == -1 && !g_buttons.empty())
            g_focusedButton = 0;

        switch (wParam) {
        case VK_TAB:
        case VK_RIGHT:
        case VK_DOWN:
            g_focusedButton = (g_focusedButton + 1) % g_buttons.size();
            InvalidateRect(hWnd, nullptr, TRUE);
            break;
        case VK_LEFT:
        case VK_UP:
            g_focusedButton = (g_focusedButton - 1 + g_buttons.size()) % g_buttons.size();
            InvalidateRect(hWnd, nullptr, TRUE);
            break;
        case VK_RETURN:
        case VK_SPACE:
            if (g_focusedButton >= 0)
                MessageBox(hWnd, g_buttons[g_focusedButton].text.c_str(), L"Clicked", MB_OK);
            break;
        }
        break;
    }

    case WM_SIZE:
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_ERASEBKGND:
        return 1;

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
