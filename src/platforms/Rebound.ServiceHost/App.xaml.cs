﻿// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Windows.Win32.System.Shutdown;
using WinUI3Localizer;
using WinUIEx;

namespace Rebound.ServiceHost;

[ReboundApp("Rebound.ServiceHost", "")]
public partial class App : Application
{
    private TrustedPipeServer? PipeServer;

    public App()
    {

    }

    private void StartPipeServer()
    {
        PipeServer = new TrustedPipeServer("REBOUND_SERVICE_HOST");
        _ = PipeServer.StartAsync();

        PipeServer.MessageReceived += PipeServer_MessageReceived;
    }

    private static readonly SHUTDOWN_REASON[] MajorReasons =
    [
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_HARDWARE,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OPERATINGSYSTEM,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_HARDWARE,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_POWER,
        SHUTDOWN_REASON.SHTDN_REASON_MAJOR_SYSTEM
    ];

    private static readonly SHUTDOWN_REASON[] MinorReasons =
    [
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_OTHER,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_MAINTENANCE,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_INSTALLATION,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_HARDWARE_DRIVER,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_POWER_SUPPLY,
        SHUTDOWN_REASON.SHTDN_REASON_MINOR_BLUESCREEN
    ];

    private static readonly SHUTDOWN_REASON[] Flags =
    [
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_PLANNED,
        0x00000000, // Unplanned
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_USER_DEFINED,
        SHUTDOWN_REASON.SHTDN_REASON_FLAG_DIRTY_UI
    ];

    private async Task PipeServer_MessageReceived(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return;

        if (arg == "Shell::RestartToUEFI")
            RunShutdownCommand("/r /fw /t 0");

        else if (arg == "Shell::RestartToRecovery")
            RunShutdownCommand("/r /o /t 0");

        else if (arg == "Shell::Shutdown")
            RunShutdownCommand("/s /t 0");

        else if (arg == "Shell::Restart")
            RunShutdownCommand("/r /t 0");

        else if (arg.StartsWith("Shell::ShutdownServer#"))
        {
            var parts = arg["Shell::ShutdownServer#".Length..].ToCharArray();

            if (parts.Length >= 2 &&
                int.TryParse(parts[0].ToString(), out var reasonIndex) &&
                int.TryParse(parts[1].ToString(), out var modeIndex))
            {
                // Clamp to array length just to be safe
                reasonIndex = Math.Clamp(reasonIndex, 0, MajorReasons.Length - 1);
                modeIndex = Math.Clamp(modeIndex, 0, Flags.Length - 1);

                var reasonCode = MajorReasons[reasonIndex] | MinorReasons[reasonIndex] | Flags[modeIndex];

                RunShutdownCommand("/s /t 0", reasonCode);
            }
        }

        else if (arg.StartsWith("IFEOEngine::Pause#"))
        {
            var part = arg["IFEOEngine::Pause#".Length..];

            await IFEOEngine.PauseIFEOEntryAsync(part);
            await Task.Delay(1000);
            await IFEOEngine.ResumeIFEOEntryAsync(part);
        }

        return;
    }

    private void RunShutdownCommand(string args, SHUTDOWN_REASON reason)
    {
        _ = PInvoke.InitiateSystemShutdownEx(
            null, "Shutdown initiated via broker".ToPWSTR(),
            0, true, args.Contains("/r"), reason);
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

            // Server
            _ = Task.Run(StartPipeServer);

            // Activation
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
            MainAppWindow.Show();
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

            await PipeServer.BroadcastMessageAsync("Shell::SpawnCantRunDialog");
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
