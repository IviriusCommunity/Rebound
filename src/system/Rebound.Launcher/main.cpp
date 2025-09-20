#pragma once
#include <windows.h>
#include <shellapi.h>
#include <commctrl.h>
#include <gdiplus.h>
#include <string>
#include <vector>
#include <algorithm>
#include <memory>
#include <windowsx.h>
#include <dwmapi.h>

#pragma comment(lib, "dwmapi.lib")
#pragma comment(lib, "Comctl32.lib")
#pragma comment(lib, "gdiplus.lib")

using namespace Gdiplus;

const wchar_t CLASS_NAME[] = L"ReboundWrapperClass";
HFONT g_hFont;

enum class HAlign { Left, Center, Right, Stretch };
enum class VAlign { Top, Center, Bottom, Stretch };

struct Margins { float left, top, right, bottom; };

struct ButtonColors {
    Color bgNormal;
    Color bgHover;
    Color bgPressed;
    Color border;
    Color textNormal;
    Color textHover;
    Color textPressed;
};

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

struct ButtonDrawColors {
    Color bg;
    Color text;
    bool hasBorder;
    Color border;
};

Color LerpColor(const Color& a, const Color& b, float t) {
    BYTE r = (BYTE)(a.GetR() + (b.GetR() - a.GetR()) * t);
    BYTE g = (BYTE)(a.GetG() + (b.GetG() - a.GetG()) * t);
    BYTE b_ = (BYTE)(a.GetB() + (b.GetB() - a.GetB()) * t);
    BYTE a_ = (BYTE)(a.GetA() + (b.GetA() - a.GetA()) * t);
    return Color(a_, r, g, b_);
}

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

Color AdjustLuminance(const Color& c, float factor);
Color LerpColor(const Color& a, const Color& b, float t);
Color ToGrayscale(const Color& c, float amount);
float GetRelativeLuminance(const Color& c);
bool IsDarkMode();

// Increase saturation by factor (>1)
Color IncreaseSaturation(const Color& c, float factor) {
    BYTE a = c.GetA();
    float r = c.GetR() / 255.0f;
    float g = c.GetG() / 255.0f;
    float b = c.GetB() / 255.0f;

    // Compute gray (average)
    float gray = (r + g + b) / 3.0f;

    // Move each channel away from gray by factor
    r = gray + (r - gray) * factor;
    g = gray + (g - gray) * factor;
    b = gray + (b - gray) * factor;

    // Clamp
    r = std::clamp(r, 0.0f, 1.0f);
    g = std::clamp(g, 0.0f, 1.0f);
    b = std::clamp(b, 0.0f, 1.0f);

    return Color(a, (BYTE)(r * 255.0f), (BYTE)(g * 255.0f), (BYTE)(b * 255.0f));
}

struct Button;

ButtonDrawColors GetButtonColorsForDraw(const Button& btn);
void DrawButtonBase(Graphics& g, const Button& btn, int btnIndex, REAL radius = 4.0f);
void UpdateButtonLayout(Button& btn, const Size& windowSize);

struct Button {
    std::wstring text;
    RECT rect{};
    float width = 0, height = 0;
    HAlign hAlign = HAlign::Center;
    VAlign vAlign = VAlign::Center;
    Margins margin{ 0,0,0,0 };

    bool hovered = false;
    bool pressed = false;

    Color currentBg{};
    Color targetBg{};

    Button() = default;
    Button(const std::wstring& t, float w_, float h_,
        HAlign ha, VAlign va, Margins m)
        : text(t), width(w_), height(h_), hAlign(ha), vAlign(va), margin(m) {
    }

    virtual ~Button() = default;

    virtual ButtonColors GetColors(bool darkMode) const {
        return ::GetButtonColors(darkMode);
    }

    virtual void Draw(Graphics& g, int btnIndex) const {
        auto drawColors = GetButtonColorsForDraw(*this);
        DrawButtonBase(g, *this, btnIndex, 4.0f);
    }

    virtual bool HitTest(POINT pt) const { return PtInRect(&rect, pt); }

    virtual void UpdateLayout(const Size& windowSize) {
        UpdateButtonLayout(*this, windowSize);
    }
};

struct AccentButton : public Button {
    using Button::Button;

    virtual ButtonColors GetColors(bool darkMode) const override {
        DWORD accentRGB = 0;
        DWORD size = sizeof(accentRGB);
        if (RegGetValueW(HKEY_CURRENT_USER,
            L"Software\\Microsoft\\Windows\\DWM",
            L"AccentColor", RRF_RT_REG_DWORD, nullptr,
            &accentRGB, &size) != ERROR_SUCCESS) {
            accentRGB = 0xFF0078D7; // fallback
        }

        BYTE a = (accentRGB >> 24) & 0xFF;
        BYTE r = (accentRGB >> 0) & 0xFF;
        BYTE g = (accentRGB >> 8) & 0xFF;
        BYTE b = (accentRGB >> 16) & 0xFF;
        Color baseAccent(a, r, g, b);

        Color normal = darkMode ? IncreaseSaturation(AdjustLuminance(baseAccent, 1.6f), 0.9f)
            : IncreaseSaturation(AdjustLuminance(baseAccent, 0.6f), 1.4f);
        Color hover = LerpColor(normal, Color(255, 255, 255, 255), 0.05f);
        Color pressed = ToGrayscale(normal, 0.2f);
        Color text = darkMode ? Color(255, 0, 0, 0) : Color(255, 255, 255, 255);
        Color textPressed = darkMode ? Color(160, 0, 0, 0) : Color(160, 255, 255, 255);

        return { normal, hover, pressed, Color(0,0,0,0), text, text, textPressed };
    }
};


ButtonDrawColors GetButtonColorsForDraw(const Button& btn)
{
    bool darkMode = IsDarkMode();
    ButtonColors colors = btn.GetColors(darkMode); // <-- polymorphic call!

    Color text;
    bool hasBorder = true;

    Color target;
    if (btn.pressed) {
        target = colors.bgPressed;
        text = colors.textPressed;
    }
    else if (btn.hovered) {
        target = colors.bgHover;
        text = colors.textHover;
    }
    else {
        target = colors.bgNormal;
        text = colors.textNormal;
    }

    // Lerp currentBg toward target for smooth animation
    float t = 0.083f; // tweak speed
    const_cast<Button&>(btn).currentBg = LerpColor(btn.currentBg, target, t);

    // If this is an AccentButton, we don't want a border
    if (dynamic_cast<const AccentButton*>(&btn))
        hasBorder = false;

    return { target, text, hasBorder, colors.border };
}

struct ButtonManager {
    std::vector<std::unique_ptr<Button>> buttons;
    int focusedButton = -1;
    int mouseDownButton = -1;

    void AddButton(std::unique_ptr<Button> btn) {
        buttons.push_back(std::move(btn)); // take ownership
    }

    Button* GetButton(size_t i) {
        return buttons[i].get(); // return raw pointer if needed
    }

    void DrawAll(Graphics& g, const Size& winSize) {
        for (size_t i = 0; i < buttons.size(); i++) {
            buttons[i]->UpdateLayout(winSize);
            buttons[i]->Draw(g, (int)i);
        }
    }

    bool HandleMouseMove(POINT pt) {
        bool needRedraw = false;
        for (size_t i = 0; i < buttons.size(); i++) {
            bool wasHover = buttons[i]->hovered;
            buttons[i]->hovered = buttons[i]->HitTest(pt);

            if (mouseDownButton == (int)i)
                buttons[i]->pressed = buttons[i]->hovered;

            if (wasHover != buttons[i]->hovered)
                needRedraw = true;
        }
        return needRedraw;
    }

    void HandleMouseDown(POINT pt) {
        mouseDownButton = -1;
        focusedButton = -1;
        for (size_t i = 0; i < buttons.size(); i++) {
            if (buttons[i]->HitTest(pt)) {
                buttons[i]->pressed = true;
                mouseDownButton = (int)i;
                break;
            }
        }
    }

    void HandleMouseUp(POINT pt, HWND hWnd) {
        if (mouseDownButton >= 0 && buttons[mouseDownButton]->HitTest(pt))
            MessageBox(hWnd, buttons[mouseDownButton]->text.c_str(), L"Clicked", MB_OK);

        for (auto& btn : buttons) btn->pressed = false;
        mouseDownButton = -1;
    }

    void HandleKeyboard(WPARAM key, HWND hWnd) {
        if (focusedButton == -1 && !buttons.empty())
            focusedButton = 0;

        switch (key) {
        case VK_TAB:
        case VK_RIGHT:
        case VK_DOWN:
            focusedButton = (focusedButton + 1) % buttons.size();
            break;
        case VK_LEFT:
        case VK_UP:
            focusedButton = (focusedButton - 1 + buttons.size()) % buttons.size();
            break;
        case VK_RETURN:
        case VK_SPACE:
            if (focusedButton >= 0)
                MessageBox(hWnd, buttons[focusedButton]->text.c_str(), L"Clicked", MB_OK);
            break;
        }
    }
};


Color GetSystemAccentColor() {
    DWORD accentArgb = 0xFF0078D7; // fallback: default Win10 blue
    HKEY hKey;
    if (RegOpenKeyExW(HKEY_CURRENT_USER,
        L"Software\\Microsoft\\Windows\\DWM", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        DWORD size = sizeof(accentArgb);
        RegQueryValueExW(hKey, L"AccentColor", nullptr, nullptr, (LPBYTE)&accentArgb, &size);
        RegCloseKey(hKey);
    }

    BYTE a = (accentArgb >> 24) & 0xFF;
    BYTE r = (accentArgb >> 16) & 0xFF;
    BYTE g = (accentArgb >> 8) & 0xFF;
    BYTE b = accentArgb & 0xFF;

    return Color(a, r, g, b);
}

// Relative luminance calculation (per WCAG)
float GetRelativeLuminance(const Color& c) {
    auto channel = [](float v) {
        v /= 255.0f;
        return (v <= 0.03928f) ? (v / 12.92f) : powf((v + 0.055f) / 1.055f, 2.4f);
        };
    float R = channel((float)c.GetR());
    float G = channel((float)c.GetG());
    float B = channel((float)c.GetB());
    return 0.2126f * R + 0.7152f * G + 0.0722f * B;
}

// Adjust brightness perceptually
Color AdjustLuminance(const Color& c, float factor) {
    // factor > 1.0 = lighter, factor < 1.0 = darker
    float r = c.GetR() * factor;
    float g = c.GetG() * factor;
    float b = c.GetB() * factor;
    r = max(0.0f, min(255.0f, r));
    g = max(0.0f, min(255.0f, g));
    b = max(0.0f, min(255.0f, b));
    return Color(c.GetA(), (BYTE)r, (BYTE)g, (BYTE)b);
}

// Desaturate (convert towards grayscale)
Color ToGrayscale(const Color& c, float amount) {
    // amount = 0 -> original, 1 -> full grayscale
    BYTE gray = (BYTE)(0.3f * c.GetR() + 0.59f * c.GetG() + 0.11f * c.GetB());
    BYTE r = (BYTE)(c.GetR() * (1 - amount) + gray * amount);
    BYTE g = (BYTE)(c.GetG() * (1 - amount) + gray * amount);
    BYTE b = (BYTE)(c.GetB() * (1 - amount) + gray * amount);
    return Color(c.GetA(), r, g, b);
}

static std::vector<Button> g_buttons;
static int g_focusedButton = -1;
static int g_mouseDownButton = -1;

void DrawButtonBase(Graphics& g, const Button& btn, int btnIndex, REAL radius) {
    auto colors = GetButtonColorsForDraw(btn);

    // Determine rect
    RectF rectF((REAL)btn.rect.left, (REAL)btn.rect.top,
        (REAL)(btn.rect.right - btn.rect.left),
        (REAL)(btn.rect.bottom - btn.rect.top));

    // Accent button expansion
    if (dynamic_cast<const AccentButton*>(&btn)) {
        rectF.X -= 0.5f;
		rectF.Y -= 0.5f;
        rectF.Width += 1.0f;
        rectF.Height += 1.0f;
    }

    // Rounded rect path
    GraphicsPath path;
    path.AddArc(rectF.X, rectF.Y, radius * 2, radius * 2, 180, 90);
    path.AddArc(rectF.GetRight() - radius * 2, rectF.Y, radius * 2, radius * 2, 270, 90);
    path.AddArc(rectF.GetRight() - radius * 2, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 0, 90);
    path.AddArc(rectF.X, rectF.GetBottom() - radius * 2, radius * 2, radius * 2, 90, 90);
    path.CloseFigure();

    // Fill background
    SolidBrush brush(colors.bg);
    g.FillPath(&brush, &path);

    // Draw border if needed
    if (colors.hasBorder) {
        Pen pen(colors.border, 1.0f);
        g.DrawPath(&pen, &path);
    }

    // Draw text
    FontFamily fontFamily(L"Segoe UI");
    Font font(&fontFamily, 14.0f, FontStyleRegular, UnitPixel);
    StringFormat sf;
    sf.SetAlignment(StringAlignmentCenter);
    sf.SetLineAlignment(StringAlignmentCenter);
    SolidBrush textBrush(colors.text);
    g.DrawString(btn.text.c_str(), -1, &font, rectF, &sf, &textBrush);

    // Focus outline (optional)
    if (btnIndex == g_focusedButton) {
        Pen focusPen(Color(255, 255, 255, 255), 2.0f);
        g.DrawPath(&focusPen, &path);
    }
}

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

struct Label {
    std::wstring text;
    RECT rect{};
    float width = 0, height = 0;
    HAlign hAlign = HAlign::Left;
    VAlign vAlign = VAlign::Top;
    Margins margin{ 0,0,0,0 };

    float fontSize = 12.0f;
    bool semibold = false;

    virtual ~Label() = default;

    virtual void Draw(Graphics& g, const Color& themeColor) const {
        RectF rectF((REAL)rect.left, (REAL)rect.top,
            (REAL)(rect.right - rect.left),
            (REAL)(rect.bottom - rect.top));

        FontFamily fontFamily(L"Segoe UI");
        INT style = semibold ? FontStyleBold : FontStyleRegular;
        Font font(&fontFamily, fontSize, style, UnitPixel);

        StringFormat sf;
        sf.SetAlignment(StringAlignmentNear);
        sf.SetLineAlignment(StringAlignmentNear);
        sf.SetFormatFlags(StringFormatFlagsLineLimit); // wrap text
        sf.SetTrimming(StringTrimmingWord);

        SolidBrush brush(themeColor);
        g.DrawString(text.c_str(), -1, &font, rectF, &sf, &brush);
    }

    void UpdateLayout(const Size& windowSize) {
        float x = margin.left;
        float y = margin.top;
        float w = width;
        float h = height;

        switch (hAlign) {
        case HAlign::Center: x = (windowSize.Width - w) / 2.0f; break;
        case HAlign::Right:  x = windowSize.Width - w - margin.right; break;
        case HAlign::Stretch: w = windowSize.Width - margin.left - margin.right; break;
        default: break;
        }

        switch (vAlign) {
        case VAlign::Center: y = (windowSize.Height - h) / 2.0f; break;
        case VAlign::Bottom: y = windowSize.Height - h - margin.bottom; break;
        case VAlign::Stretch: h = windowSize.Height - margin.top - margin.bottom; break;
        default: break;
        }

        rect.left = (LONG)x;
        rect.top = (LONG)y;
        rect.right = (LONG)(x + w);
        rect.bottom = (LONG)(y + h);
    }
};

static ButtonManager g_btnMgr;

static std::vector<std::unique_ptr<Label>> g_labels;

void SetWindowDarkMode(HWND hWnd, bool dark)
{
    const DWORD useDark = dark ? 1 : 0;
    DwmSetWindowAttribute(hWnd, 20 /*DWMWA_USE_IMMERSIVE_DARK_MODE*/, &useDark, sizeof(useDark));
}

// ----- WINDOW PROC -----
LRESULT CALLBACK WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_CREATE:
    {
        SetWindowDarkMode(hWnd, IsDarkMode());

        g_btnMgr.AddButton(std::make_unique<AccentButton>(L"Open Rebound Hub", 160, 32,
            HAlign::Right, VAlign::Bottom, Margins{ 0,0,190,24 }));
        g_btnMgr.AddButton(std::make_unique<Button>(L"Close", 160, 32,
            HAlign::Right, VAlign::Bottom, Margins{ 0,0,24,24 }));

        // Header label
        auto header = std::make_unique<Label>();
        header->text = L"Welcome to the Rebound Launcher!";
        header->fontSize = 20.0f;
        header->semibold = true;
        header->hAlign = HAlign::Stretch;
        header->vAlign = VAlign::Top;
        header->margin = { 24, 16, 24, 0 };
        header->width = 250; // approximate wrapping width
        header->height = 40;
        g_labels.push_back(std::move(header));

        // Sub-label
        auto subLabel = std::make_unique<Label>();
        subLabel->text = L"This tool helps you safely manage and launch the Rebound modding layer. It’s intended to be launched by other executables, not by users.";
        subLabel->fontSize = 14.0f;
        subLabel->semibold = false;
        subLabel->hAlign = HAlign::Stretch;
        subLabel->vAlign = VAlign::Top;
        subLabel->margin = { 24, 60, 24, 0 };
        subLabel->width = 250;
        //subLabel->height = 32;
        g_labels.push_back(std::move(subLabel));

        SetTimer(hWnd, 1, 16, NULL);
        return 0;
    }

    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hWnd, &ps);
        RECT rc; GetClientRect(hWnd, &rc);
        int width = rc.right - rc.left, height = rc.bottom - rc.top;

        HDC memDC = CreateCompatibleDC(hdc);
        HBITMAP memBmp = CreateCompatibleBitmap(hdc, width, height);
        SelectObject(memDC, memBmp);

        Graphics g(memDC);
        g.SetSmoothingMode(SmoothingModeAntiAlias);
        g.Clear(Color(0, 0, 0, 0));

        // Bottom bar
        ButtonColors colors = GetButtonColors(IsDarkMode());
        SolidBrush brush(colors.bgNormal);  // create named object
        g.FillRectangle(&brush, RectF(0, (REAL)(height - 80), (REAL)width, 80.0f));

        // Labels
        for (auto& lbl : g_labels) {
            lbl->UpdateLayout(Size(width, height));
            lbl->Draw(g, GetButtonColors(IsDarkMode()).textNormal);
        }

        // Buttons
        g_btnMgr.DrawAll(g, Size(width, height));
        BitBlt(hdc, 0, 0, width, height, memDC, 0, 0, SRCCOPY);

        DeleteObject(memBmp); DeleteDC(memDC);
        EndPaint(hWnd, &ps);
        return 0;
    }

    case WM_MOUSEMOVE:
        if (g_btnMgr.HandleMouseMove({ GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) }))
            InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_LBUTTONDOWN:
        g_btnMgr.HandleMouseDown({ GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) });
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_LBUTTONUP:
        g_btnMgr.HandleMouseUp({ GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) }, hWnd);
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

    case WM_KEYDOWN:
        g_btnMgr.HandleKeyboard(wParam, hWnd);
        InvalidateRect(hWnd, nullptr, TRUE);
        break;

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

    HWND hwnd = CreateWindowEx(0, CLASS_NAME, L"Rebound Launcher",
        WS_OVERLAPPEDWINDOW & ~WS_MAXIMIZEBOX,
        CW_USEDEFAULT, CW_USEDEFAULT, 520, 252,
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
