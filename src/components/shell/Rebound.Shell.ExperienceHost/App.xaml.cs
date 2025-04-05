using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers;
using Rebound.Shell.Desktop;
using Rebound.ShellExperiencePack;
using Windows.Storage;
using Windows.Win32;
using WinUIEx;

namespace Rebound.Shell.ExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "Legacy Shell")]
public partial class App : Application
{
    public App()
    {

    }

    private async void Run()
    {
        var thread = new Thread(() =>
        {
            var hook1 = new WindowHook("#32770", "Shut Down Windows", "explorer");
            hook1.WindowDetected += Hook_WindowDetected_Shutdown;

            var hook2 = new WindowHook("#32770", "Run", "explorer");
            hook2.WindowDetected += Hook_WindowDetected_Run;

            // Keep message pump alive so both hooks keep working
            NativeMessageLoop();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        // Background window
        BackgroundWindow = new() { SystemBackdrop = new TransparentTintBackdrop(), IsMaximizable = false };
        BackgroundWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
        BackgroundWindow.SetWindowStyle(WindowStyle.Visible);
        BackgroundWindow.Activate();
        BackgroundWindow.MoveAndResize(0, 0, 0, 0);
        BackgroundWindow.Minimize();
        BackgroundWindow.SetWindowOpacity(0);

        RunWindow = new WindowEx();

        ShutdownDialog = new ShutdownDialog.ShutdownDialog();
        ShutdownDialog.SetDarkMode();
        ShutdownDialog.RemoveIcon();

        // Desktop window
        DesktopWindow = new DesktopWindow(ShutdownDialog);
        DesktopWindow.Activate();
        DesktopWindow.AttachToProgMan();

        await Task.Delay(1000).ConfigureAwait(true);

        ShutdownDialog.Minimize();
    }

    private const uint WM_CLOSE = 0x10; // WM_CLOSE constant

    private void Hook_WindowDetected_Run(object? sender, WindowDetectedEventArgs e)
    {
        // Send WM_CLOSE message to close the window
        PInvoke.SendMessage(new(e.Handle), WM_CLOSE, 0, 0);

        // Make sure to update the UI (run window activation) on the UI thread
        BackgroundWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            RunWindow?.Activate();
            RunWindow?.Restore();
        });
    }

    private void Hook_WindowDetected_Shutdown(object? sender, WindowDetectedEventArgs e)
    {
        PInvoke.DestroyWindow(new(e.Handle));
        BackgroundWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            ShutdownDialog?.Activate();
            ShutdownDialog?.Restore();
        });
    }

    private static void NativeMessageLoop()
    {
        while (true)
        {
            PInvoke.GetMessage(out var msg, Windows.Win32.Foundation.HWND.Null, 0, 0);
            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
    }

    private void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            Run();
        }
    }

    public static WindowEx? RunWindow { get; set; }
    public static WindowEx? DesktopWindow { get; set; }
    public static WindowEx? ShutdownDialog { get; set; }
    public static WindowEx? BackgroundWindow { get; set; }
}