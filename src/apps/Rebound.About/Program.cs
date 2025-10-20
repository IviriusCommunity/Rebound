using Rebound.Core.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using Windows.UI.Xaml;

namespace Rebound.About;

public partial class App : Application
{
    public static readonly List<IslandsWindow> _openWindows = [];

    public static void RegisterWindow(IslandsWindow window)
    {
        _openWindows.Add(window);
        window.Closed += (s, e) =>
        {
            _openWindows.Remove(window);
            if (_openWindows.Count == 0)
            {
                Current.Exit();
                Process.GetCurrentProcess().Kill();
            }
        };
    }
}

internal class Program
{
    private static readonly ConcurrentQueue<Func<Task>> _actions = new();
    private static readonly SemaphoreSlim _actionSignal = new(0);

    public static void QueueAction(Func<Task> action)
    {
        _actions.Enqueue(action);
        _actionSignal.Release(); // signal that a new action is available
    }

    [STAThread]
    static unsafe void Main(string[] args)
    {
        TerraFX.Interop.WinRT.WinRT.RoInitialize(
            TerraFX.Interop.WinRT.RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

        var app = new App();

        // Launch single-instance logic (blocking) on a background thread
        Task.Run(() => app._singleInstanceAppService.Launch(string.Join(" ", args)));

        MSG msg;
        while (true)
        {
            // Process all Windows messages in the queue without blocking
            while (TerraFX.Interop.Windows.Windows.PeekMessageW(&msg, HWND.NULL, 0, 0, PM.PM_REMOVE))
            {
                foreach (var window in App._openWindows.ToArray())
                {
                    if (!window._closed)
                    {
                        if (!window.PreTranslateMessage(&msg))
                        {
                            TerraFX.Interop.Windows.Windows.TranslateMessage(&msg);
                            TerraFX.Interop.Windows.Windows.DispatchMessageW(&msg);
                        }
                    }
                }
            }

            // Process queued actions if any
            var actionsToRun = new List<Func<Task>>();
            while (_actions.TryDequeue(out var action))
                actionsToRun.Add(action);

            foreach (var action in actionsToRun.ToArray()) // snapshot to avoid collection modified issues
            {
                try { _ = action(); }
                catch { /* log */ }
            }
        }
    }
}