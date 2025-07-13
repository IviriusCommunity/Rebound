using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Core.Helpers;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Win32;
using WinUI3Localizer;

namespace Rebound.ServiceHost;

[ReboundApp("Rebound.ServiceHost", "")]
public partial class App : Application
{
    private TrustedPipeServer? PipeServer;

    private void StartPipeServer()
    {
        PipeServer = new TrustedPipeServer("REBOUND_SERVICE_HOST", IsTrustedClient);
        _ = PipeServer.StartAsync();

        PipeServer.MessageReceived += PipeServer_MessageReceived;
    }

    private Task PipeServer_MessageReceived(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return Task.CompletedTask;

        switch (arg)
        {
            case "Shell::RestartToUEFI":
                RunShutdownCommand("/r /fw /t 0");
                break;

            case "Shell::RestartToRecovery":
                RunShutdownCommand("/r /o /t 0");
                break;

            case "Shell::Shutdown":
                RunShutdownCommand("/s /t 0");
                break;

            case "Shell::Restart":
                RunShutdownCommand("/r /t 0");
                break;
        }

        return Task.CompletedTask;
    }

    private void RunShutdownCommand(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = arguments,
            UseShellExecute = true,
            CreateNoWindow = true
        };
        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log or handle error here if needed
            Debug.WriteLine($"Failed to execute shutdown command: {ex.Message}");
        }
    }

    private bool IsTrustedClient(string? exePath)
    {
#if DEBUG
    // In debug builds, trust all clients for easier development/testing
    return true;
#else
        if (string.IsNullOrEmpty(exePath))
            return false;

        // Flatten all known trusted paths from your instructions
        var trustedPaths = ReboundTotalInstructions.AppInstrunctions
            .Where(inst => !string.IsNullOrWhiteSpace(inst.EntryExecutable))
            .Select(inst => inst.EntryExecutable)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return trustedPaths.Any(path => string.Equals(path, exePath, StringComparison.OrdinalIgnoreCase));
#endif
    }

    public static ILocalizer Localizer { get; set; }

    private async void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            // Localizations
            var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

            var stringsFolder = await StorageFolder.GetFolderFromPathAsync(stringsFolderPath);

            Localizer = new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en-US";
                })
                .Build();

            var stringFolders = await stringsFolder.GetFoldersAsync(Windows.Storage.Search.CommonFolderQuery.DefaultQuery);

            if (stringFolders.Any(item =>
                item.Name.Equals(GlobalizationPreferences.Languages[0], StringComparison.OrdinalIgnoreCase)))
            {
                Localizer.SetLanguage(GlobalizationPreferences.Languages[0]);
            }
            else
            {
                Localizer.SetLanguage("en-US");
            }

            // Window hooks
            var thread = new Thread(() =>
            {
                var hook1 = new WindowHook("#32770", "Shut Down Windows", "explorer");
                hook1.WindowDetected += Hook_WindowDetected_Shutdown;

                var hook2 = new WindowHook("#32770", "RunBoxTitle".GetLocalizedString(), "explorer");
                hook2.WindowDetected += Hook_WindowDetected_Run;

                var hook3 = new WindowHook("#32770", "RunBoxTitleTaskManager".GetLocalizedString(), "taskmgr");
                hook3.WindowDetected += Hook_WindowDetected_Run;

                var hook4 = new WindowHook("Shell_Dialog", "This app can’t run on your PC", "explorer");
                hook4.WindowDetected += Hook_WindowDetected_CantRun;

                // Keep message pump alive so all hooks keep working
                NativeMessageLoop();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            // Activation
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();

            // Server
            StartPipeServer();
        }
    }

    private const uint WM_CLOSE = 0x10; // WM_CLOSE constant

    private async void Hook_WindowDetected_Run(object? sender, WindowDetectedEventArgs e)
    {
        if (SettingsHelper.GetValue("InstallRun", "rebound", true))
        {
            // Send WM_CLOSE message to close the window
            PInvoke.SendMessage(new(e.Handle), WM_CLOSE, 0, 0);

            await PipeServer.BroadcastMessageAsync("Shell::SpawnRunWindow");
        }
    }

    private async void Hook_WindowDetected_Shutdown(object? sender, WindowDetectedEventArgs e)
    {
        if (SettingsHelper.GetValue("InstallShutdownDialog", "rebound", true))
        {
            PInvoke.PostMessage(new(e.Handle), WM_CLOSE, new Windows.Win32.Foundation.WPARAM(0), IntPtr.Zero);
            PInvoke.DestroyWindow(new(e.Handle));

            await PipeServer.BroadcastMessageAsync("Shell::SpawnShutdownDialog");
        }
    }

    private async void Hook_WindowDetected_CantRun(object? sender, WindowDetectedEventArgs e)
    {
        if (PInvoke.IsWindow(new(e.Handle)) && SettingsHelper.GetValue("InstallThisAppCantRunOnYourPC", "rebound", true))
        {
            // Send WM_CLOSE asynchronously, non-blocking
            PInvoke.PostMessage(new(e.Handle), WM_CLOSE, new Windows.Win32.Foundation.WPARAM(0), IntPtr.Zero);

            await PipeServer.BroadcastMessageAsync("Shell::SpawnCantRunWindow");
        }
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
}
