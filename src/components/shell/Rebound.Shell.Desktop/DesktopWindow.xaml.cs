using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rebound.Helpers;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Shell.Desktop;

public sealed partial class DesktopWindow : WindowEx
{
    private WindowEx ShutdownDialog { get; set; }

    public DesktopWindow(WindowEx shutdownDialog)
    {
        ShutdownDialog = shutdownDialog;
        InitializeComponent();
        this.ToggleWindowStyle(false, WindowStyle.SizeBox | WindowStyle.Caption | WindowStyle.MaximizeBox | WindowStyle.MinimizeBox | WindowStyle.Border | WindowStyle.Iconic | WindowStyle.SysMenu);
        this.MoveAndResize(0, 0, Display.GetDisplayRect(this).Width, Display.GetDisplayRect(this).Height);
        RootFrame.Navigate(typeof(DesktopPage), this);
    }

    bool canClose;

    public void RestoreExplorerDesktop()
    {
        canClose = true;
        Close();
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        args.Handled = true;
        if (!canClose)
        {
            ShutdownDialog.Activate();
            ShutdownDialog.BringToFront();
        }
        else
        {
            Process.GetProcessesByName("explorer").FirstOrDefault()?.Kill();
            PInvoke.DestroyWindow(new(this.GetWindowHandle()));
        }
    }
}