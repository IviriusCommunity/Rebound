using WinUIEx;

namespace Rebound.ShellExperiencePack;

#nullable enable

public partial class TrayWindow : WindowEx
{

    public TrayWindow()
    {
        try
        {
            /*SystemBackdrop = new TransparentTintBackdrop();
            IsMaximizable = false;
            this.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
            this.SetWindowStyle(WindowStyle.Visible);
            Activate();
            this.MoveAndResize(0, 0, 0, 0);
            this.Minimize();
            this.SetWindowOpacity(0);*/

            var icon = new H.NotifyIcon.Core.TrayIcon();
            icon.Create();
            icon.Show();
            icon.UpdateToolTip("Rebound Shell is operational.");
        }
        catch { }
    }
}