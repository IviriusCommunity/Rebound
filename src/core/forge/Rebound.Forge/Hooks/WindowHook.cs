// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Rebound.Forge.Hooks;

public abstract class Win32Control
{
    public abstract string ClassName { get; }
}

public class ButtonControl : Win32Control
{
    public override string ClassName => "Button";
}

public class ComboBoxControl : Win32Control
{
    public override string ClassName => "ComboBox";
}

public class StaticControl : Win32Control
{
    public override string ClassName => "Static";
}
public class WindowDetectedEventArgs(IntPtr handle) : EventArgs
{
    public IntPtr Handle { get; private set; } = handle;
}

public unsafe class WindowHook
{
    private readonly string? ClassName;
    private readonly string? Name;
    private readonly string ProcessName;

    private readonly List<Win32Control> ExpectedChildren;
    private HWINEVENTHOOK HookHandle { get; set; }

    private static readonly List<WindowHook> Instances = new();

    public WindowHook(string? lpClassName, string? lpName, string lpProcessName, List<Win32Control> expectedChildren)
    {
        ClassName = lpClassName;
        Name = lpName;
        ProcessName = lpProcessName;
        ExpectedChildren = expectedChildren;

        Instances.Add(this);
        HookHandle = PInvoke.SetWinEventHook(
            EVENT_OBJECT_SHOW,
            EVENT_OBJECT_SHOW,
            HMODULE.Null,
            &StaticWinEventProc,
            0,
            0,
            WINEVENT_OUTOFCONTEXT);

        Trigger();
    }

    public event EventHandler<WindowDetectedEventArgs>? WindowDetected;

    private void Trigger()
    {
        HWND handle = PInvoke.FindWindow(ClassName, Name);
        if (handle == HWND.Null) return;

        if (!string.IsNullOrEmpty(ProcessName))
        {
            uint pid = 0;
            _ = PInvoke.GetWindowThreadProcessId(handle, &pid);
            try
            {
                if (!Process.GetProcessById((int)pid).ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            catch { return; }
        }

        // Enumerate children and check types
        if (!MatchesExpectedChildren(handle)) return;

        WindowDetected?.Invoke(this, new(handle));
    }
    private static List<string>? s_children;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static BOOL EnumChildCallbackWithParam(HWND hwnd, LPARAM lParam)
    {
        Span<char> buffer = stackalloc char[256];
        _ = PInvoke.GetClassName(hwnd, buffer);

        var list = (List<string>)GCHandle.FromIntPtr(new IntPtr(lParam)).Target!;
        list.Add(new string(buffer.TrimEnd('\0')));

        return true;
    }

    private unsafe bool MatchesExpectedChildren(HWND hwnd)
    {
        var children = new List<string>();
        GCHandle handle = GCHandle.Alloc(children);
        IntPtr ptr = (IntPtr)handle;

        delegate* unmanaged[Stdcall]<HWND, LPARAM, BOOL> callback = &EnumChildCallbackWithParam;
        PInvoke.EnumChildWindows(hwnd, callback, new LPARAM(ptr));

        handle.Free();

        // Count expected types
        var expectedCounts = new Dictionary<string, int>();
        foreach (var c in ExpectedChildren)
        {
            if (!expectedCounts.ContainsKey(c.ClassName))
                expectedCounts[c.ClassName] = 0;
            expectedCounts[c.ClassName]++;
        }

        // Count actual types
        var actualCounts = new Dictionary<string, int>();
        foreach (var cls in children)
        {
            if (!actualCounts.ContainsKey(cls)) actualCounts[cls] = 0;
            actualCounts[cls]++;
        }

        // Check: each expected type must appear at least as many times as expected
        foreach (var kv in expectedCounts)
        {
            if (!actualCounts.TryGetValue(kv.Key, out int count) || count < kv.Value)
                return false; // missing expected control
        }

        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void StaticWinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        foreach (var instance in Instances)
        {
            instance.Trigger();
        }
    }

    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint WINEVENT_OUTOFCONTEXT = 0;
}