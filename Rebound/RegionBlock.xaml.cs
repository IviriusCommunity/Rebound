using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RegionBlock : Window
{
    public RegionBlock()
    {
        this.InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
        SystemBackdrop = new TransparentTintBackdrop();
        this.SetIsMinimizable(false);
        this.SetIsMaximizable(false);
        this.SetIsAlwaysOnTop(true);
        Activate();
        this.Maximize();
        Load();
    }

    // Constants for SystemParametersInfo function
    private const int SPI_GETDESKWALLPAPER = 0x0073;
    private const int MAX_PATH = 260;

    // P/Invoke declaration for SystemParametersInfo function
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

    // Method to retrieve the current user's wallpaper path
    private string GetWallpaperPath()
    {
        StringBuilder wallpaperPath = new StringBuilder(MAX_PATH);
        SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);
        return wallpaperPath.ToString();
    }

    public async void LoadWallpaper()
    {
        try
        {
            RegionBkg.Source = new BitmapImage(new Uri(GetWallpaperPath(), UriKind.RelativeOrAbsolute));
        }
        catch
        {

        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetClientRect(IntPtr hWnd, out RECT rect);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;

    private void BlockTaskbarInteraction()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var desktopHwnd = GetDesktopWindow();

        // Get the desktop rectangle
        GetWindowRect(desktopHwnd, out RECT desktopRect);
        var windowRect = new RECT { Left = 0, Top = 0, Right = desktopRect.Right, Bottom = desktopRect.Bottom };

        // Set your window size and position
        SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top, SWP_NOMOVE);

        // To intercept input, you'll need a window that captures input events.
        // Consider using low-level hooks or other methods to ensure the taskbar cannot be interacted with.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public async void Load()
    {
        await Task.Delay(800);
        LoadWallpaper();
        await Task.Delay(200);
        //BlockTaskbarInteraction();
        BlockDialog.XamlRoot = this.Content.XamlRoot;
        await BlockDialog.ShowAsync();
        Close();
    }
}
