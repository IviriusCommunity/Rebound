// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using OwlCore.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.Shell;

namespace Rebound.Shell.ExperiencePack;

public class WindowDetectedEventArgs(IntPtr handle) : EventArgs
{
    public IntPtr Handle { get; private set; } = handle;
}

public unsafe class WindowHook
{
    private readonly string? ClassName;
    private readonly string? Name;
    private readonly string ProcessName;
    private HWINEVENTHOOK HookHandle { get; set; }
    private static readonly delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void> WinEventProcPtr = &StaticWinEventProc;

    private static readonly List<WindowHook> Instances = new(); // To associate events per instance

    public WindowHook(string? lpClassName, string? lpName, string lpProcessName)
    {
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = lpProcessName;

        Instances.Add(this); // Store this for callback use
        HookHandle = PInvoke.SetWinEventHook(
            EVENT_OBJECT_CREATE,
            EVENT_OBJECT_CREATE,
            HMODULE.Null,
            WinEventProcPtr,
            0,
            0,
            WINEVENT_OUTOFCONTEXT);

        Trigger(); // Initial check
    }

    public WindowHook(string? lpClassName, string? lpName) : this(lpClassName, lpName, string.Empty) { }

    public event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    private void Trigger()
    {
        HWND handle = PInvoke.FindWindow(ClassName, Name);
        if (handle == HWND.Null)
            return;

        if (!string.IsNullOrEmpty(ProcessName))
        {
            uint pid;
            PInvoke.GetWindowThreadProcessId(handle, &pid);
            try
            {
                if (Process.GetProcessById((int)pid).ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    WindowDetected?.Invoke(this, new(handle));
                }
            }
            catch
            {
                // Ignore process not found, etc.
            }
        }
        else
        {
            WindowDetected?.Invoke(this, new(handle));
        }
    }

    // Function pointer target must be static
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void StaticWinEventProc(
        HWINEVENTHOOK hWinEventHook,
        uint eventType,
        HWND hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        foreach (var instance in Instances)
        {
            instance.Trigger(); // In a real app, match hook to instance
        }
    }

    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint WINEVENT_OUTOFCONTEXT = 0;
}

/*public class ChildWindowHook
{
    private string? ClassName { get; set; }

    private string? Name { get; set; }

    private string ProcessName { get; set; }

    private HWND Parent { get; set; }

    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    private HWINEVENTHOOK HookHandle { get; }

    public ChildWindowHook(IntPtr parent, string? lpClassName, string? lpName, string lpProcessName)
    {
        Parent = new(parent);
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = lpProcessName;
        _winEventProc = WinEventProc;
        HookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
        Trigger();
    }

    public ChildWindowHook(IntPtr parent, string? lpClassName, string? lpName)
    {
        Parent = new(parent);
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = string.Empty;
        _winEventProc = WinEventProc;
        HookHandle = PInvoke.SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, HMODULE.Null, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
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
}*/