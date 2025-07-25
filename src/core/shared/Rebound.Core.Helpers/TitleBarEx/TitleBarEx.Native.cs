using Windows.Win32;
using WinRT.Interop;
using WinUIEx;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx
{
    // Window messages
    private const int WM_NCLBUTTONDOWN = 0x00A1;       // Non client left button down
    private const int WM_NCHITTEST = 0x0084;           // Non client hit test
    private const int WM_NCLBUTTONUP = 0x00A2;         // Non client left button up
    private const int WM_NCRBUTTONUP = 0x00A5;         // Non client right button up
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;     // Non client left button double click
    private const int WM_ACTIVATE = 0x0006;            // Activate

    // NCHITTEST response
    private const int HTMINBUTTON = 8;                 // Hit test minimize button
    private const int HTMAXBUTTON = 9;                 // Hit test maximize button
    private const int HTCLOSE = 20;                    // Hit test close button
    private const int HTBORDER = 18;                   // Hit test border (for no interaction)

    // Window frame
    private const int WND_FRAME_LEFT = 7;              // Window frame width
    private const int WND_FRAME_TOP_MAXIMIZED = 7;     // Top window frame (it goes up by 6px when maximized because of how Windows works)
    private const int WND_FRAME_TOP_NORMAL = 1;        // Top window frame (not maximized)

    // Others
    private const int VK_LBUTTON = 0x01;               // Virtual key code for the left mouse button

    private static bool IsLeftMouseButtonDown()
        // The high-order bit indicates if the key is down
        => (PInvoke.GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

    /// <summary>
    /// Check if the window is focused.
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public static bool IsWindowFocused(WindowEx window)
    {
        // Get HWND for this window
        var hwnd = new Windows.Win32.Foundation.HWND(WindowNative.GetWindowHandle(window));

        // Get the foreground window
        var foregroundHwnd = PInvoke.GetForegroundWindow();

        // Compare
        return hwnd == foregroundHwnd;
    }
}
