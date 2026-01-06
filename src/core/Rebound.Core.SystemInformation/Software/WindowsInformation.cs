// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
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

    public static string GetComputerName()
    {
        return Environment.MachineName;
    }

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
