using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers;
using Rebound.Helpers.Services;
using Rebound.Run.Helpers;
using WinUIEx;

#nullable enable

namespace Rebound.Run;

[ReboundApp("Rebound.Run", "Legacy Run")]
public partial class App : Application
{
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    private static WinEventDelegate _winEventProc = WinEventProc;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    public static WindowEx? BackgroundWindow { get; private set; }
    public static WindowEx? MainWindow { get; set; }

    public App()
    {
        InitializeComponent();
        //LaunchWork();
    }

    private void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            LaunchWork();
        }
    }

    private void LaunchWork()
    {
        _ = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        InitializeBackgroundWindow();
        MainWindow = new MainWindow();
        RunDialogReplace();
        if (IsStartupArgumentPresent()) return;
        ActivateMainWindowAsync();
        return;
    }

    private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd != IntPtr.Zero) RunDialogReplace();
    }

    private static async void RunDialogReplace()
    {
        if (!LegacyRunBoxExists()) return;
        MainWindow ??= new MainWindow();
        ActivateMainWindowAsync();
    }

    private static bool LegacyRunBoxExists()
    {
        var hWnd = Win32Helper.FindWindow("#32770", "Run");
        return hWnd != IntPtr.Zero && IsExplorerWindow(hWnd) && Win32Helper.PostMessage(hWnd, Win32Helper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static bool IsExplorerWindow(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out var processId);
        try { return Process.GetProcessById((int)processId).ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    private static void InitializeBackgroundWindow()
    {
        try
        {
            BackgroundWindow = new() { SystemBackdrop = new TransparentTintBackdrop(), IsMaximizable = false };
            BackgroundWindow.SetExtendedWindowStyle(ExtendedWindowStyle.ToolWindow);
            BackgroundWindow.SetWindowStyle(WindowStyle.Visible);
            BackgroundWindow.Activate();
            BackgroundWindow.MoveAndResize(0, 0, 0, 0);
            BackgroundWindow.Minimize();
            BackgroundWindow.SetWindowOpacity(0);
        }
        catch { }
    }

    private static void ActivateMainWindowAsync()
    {
        try { MainWindow?.Activate(); }
        catch { }
        MainWindow?.Show();
        MainWindow?.BringToFront();
    }

    private static bool IsStartupArgumentPresent() => Environment.GetCommandLineArgs().Skip(1).Contains("STARTUP");
}