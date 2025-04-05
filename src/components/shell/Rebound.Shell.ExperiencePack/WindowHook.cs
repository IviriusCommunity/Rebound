using System;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rebound.ShellExperiencePack;

public class WindowDetectedEventArgs(IntPtr handle) : EventArgs
{
    public IntPtr Handle { get; private set; } = handle;
}

public class WindowHook
{
    private string? ClassName { get; set; }

    private string? Name { get; set; }

    private string ProcessName { get; set; }

    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    private HWINEVENTHOOK _hookHandle;

    public WindowHook(string? lpClassName, string? lpName, string lpProcessName)
    {
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = lpProcessName;
        _winEventProc = WinEventProc;
        _hookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        Trigger();
    }

    public WindowHook(string? lpClassName, string? lpName)
    {
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = string.Empty;
        _winEventProc = WinEventProc;
        _hookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        Trigger();
    }

    public event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    private readonly WINEVENTPROC _winEventProc;

    private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) => Trigger();

    private unsafe void Trigger()
    {
        var handle = PInvoke.FindWindow(ClassName, Name);
        if (handle != HWND.Null)
        {
            if (!string.IsNullOrEmpty(ProcessName))
            {
                uint lpdwProcessId;
                _ = PInvoke.GetWindowThreadProcessId(handle, &lpdwProcessId);
                if (Process.GetProcessById((int)lpdwProcessId).ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    WindowDetected?.Invoke(this, new(handle));
                }
            }
            else
            {
                WindowDetected?.Invoke(this, new(handle));
            }
        }
    }
}

public class ChildWindowHook
{
    private string? ClassName { get; set; }

    private string? Name { get; set; }

    private string ProcessName { get; set; }

    private HWND Parent { get; set; }

    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    private HWINEVENTHOOK _hookHandle;

    public ChildWindowHook(IntPtr parent, string? lpClassName, string? lpName, string lpProcessName)
    {
        Parent = new(parent);
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = lpProcessName;
        _winEventProc = WinEventProc;
        _hookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        Trigger();
    }

    public ChildWindowHook(IntPtr parent, string? lpClassName, string? lpName)
    {
        Parent = new(parent);
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = string.Empty;
        _winEventProc = WinEventProc;
        _hookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        Trigger();
    }

    public event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    private readonly WINEVENTPROC _winEventProc;

    private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) => Trigger();

    private unsafe void Trigger()
    {
        var handle = PInvoke.FindWindowEx(Parent, HWND.Null, ClassName, Name);
        if (handle != HWND.Null)
        {
            if (!string.IsNullOrEmpty(ProcessName))
            {
                uint lpdwProcessId;
                _ = PInvoke.GetWindowThreadProcessId(handle, &lpdwProcessId);
                if (Process.GetProcessById((int)lpdwProcessId).ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    WindowDetected?.Invoke(this, new(handle));
                }
            }
            else
            {
                WindowDetected?.Invoke(this, new(handle));
            }
        }
    }
}