using WinUIEx;

namespace Rebound.Helpers.Windowing;

public static class WindowHelper
{
    public static void SetWindowIcon(this WindowEx window, string iconPath)
    {
        window.SetIcon(iconPath);
        window.SetTaskBarIcon(Icon.FromFile(iconPath));
    }
}
