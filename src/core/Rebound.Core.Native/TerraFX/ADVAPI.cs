// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Rebound.Core.Native.TerraFX;

#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1028 // Enum Storage should be Int32
public enum SERVICE_START_TYPE : uint
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    SERVICE_BOOT_START = 0,
    SERVICE_SYSTEM_START = 1,
    SERVICE_AUTO_START = 2,
    SERVICE_DEMAND_START = 3,
    SERVICE_DISABLED = 4
}

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
public struct SERVICE_STATUS
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    public uint dwServiceType;
    public uint dwCurrentState;
    public uint dwControlsAccepted;
    public uint dwWin32ExitCode;
    public uint dwServiceSpecificExitCode;
    public uint dwCheckPoint;
    public uint dwWaitHint;
}

public static partial class Windows
{
    public const uint SC_MANAGER_CONNECT = 0x0001;
    
    public const uint SERVICE_QUERY_STATUS = 0x0004;
    public const uint SERVICE_START = 0x0010;
    public const uint SERVICE_STOP = 0x0020;
    public const uint SERVICE_CHANGE_CONFIG = 0x0002;
    
    public const uint SERVICE_CONTROL_STOP = 0x00000001;
    
    public const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW")]
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static unsafe partial nint OpenSCManagerW(
        char* lpMachineName,
        char* lpDatabaseName,
        uint dwDesiredAccess);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW")]
    public static unsafe partial nint OpenServiceW(
#pragma warning restore CA1401 // P/Invokes should not be visible
        nint hSCManager,
        char* lpServiceName,
        uint dwDesiredAccess);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfigW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ChangeServiceConfigW(
        nint hService,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        char* lpBinaryPathName,
        char* lpLoadOrderGroup,
        uint* lpdwTagId,
        char* lpDependencies,
        char* lpServiceStartName,
        char* lpPassword,
        char* lpDisplayName);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "ControlService")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ControlService(
        nint hService,
        uint dwControl,
        SERVICE_STATUS* lpServiceStatus);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "StartServiceW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool StartServiceW(
        nint hService,
        uint dwNumServiceArgs,
        char** lpServiceArgVectors);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseServiceHandle(
        nint hSCObject);
}
#pragma warning restore CA1707 // Identifiers should not contain underscores