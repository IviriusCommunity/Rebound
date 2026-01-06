// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core.UI;

public unsafe class KeyboardHook : IDisposable
{
    private HHOOK _hookId;
    private readonly delegate* unmanaged<int, WPARAM, LPARAM, LRESULT> _hookProc;
    private bool _isHooked;

    public event EventHandler<KeyboardHookEventArgs>? KeyCombinationPressed;

    private readonly HashSet<int> _modifierKeys = new();
    private readonly HashSet<int> _targetKeys = new();
    private readonly Dictionary<int, bool> _keyStates = new();

    // Static dictionary to track all hook instances
    private static readonly Dictionary<nint, KeyboardHook> _instances = new();
    private static readonly object _lock = new();

    public class KeyboardHookEventArgs : EventArgs
    {
        public int VirtualKeyCode { get; set; }
        public bool IsKeyDown { get; set; }
        public HashSet<int> ActiveModifiers { get; set; } = new();
        public bool Handled { get; set; }
    }

    public KeyboardHook()
    {
        _hookProc = &HookCallbackStatic;
    }

    /// <summary>
    /// Adds a modifier key that must be pressed (e.g., VK_LWIN, VK_SHIFT, VK_CONTROL, VK_MENU)
    /// </summary>
    public void AddModifier(int virtualKeyCode)
    {
        _modifierKeys.Add(virtualKeyCode);
    }

    /// <summary>
    /// Adds a target key to monitor (e.g., VK_F23)
    /// </summary>
    public void AddTargetKey(int virtualKeyCode)
    {
        _targetKeys.Add(virtualKeyCode);
    }

    /// <summary>
    /// Configures the hook for Windows + Shift + F23
    /// </summary>
    public void ConfigureForWinShiftF23()
    {
        AddModifier(0x5B); // VK_LWIN
        AddModifier(0x5C); // VK_RWIN
        AddModifier(0xA0); // VK_LSHIFT
        AddModifier(0xA1); // VK_RSHIFT
        AddTargetKey(0x86); // VK_F23
    }

    /// <summary>
    /// Configures the hook for Ctrl + Alt + specific key
    /// </summary>
    public void ConfigureForAltTab()
    {
        AddModifier(0xA4); // VK_LMENU (Alt)
        AddModifier(0xA5); // VK_RMENU (Alt)
        AddModifier(0x09); // TAB
    }

    public void Install()
    {
        if (_isHooked) return;

        var hInstance = TerraFX.Interop.Windows.Windows.GetModuleHandleW(null);

        _hookId = TerraFX.Interop.Windows.Windows.SetWindowsHookExW(
            13, // WH_KEYBOARD_LL
            _hookProc,
            hInstance,
            0
        );

        if (_hookId.Value == null)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        lock (_lock)
        {
            _instances[(nint)_hookId.Value] = this;
        }

        _isHooked = true;
    }

    public void Uninstall()
    {
        if (!_isHooked) return;

        lock (_lock)
        {
            _instances.Remove((nint)_hookId.Value);
        }

        TerraFX.Interop.Windows.Windows.UnhookWindowsHookEx(_hookId);
        _isHooked = false;
    }

    [UnmanagedCallersOnly]
    private static LRESULT HookCallbackStatic(int nCode, WPARAM wParam, LPARAM lParam)
    {
        lock (_lock)
        {
            foreach (var instance in _instances.Values)
            {
                var result = instance.HookCallback(nCode, wParam, lParam);
                if (result.Value != 0)
                    return result;
            }
        }

        foreach (var instance in _instances.Values)
        {
            if (instance._isHooked)
            {
                return TerraFX.Interop.Windows.Windows.CallNextHookEx(instance._hookId, nCode, wParam, lParam);
            }
        }

        return new LRESULT(0);
    }

    private LRESULT HookCallback(int nCode, WPARAM wParam, LPARAM lParam)
    {
        if (nCode >= 0)
        {
            bool isKeyDown = wParam.Value == 0x0100 || wParam.Value == 0x0104; // WM_KEYDOWN or WM_SYSKEYDOWN

            var kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>((nint)lParam.Value);
            int vkCode = (int)kbStruct.vkCode;

            // Only process when a target key is pressed
            if (_targetKeys.Contains(vkCode) && isKeyDown)
            {
                // Check modifier states NOW using GetAsyncKeyState
                bool winPressed = IsKeyPressed(0x5B) || IsKeyPressed(0x5C); // VK_LWIN or VK_RWIN
                bool shiftPressed = IsKeyPressed(0xA0) || IsKeyPressed(0xA1); // VK_LSHIFT or VK_RSHIFT
                bool ctrlPressed = IsKeyPressed(0xA2) || IsKeyPressed(0xA3); // VK_LCONTROL or VK_RCONTROL
                bool altPressed = IsKeyPressed(0xA4) || IsKeyPressed(0xA5); // VK_LMENU or VK_RMENU

                // Check if required modifiers match
                bool needsWin = _modifierKeys.Overlaps(new[] { 0x5B, 0x5C });
                bool needsShift = _modifierKeys.Overlaps(new[] { 0xA0, 0xA1, 0x10 });
                bool needsCtrl = _modifierKeys.Overlaps(new[] { 0xA2, 0xA3, 0x11 });
                bool needsAlt = _modifierKeys.Overlaps(new[] { 0xA4, 0xA5, 0x12 });

                bool modifiersMatch = (!needsWin || winPressed) &&
                                      (!needsShift || shiftPressed) &&
                                      (!needsCtrl || ctrlPressed) &&
                                      (!needsAlt || altPressed);

                if (modifiersMatch)
                {
                    var activeModifiers = new HashSet<int>();
                    if (winPressed) { activeModifiers.Add(0x5B); }
                    if (shiftPressed) { activeModifiers.Add(0xA0); }
                    if (ctrlPressed) { activeModifiers.Add(0xA2); }
                    if (altPressed) { activeModifiers.Add(0xA4); }

                    var eventArgs = new KeyboardHookEventArgs
                    {
                        VirtualKeyCode = vkCode,
                        IsKeyDown = true,
                        ActiveModifiers = activeModifiers,
                        Handled = false
                    };

                    KeyCombinationPressed?.Invoke(this, eventArgs);

                    if (eventArgs.Handled)
                    {
                        return new LRESULT(1); // Block the key
                    }
                }
            }
        }

        return new LRESULT(0);
    }

    private bool IsKeyPressed(int vkCode)
    {
        return (TerraFX.Interop.Windows.Windows.GetAsyncKeyState(vkCode) & 0x8000) != 0;
    }

    private bool AreRequiredModifiersPressed()
    {
        if (_modifierKeys.Count == 0)
            return true; // No modifiers required

        // Check if at least one key from each modifier group is pressed
        var winKeys = new[] { 0x5B, 0x5C };
        var shiftKeys = new[] { 0xA0, 0xA1, 0x10 };
        var ctrlKeys = new[] { 0xA2, 0xA3, 0x11 };
        var altKeys = new[] { 0xA4, 0xA5, 0x12 };

        bool needsWin = _modifierKeys.Overlaps(winKeys);
        bool needsShift = _modifierKeys.Overlaps(shiftKeys);
        bool needsCtrl = _modifierKeys.Overlaps(ctrlKeys);
        bool needsAlt = _modifierKeys.Overlaps(altKeys);

        bool hasWin = !needsWin || IsAnyKeyPressed(winKeys);
        bool hasShift = !needsShift || IsAnyKeyPressed(shiftKeys);
        bool hasCtrl = !needsCtrl || IsAnyKeyPressed(ctrlKeys);
        bool hasAlt = !needsAlt || IsAnyKeyPressed(altKeys);

        return hasWin && hasShift && hasCtrl && hasAlt;
    }

    private bool IsAnyKeyPressed(int[] keys)
    {
        foreach (var key in keys)
        {
            if (_keyStates.TryGetValue(key, out bool isPressed) && isPressed)
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        Uninstall();
        GC.SuppressFinalize(this);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public nuint dwExtraInfo;
}