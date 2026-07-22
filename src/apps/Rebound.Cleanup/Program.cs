// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Rebound.Core.Native.Wrappers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop.Windows;
using WinRT;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Cleanup;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        var res = SystemCompositionHack.TrySetSystemEngine();

        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
        });
    }
}

internal static unsafe partial class SystemCompositionHack
{
    private const int ExpectedState = 2;
    private const int SystemState = 1;
    private const int PageReadWrite = 0x04;
    private const long SwitcherStateRva = 0x2D5F80;
    private const long ExpectedImageSize = 3_204_920;
    private const string ExpectedFileVersion = "10.0.27200.1038 (WinBuild.160101.0800)";

    internal static bool TrySetSystemEngine()
    {
        bool initialResult = CompositionEngine.TrySetProcessEngine(CompositionEngineType.System);
        Log($"Initial TrySetProcessEngine(System): {initialResult}");
        if (initialResult)
        {
            return true;
        }

        if (!OperatingSystem.IsWindows() || IntPtr.Size != 8)
        {
            Log($"Unsupported process: Windows={OperatingSystem.IsWindows()}, PointerSize={IntPtr.Size}");
            return false;
        }

        using ManagedPtr<char> dcompiPtr = "dcompi.dll";

        var module = GetModuleHandleW(dcompiPtr);
        Log($"dcompi.dll module handle: 0x{module:X}");
        if (module == 0)
        {
            Log("dcompi.dll was not loaded");
            return false;
        }

        if (!IsExpectedModule(module))
        {
            Log("dcompi.dll validation failed");
            return false;
        }

        var stateAddress = module + (int)SwitcherStateRva;
        int currentState = *(int*)stateAddress;
        Log($"g_switcherState address: 0x{stateAddress:X}, value: {currentState}");
        if (currentState != ExpectedState)
        {
            Log($"Expected g_switcherState to be {ExpectedState}, found {currentState}");
            return false;
        }

        uint oldProtection;

        if (!VirtualProtect(&stateAddress, sizeof(int), PageReadWrite, &oldProtection))
        {
            Log($"VirtualProtect(write) failed: {Marshal.GetLastWin32Error()}");
            return false;
        }

        try
        {
            *(int*)stateAddress = SystemState;
            Log($"g_switcherState patched to {SystemState}");
        }
        finally
        {
            uint restoreError;

            bool restored = VirtualProtect(&stateAddress, sizeof(int), oldProtection, &restoreError);
            Log(restored
                ? "Original memory protection restored"
                : $"VirtualProtect(restore) failed: {restoreError}");
        }

        bool retryResult = CompositionEngine.TrySetProcessEngine(CompositionEngineType.System);
        Log($"Retry TrySetProcessEngine(System): {retryResult}");
        return retryResult;
    }

    private static bool IsExpectedModule(HMODULE module)
    {
        Span<char> pathBuffer = stackalloc char[32768];
        fixed (char* path = pathBuffer)
        {
            uint pathLength = GetModuleFileName(module, path, (uint)pathBuffer.Length);
            if (pathLength == 0 || pathLength >= pathBuffer.Length)
            {
                Log($"GetModuleFileName failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                string modulePath = new(path, 0, (int)pathLength);
                FileInfo file = new(modulePath);
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(file.FullName);
                bool matches = file.Length == ExpectedImageSize
                    && string.Equals(version.FileVersion, ExpectedFileVersion, StringComparison.Ordinal);
                Log($"dcompi.dll path: {modulePath}");
                Log($"dcompi.dll version: {version.FileVersion}");
                Log($"dcompi.dll size: {file.Length}; expected: {ExpectedImageSize}; matches: {matches}");
                return matches;
            }
            catch (IOException)
            {
                Log("dcompi.dll validation failed with an I/O error");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                Log("dcompi.dll validation failed with an access error");
                return false;
            }
            catch (ArgumentException)
            {
                Log("dcompi.dll validation failed with an argument error");
                return false;
            }
        }
    }

    private static void Log(string message)
    {
        string output = $"[SystemCompositionHack] {message}";
        Debug.WriteLine(output);
    }
}