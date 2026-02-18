// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;
using HRESULT = TerraFX.Interop.Windows.HRESULT;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Rebound.Core.SystemInformation.Software;

public enum WindowsActivationType
{
    Unlicensed,
    Activated,
    GracePeriod,
    NonGenuine,
    ExtendedGracePeriod,
    Unknown
}

public static class WindowsInformation
{
    /// <summary>
    /// Checks if the Server Shutdown Reason UI is enabled by reading the appropriate registry keys.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether the Server Shutdown Reason UI is enabled. It first checks the policy key,
    /// and if not set, falls back to the general reliability key.
    /// </returns>
    public static bool IsServerShutdownUIEnabled()
    {
        const string policyKey = @"SOFTWARE\Policies\Microsoft\Windows NT\Reliability";
        const string fallbackKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability";
        const string valueNamePolicy = "ShutdownReasonUI";
        const string valueNameFallback = "ShutdownReasonOn";

        int? policyValue = null;
        int? fallbackValue = null;

        using (var key = Registry.LocalMachine.OpenSubKey(policyKey))
            policyValue = key?.GetValue(valueNamePolicy) as int?;

        if (policyValue.HasValue)
            return policyValue.Value != 0;

        using (var key = Registry.LocalMachine.OpenSubKey(fallbackKey))
            fallbackValue = key?.GetValue(valueNameFallback) as int?;

        return fallbackValue.HasValue && fallbackValue.Value != 0;
    }

    private static unsafe char* ToPointer(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return valueCharPtr;
        }
    }

    // COM CLSIDs / IIDs
    // CLSID_WbemLocator = {4590F811-1D3A-11D0-891F-00AA004B2E24}
    static readonly Guid CLSID_WbemLocator = new("4590F811-1D3A-11D0-891F-00AA004B2E24");

    // HRESULT
    const int S_OK = 0;

    // WBEM flags (wbemcli.h)
    const int WBEM_FLAG_RETURN_IMMEDIATELY = 0x10;
    const int WBEM_FLAG_FORWARD_ONLY = 0x20;
    const uint WBEM_INFINITE = 0xFFFFFFFF;

    // RPC authentication constants (rpcdce.h)
    const uint RPC_C_AUTHN_WINNT = 10;
    const uint RPC_C_AUTHZ_NONE = 0;
    const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;

    // EOAC flags (objidlbase.h)
    const uint EOAC_NONE = 0x0;

    /// <summary>
    /// Gets the computer name of the current machine using Environment.MachineName, which retrieves the NetBIOS name of the local computer.
    /// </summary>
    /// <returns>
    /// A string representing the computer name. This is typically the hostname assigned to the machine, 
    /// and it may differ from the fully qualified domain name (FQDN) if the machine is part of a domain.
    /// </returns>
#pragma warning disable CA1024 // Use properties where appropriate
    public static string GetComputerName() => Environment.MachineName;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>
    /// Retrieves the installation date of the current Windows operating system by reading the "InstallDate" 
    /// value from the registry key "SOFTWARE\Microsoft\Windows NT\CurrentVersion".
    /// </summary>
    /// <returns>
    /// A DateTime object representing the installation date of Windows. If the registry key or value is not found,
    /// it returns DateTime.MinValue.
    /// </returns>
    public static unsafe DateTime GetInstalledOnDate()
    {
        const string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        const string valueName = "InstallDate";

        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
        if (key != null)
        {
            var installDateObj = key.GetValue(valueName);
            if (installDateObj is int installDateInt)
            {
                // Convert Unix timestamp to DateTime
                DateTime installDate = DateTimeOffset.FromUnixTimeSeconds(installDateInt).DateTime;
                return installDate;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        else
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Retrieves the system uptime by using Environment.TickCount64, which returns the number of 
    /// milliseconds that have elapsed since the system started.
    /// </summary>
    /// <returns>
    /// A TimeSpan object representing the duration of time that has passed since the system was last
    /// started. This value is derived from Environment.TickCount64,
    /// </returns>
    public static TimeSpan GetUptime()
        => TimeSpan.FromMilliseconds(Environment.TickCount64);

    /// <summary>
    /// Retrieves the system uptime as a formatted string by calling <see cref="GetUptime"/> and 
    /// formatting the resulting TimeSpan into a human-readable format of days, hours, and minutes.
    /// </summary>
    /// <returns>
    /// A string representing the system uptime in the format of "Xd Yh Zm", where X is the number of 
    /// days, Y is the number of hours, and Z is the number of minutes.
    /// </returns>
    public static string GetUptimeString()
    {
        var uptime = GetUptime();
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }

    /// <summary>
    /// Retrieves the current system locale by accessing CultureInfo.CurrentCulture.DisplayName, 
    /// which provides a human-readable name for the current culture settings of the system.
    /// </summary>
    /// <returns>
    /// A string representing the current system locale in a human-readable format (e.g., "English (United States)",
    /// "French (France)", etc.). This value reflects the culture settings of the operating system 
    /// and may influence how dates, times, numbers, and other culture-specific information are formatted and displayed.
    /// </returns>
    public static string GetLocale()
        => CultureInfo.CurrentCulture.DisplayName;

    /// <summary>
    /// Retrieves the local IP address of the machine by creating a UDP socket and connecting to a dummy IP address.
    /// </summary>
    /// <returns>
    /// A string representing the local IP address of the machine.
    /// </returns>
    public static string GetLocalIP()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "Unknown";
        }
        catch { return "Unknown"; }
    }

    /// <summary>
    /// Retrieves the CPU architecture of the current process using RuntimeInformation.ProcessArchitecture 
    /// and maps it to a human-readable string.
    /// </summary>
    /// <returns>
    /// A string representing the CPU architecture of the current process. Possible values include "x64", "x86",
    /// "ARM64", "ARM", "WASM", "S390x", "LoongArch64", "ARMv6", "PPC64LE", or "Unknown" if the architecture cannot be determined.
    /// </returns>
    public static string GetCPUArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "ARM64",
            Architecture.Arm => "ARM",
            Architecture.Wasm => "WASM",
            Architecture.S390x => "S390x",
            Architecture.LoongArch64 => "LoongArch64",
            Architecture.Armv6 => "ARMv6",
            Architecture.Ppc64le => "PPC64LE",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Retrieves the installation date of Windows as a formatted string by calling <see cref="GetInstalledOnDate"/>.
    /// </summary>
    /// <returns>
    /// A string representing the installation date of Windows in "yyyy-MM-dd" format.
    /// If the installation date cannot be determined, it returns "Unknown".
    /// </returns>
    public static string GetInstalledOnDateString()
    {
        var installDate = GetInstalledOnDate();
        return installDate == DateTime.MinValue ? "Unknown" : installDate.ToString((IFormatProvider?)null);
    }

    /// <summary>
    /// Retrieves the Windows activation status by querying WMI for the "LicenseStatus" property of the "SoftwareLicensingProduct" class.
    /// </summary>
    /// <returns>
    /// A WindowsActivationType enum value indicating the activation status of the current Windows installation.
    /// </returns>
    public static unsafe WindowsActivationType GetWindowsActivationType()
    {
        HRESULT hr;
        WindowsActivationType result = WindowsActivationType.Unknown;

        ComPtr<IWbemLocator> pLocator = null;
        ComPtr<IWbemServices> pServices = null;
        ComPtr<IEnumWbemClassObject> pEnumerator = null;

        try
        {
            var clsid = CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;

            // Create IWbemLocator
            hr = CoCreateInstance(
                &clsid,          // Correct TerraFX CLSID
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                &iid,
                (void**)&pLocator
            );
            if (FAILED(hr))
                throw new Exception($"CoCreateInstance failed: 0x{hr.Value:X8}");

            // Connect to WMI namespace ROOT\CIMV2
            hr = pLocator.Get()->ConnectServer(
                "ROOT\\CIMV2".ToPointer(),
                null,
                null,
                null,
                0,
                null,
                null,
                pServices.GetAddressOf()
            );
            if (FAILED(hr))
                throw new Exception($"ConnectServer failed: 0x{hr.Value:X8}");

            // Set security levels on the proxy
            hr = CoSetProxyBlanket(
                (IUnknown*)pServices.Get(),
                (uint)RPC_C_AUTHN_WINNT,
                (uint)RPC_C_AUTHZ_NONE,
                null,
                (uint)RPC_C_AUTHN_LEVEL_CALL,
                (uint)RPC_C_IMP_LEVEL_IMPERSONATE,
                null,
                (uint)EOAC_NONE
            );
            if (FAILED(hr))
                throw new Exception($"CoSetProxyBlanket failed: 0x{hr.Value:X8}");

            // Query for SoftwareLicensingProduct entries
            hr = pServices.Get()->ExecQuery(
                "WQL".ToPointer(),
                "SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL".ToPointer(),
                WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY,
                null,
                pEnumerator.GetAddressOf()
            );
            if (FAILED(hr))
                throw new Exception($"ExecQuery failed: 0x{hr.Value:X8}");

            // Enumerate results
            IWbemClassObject* pObj = null;
            uint returned = 0;

            while (pEnumerator.Get()->Next(unchecked((int)WBEM_INFINITE), 1, &pObj, &returned) == S_OK && returned != 0)
            {
                VARIANT vtStatus;
                VariantInit(&vtStatus);

                pObj->Get("LicenseStatus".ToPointer(), 0, &vtStatus, null, null);
                uint status = vtStatus.uintVal;
                VariantClear(&vtStatus);
                pObj->Release();

                result = status switch
                {
                    0 => WindowsActivationType.Unlicensed,
                    1 => WindowsActivationType.Activated,
                    2 => WindowsActivationType.GracePeriod,
                    3 => WindowsActivationType.NonGenuine,
                    4 => WindowsActivationType.ExtendedGracePeriod,
                    _ => WindowsActivationType.Unknown
                };

                break; // only first valid entry needed
            }
        }
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Get()->Release();
            if (pServices.Get() is not null) pServices.Get()->Release();
            if (pLocator.Get() is not null) pLocator.Get()->Release();
            CoUninitialize();
        }

        return result;
    }

    /// <summary>
    /// Retrieves the drive letter of the Windows installation by getting the system directory path and extracting the root drive from it.
    /// </summary>
    /// <returns>
    /// A string representing the drive letter of the Windows installation (e.g., "C:\"). If the system directory path cannot be determined, it defaults to "C:\".
    /// </returns>
    public static string GetWindowsInstallationDrivePath()
    {
        // Get the system directory path
        var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        // Extract the drive letter
        var driveLetter = Path.GetPathRoot(systemPath);
        return driveLetter ??= "C:\\";
    }

    /// <summary>
    /// Gets the display name of the current Windows installation.
    /// </summary>
    /// <returns>Windows 10, Windows 11, or Windows Server.</returns>
    public static string GetOSDisplayName()
    {
        return GetOSName().Contains("10", StringComparison.InvariantCultureIgnoreCase) ?
            "Windows 10" :
            GetOSName().Contains("Server", StringComparison.InvariantCultureIgnoreCase) ?
            "Windows Server" :
            "Windows 11";
    }

    /// <summary>
    /// Gets the official name of the current Windows operating system.
    /// </summary>
    /// <returns>Windows 10 Home, Windows 11 Pro, etc.</returns>
    public static string GetOSName()
    {
        string regPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        using var key = Registry.LocalMachine.OpenSubKey(regPath);
        if (key != null)
        {
            var windowsVersionTitle = key.GetValue("ProductName")?.ToString();
            var isWindows11 = int.Parse(key.GetValue("CurrentBuildNumber")?.ToString()!, null) >= 22000;

            return isWindows11
                ? windowsVersionTitle?.Replace("10", "11", StringComparison.InvariantCultureIgnoreCase)!
                : windowsVersionTitle ?? "Unknown";
        }

        return "Unknown";
    }

    /// <summary>
    /// Retrieves the registered owner and organization of the Windows license from the registry. It reads the "RegisteredOwner" and "RegisteredOrganization" 
    /// values from the "SOFTWARE\Microsoft\Windows NT\CurrentVersion" registry key and combines them into a single string.
    /// </summary>
    /// <returns>
    /// A string containing the registered owner and organization of the Windows license, formatted as "Owner, Organization". If the organization is not specified, 
    /// it returns just the owner. If the registry keys are not found, it returns "UnknownLicenseHolders".
    /// </returns>
    public static string GetLicenseOwners()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            // Retrieve current username
            var owner = key.GetValue("RegisteredOwner", "Unknown") as string;
            var owner2 = key.GetValue("RegisteredOrganization", "") as string;

            return owner + (string.IsNullOrEmpty(owner2) ? string.Empty : (", " + owner2));
        }
        return "UnknownLicenseHolders";
    }

    /// <summary>
    /// Gets the current Windows display version.
    /// </summary>
    /// <returns>24H2, 25H2, etc.</returns>
    public static string GetDisplayVersion()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("DisplayVersion", "Unknown").ToString()!;
        }
        return "Unknown";
    }

    /// <summary>
    /// Retrieves the current build number of the Windows operating system by reading the "CurrentBuildNumber" value from the 
    /// registry key "SOFTWARE\Microsoft\Windows NT\CurrentVersion".
    /// </summary>
    /// <returns>
    /// A string representing the current build number of Windows. If the registry key or value is not found, it returns "Unknown".
    /// </returns>
    public static string GetCurrentBuildNumber()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("CurrentBuildNumber", "Unknown").ToString()!;
        }
        return "Unknown";
    }

    /// <summary>
    /// Retrieves the Update Build Revision (UBR) number of the current Windows installation by reading the "UBR" value from the registry key
    /// </summary>
    /// <returns>
    /// A string representing the UBR number. If the registry key or value is not found, it returns "Unknown".
    /// </returns>
    public static string GetUBR()
    {
        // Open the registry key
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key != null)
        {
            return key.GetValue("UBR", "Unknown").ToString()!;
        }
        return "Unknown";
    }
}
