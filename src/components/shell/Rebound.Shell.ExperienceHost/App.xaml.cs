using System;
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
        await Task.Run(() =>
        {
            var hook = new WindowHook("32770", "Shut Down Windows");
            hook.WindowDetected += Hook_WindowDetected;
        }).ConfigureAwait(true);

        // Background window
        BackgroundWindow = new WindowEx();
        BackgroundWindow = new() { SystemBackdrop = new TransparentTintBackdrop(), IsMaximizable = false };
        BackgroundWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
        BackgroundWindow.SetWindowStyle(WindowStyle.Visible);
        BackgroundWindow.Activate();
        BackgroundWindow.MoveAndResize(0, 0, 0, 0);
        BackgroundWindow.Minimize();
        BackgroundWindow.SetWindowOpacity(0);

        ShutdownDialog = new ShutdownDialog.ShutdownDialog();

        // Desktop window
        DesktopWindow = new DesktopWindow(ShutdownDialog);
        DesktopWindow.Activate();
        DesktopWindow.AttachToProgMan();

        await Task.Delay(1000).ConfigureAwait(true);

        ShutdownDialog.Minimize();
    }

    private void Hook_WindowDetected(object? sender, WindowDetectedEventArgs e)
    {
        PInvoke.DestroyWindow(new(e.Handle));
        DesktopWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            ShutdownDialog?.Activate();
            ShutdownDialog?.Restore();
        });
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