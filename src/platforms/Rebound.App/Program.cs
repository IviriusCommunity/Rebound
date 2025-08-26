using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.WinRT.WinRT;

namespace Rebound
{
    internal class Program
    {
        static private App _xamlApp = null;

        [STAThread]
        static unsafe void Main(string[] args)
        {
            fixed (char* lpszClassName = "XamlIslandsClass")
            fixed (char* lpWindowName = "Rebound Hub")
            {
                WNDCLASSW wc;
                wc.lpfnWndProc = &WndProc;
                wc.hInstance = GetModuleHandleW(null);
                wc.lpszClassName = lpszClassName;
                RegisterClassW(&wc);

                CreateWindowExW(WS_EX_NOREDIRECTIONBITMAP, wc.lpszClassName, lpWindowName, WS_OVERLAPPEDWINDOW | WS_VISIBLE, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, HWND.NULL, HMENU.NULL, wc.hInstance, null);

                MSG msg;
                while (GetMessageW(&msg, HWND.NULL, 0, 0))
                {
                    bool xamlSourceProcessedMessage = _xamlApp is not null && _xamlApp.PreTranslateMessage(&msg);
                    if (!xamlSourceProcessedMessage)
                    {
                        TranslateMessage(&msg);
                        DispatchMessageW(&msg);
                    }
                }
            }
        }

        [UnmanagedCallersOnly]
        private static LRESULT WndProc(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case WM_CREATE:
                    OnWindowCreate(hWnd);
                    break;
                case WM_SIZE:
                    _xamlApp?.OnResize(LOWORD(lParam), HIWORD(lParam));

                    break;
                case WM_SETTINGCHANGE:
                case WM_THEMECHANGED:
                    _xamlApp?.ProcessCoreWindowMessage(message, wParam, lParam);

                    break;
                case WM_SETFOCUS:
                    _xamlApp?.OnSetFocus();

                    break;
                case WM_DESTROY:
                    _xamlApp = null;
                    PostQuitMessage(0);
                    break;
                default:
                    return DefWindowProcW(hWnd, message, wParam, lParam);
            }
            return 0;
        }

        private static void OnWindowCreate(HWND hwnd)
        {
            RoInitialize(RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
            _xamlApp = new(hwnd);
        }
    }
}
