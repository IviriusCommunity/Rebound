// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native.Helpers;

/// <summary>
/// Wraps the Windows Restart Manager API to find which processes are locking a file.
/// </summary>
public static class RestartManagerHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public uint dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;

        public fixed char strAppName[256];
        public fixed char strServiceShortName[64];

        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;

        public int bRestartable;
    }

    public static IEnumerable<Process> GetLockingProcesses(string filePath)
    {
        var processes = GetLockingProcessesInternal(filePath);
        foreach (var proc in processes)
        {
            yield return proc;
        }
    }

    private static unsafe List<Process> GetLockingProcessesInternal(string filePath)
    {
        var foundProcesses = new List<Process>();

        // The variables
        using ManagedPtr<char> sessionKey = Guid.NewGuid().ToString();
        using ManagedPtr<char> resourcePath = filePath;
        uint sessionHandle;

        // Start the Restart Manager session
        HRESULT hr = (HRESULT)RmStartSession(&sessionHandle, 0, sessionKey);

        // Error handling
        if (FAILED(hr))
            throw new InvalidOperationException($"RmStartSession failed: 0x{hr:X8}");

        try
        {
            // Gotta point to the pointer (Win32 API is weird)
            char* pResource = (char*)resourcePath.ObjectPointer;

            // Register the resource (file) with the Restart Manager session
            hr = (HRESULT)RmRegisterResources(sessionHandle, 1, &pResource, 0, null, 0, null);

            // Error handling
            if (FAILED(hr))
                throw new InvalidOperationException($"RmRegisterResources failed: 0x{hr:X8}");

            // More variables
            uint pnProcInfoNeeded = 0, pnProcInfo = 0, lpdwRebootReasons = 0;

            // Get the list of processes locking the file (first call to get the count)
            hr = (HRESULT)RmGetList(
                sessionHandle, 
                &pnProcInfoNeeded, 
                &pnProcInfo, 
                null, 
                &lpdwRebootReasons);

            // If no processes are locking the file or if there was an error, return the empty list
#pragma warning disable CA1508 // Avoid dead conditional code (Roslyn doesn't understand pointers)
            if (pnProcInfoNeeded == 0 || FAILED(hr)) 
                return foundProcesses;
#pragma warning restore CA1508

            // Allocate an array to hold the process info
            using ManagedArrayPtr<RM_PROCESS_INFO> processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
            pnProcInfo = pnProcInfoNeeded;

            // Get the list of processes locking the file (second call to get the actual data)
            hr = (HRESULT)RmGetList(
                sessionHandle, 
                &pnProcInfoNeeded, 
                &pnProcInfo,
                (TerraFX.Interop.Windows.RM_PROCESS_INFO*)processInfo.ObjectPointer, 
                &lpdwRebootReasons);

            // Error handling
            if (FAILED(hr))
                throw new InvalidOperationException($"RmGetList failed: 0x{hr:X8}");

            // Convert the process IDs to Process objects and add them to the list
            for (int i = 0; i < pnProcInfo; i++)
            {
                foundProcesses.Add(Process.GetProcessById((int)processInfo[i].Process.dwProcessId));
            }
        }
        finally
        {
            _ = RmEndSession(sessionHandle);
        }
        return foundProcesses;
    }
}