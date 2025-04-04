using Rebound.Helpers;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopWindow : WindowEx
{
    public DesktopWindow()
    {
        InitializeComponent();
        this.ToggleWindowStyle(false, WindowStyle.SizeBox | WindowStyle.Caption | WindowStyle.MaximizeBox | WindowStyle.MinimizeBox | WindowStyle.Border | WindowStyle.Iconic | WindowStyle.SysMenu);
        this.MoveAndResize(0, 0, Display.GetDisplayRect(this).Width, Display.GetDisplayRect(this).Height);
        RootFrame.Navigate(typeof(DesktopPage));
    }
}