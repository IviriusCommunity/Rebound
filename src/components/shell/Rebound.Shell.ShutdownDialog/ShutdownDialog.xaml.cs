using WinUIEx;

namespace Rebound.Shell.ShutdownDialog;

public sealed partial class ShutdownDialog : WindowEx
{
    public ShutdownDialog()
    {
        InitializeComponent();
        this.CenterOnScreen();

        var manager = WindowManager.Get(this);
        manager.WindowMessageReceived += Manager_WindowMessageReceived;
    }

    private const int WM_ACTIVATE = 0x0006;
    private const int WA_ACTIVE = 1;
    private const int WA_INACTIVE = 0;

    private void Manager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == WM_ACTIVATE)
        {
            if (e.Message.WParam == WA_INACTIVE)
            {
                this.Minimize();
            }
        }
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        args.Handled = true;
        this.Minimize();
    }
}
