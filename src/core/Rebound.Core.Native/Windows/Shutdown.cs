// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native.Windows;

/// <summary>
/// Predefined set of reasons for shutdown operations. Reserved for servers.
/// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
public enum ShutdownReason : uint
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    Other = SHTDN.SHTDN_REASON_MAJOR_OTHER,
    Hardware = SHTDN.SHTDN_REASON_MAJOR_HARDWARE,
    OperatingSystem = SHTDN.SHTDN_REASON_MAJOR_OPERATINGSYSTEM,
    Software = SHTDN.SHTDN_REASON_MAJOR_SOFTWARE,
    Application = SHTDN.SHTDN_REASON_MAJOR_APPLICATION,
    System = SHTDN.SHTDN_REASON_MAJOR_SYSTEM,
    Power = SHTDN.SHTDN_REASON_MAJOR_POWER,
    LegacyApi = SHTDN.SHTDN_REASON_LEGACY_API,

    // Flags
    Planned = SHTDN.SHTDN_REASON_FLAG_PLANNED,
    UserDefined = SHTDN.SHTDN_REASON_FLAG_USER_DEFINED,

    // Minor reasons
#pragma warning disable CA1069 // Enums values should not be duplicated
    MinorOther = SHTDN.SHTDN_REASON_MINOR_OTHER, // Not my fault this is just Windows being Windows
#pragma warning restore CA1069 // Enums values should not be duplicated
    MinorMaintenance = SHTDN.SHTDN_REASON_MINOR_MAINTENANCE,
    MinorInstallation = SHTDN.SHTDN_REASON_MINOR_INSTALLATION,
    MinorUpgrade = SHTDN.SHTDN_REASON_MINOR_UPGRADE,
    MinorReconfig = SHTDN.SHTDN_REASON_MINOR_RECONFIG,
    MinorHung = SHTDN.SHTDN_REASON_MINOR_HUNG,
    MinorUnstable = SHTDN.SHTDN_REASON_MINOR_UNSTABLE,
    MinorBluescreen = SHTDN.SHTDN_REASON_MINOR_BLUESCREEN,
    MinorServicePack = SHTDN.SHTDN_REASON_MINOR_SERVICEPACK,
    MinorHotfix = SHTDN.SHTDN_REASON_MINOR_HOTFIX,
    MinorSecurityFix = SHTDN.SHTDN_REASON_MINOR_SECURITYFIX,
    MinorSecurity = SHTDN.SHTDN_REASON_MINOR_SECURITY,
    MinorNetworkConnectivity = SHTDN.SHTDN_REASON_MINOR_NETWORK_CONNECTIVITY,
    MinorSystemRestore = SHTDN.SHTDN_REASON_MINOR_SYSTEMRESTORE,
}

public static class Shutdown
{
    /// <param name="message">
    /// Additional note on why the operation took place.
    /// </param>
    /// <param name="reason">
    /// The reason for the shutdown operation.
    /// </param>
    /// <param name="force">
    /// <see langword="true"/> to forcefully close every open app. <see langword="false"/> to inform the user that there are some apps left open.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the operation succeeded. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool ServerShutdownNow(string message, ShutdownReason? reason, bool force = false)
        => ShutdownAdvanced(message: message, reason: reason, forceCloseApps: force);

    /// <param name="message">
    /// Additional note on why the operation took place.
    /// </param>
    /// <param name="reason">
    /// The reason for the shutdown operation.
    /// </param>
    /// <param name="force">
    /// <see langword="true"/> to forcefully close every open app. <see langword="false"/> to inform the user that there are some apps left open.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the operation succeeded. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool ServerRestartNow(string message, ShutdownReason? reason, bool force = false)
        => ShutdownAdvanced(message: message, reason: reason, restart: true, forceCloseApps: force);

    /// <param name="force">
    /// <see langword="true"/> to forcefully close every open app. <see langword="false"/> to inform the user that there are some apps left open.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the operation succeeded. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool ShutdownNow(bool force = false)
        => ShutdownAdvanced(forceCloseApps: force);

    /// <param name="force">
    /// <see langword="true"/> to forcefully close every open app. <see langword="false"/> to inform the user that there are some apps left open.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the operation succeeded. Otherwise <see langword="false"/>.
    /// </returns>
    public static bool RestartNow(bool force = false)
        => ShutdownAdvanced(restart: true, forceCloseApps: force);

    /// <summary>
    /// Triggers a power operation (shutdown/restart). Requires administrator privileges.
    /// </summary>
    /// <param name="machineName">
    /// The name of the computer to apply the operation to. Leave empty to use the current computer's name.
    /// </param>
    /// <param name="message">
    /// Additional note on why the operation took place. Reserved for servers.
    /// </param>
    /// <param name="timeout">
    /// The timeout in seconds until the operation takes place.
    /// </param>
    /// <param name="forceCloseApps">
    /// <see langword="true"/> to forcefully close every open app. <see langword="false"/> to inform the user that there are some apps left open.
    /// </param>
    /// <param name="restart">
    /// <see langword="true"/> to boot again into Windows after shutting down. <see langword="false"/> for a permanent shutdown.
    /// </param>
    /// <param name="reason">
    /// The reason for the shutdown operation. Reserved for servers.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the operation succeeded. Otherwise <see langword="false"/>.
    /// </returns>
    public static unsafe bool ShutdownAdvanced(string? machineName = null, string? message = null, uint timeout = 0, bool forceCloseApps = false, bool restart = false, ShutdownReason? reason = null)
    {
        using ManagedPtr<char> seShutdownPrivilege = "SeShutdownPrivilege";
        using ManagedPtr<char> pMachineName = machineName ?? string.Empty;
        using ManagedPtr<char> pMessage = message ?? string.Empty;

        HANDLE hToken = HANDLE.NULL;

        if (OpenProcessToken(
            GetCurrentProcess(),
            TOKEN.TOKEN_ADJUST_PRIVILEGES | TOKEN.TOKEN_QUERY,
            &hToken) == BOOL.FALSE)
            return false;

        TOKEN_PRIVILEGES tp = default;
        LUID luid;

        if (LookupPrivilegeValueW(null, seShutdownPrivilege, &luid) == BOOL.FALSE)
        {
            CloseHandle(hToken);
            return false;
        }

        tp.PrivilegeCount = 1;
        tp.Privileges[0].Luid = luid;
        tp.Privileges[0].Attributes = SE.SE_PRIVILEGE_ENABLED;

        AdjustTokenPrivileges(hToken, BOOL.FALSE, &tp, 0, null, null);
        CloseHandle(hToken);

        return InitiateSystemShutdownExW(
            machineName == null ? (char*)null : pMachineName,
            message == null ? (char*)null : pMessage,
            timeout,
            forceCloseApps,
            restart,
            (uint)(reason ?? 0)
        ) != BOOL.FALSE;
    }
}