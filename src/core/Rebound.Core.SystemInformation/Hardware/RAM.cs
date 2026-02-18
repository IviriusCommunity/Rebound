// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class RAM
{
    private const uint RPC_C_AUTHN_WINNT = 10;
    private const uint RPC_C_AUTHZ_NONE = 0;
    private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
    private const uint EOAC_NONE = 0;

    public static unsafe long GetPageFileSize()
    {
        HRESULT hr;
        long totalBytes = 0;
        ComPtr<IWbemLocator> pLocator = default;
        ComPtr<IWbemServices> pServices = default;
        ComPtr<IEnumWbemClassObject> pEnumerator = default;

        try
        {
            var clsid = CLSID.CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;
            hr = CoCreateInstance(&clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)pLocator.GetAddressOf());
            if (FAILED(hr)) return 0;

            fixed (char* pNamespace = "ROOT\\CIMV2")
            {
                hr = pLocator.Get()->ConnectServer(pNamespace, null, null, null, 0, null, null, pServices.GetAddressOf());
            }
            if (FAILED(hr)) return 0;

            hr = CoSetProxyBlanket((IUnknown*)pServices.Get(), RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                                   RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, null, EOAC_NONE);
            if (FAILED(hr)) return 0;

            fixed (char* pLanguage = "WQL")
            fixed (char* pQuery = "SELECT AllocatedBaseSize FROM Win32_PageFileUsage")
            {
                hr = pServices.Get()->ExecQuery(pLanguage, pQuery,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY | WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null, pEnumerator.GetAddressOf());
            }
            if (FAILED(hr)) return 0;

            IWbemClassObject* pObj = null;
            uint returned = 0;

            fixed (char* pProperty = "AllocatedBaseSize")
            {
                while (pEnumerator.Get()->Next((int)WBEM_TIMEOUT_TYPE.WBEM_INFINITE, 1, &pObj, &returned) == S.S_OK)
                {
                    VARIANT vt;
                    VariantInit(&vt);
                    pObj->Get(pProperty, 0, &vt, null, null);

                    // AllocatedBaseSize comes back as VT_I4 (MB)
                    if (vt.vt == (ushort)VARENUM.VT_I4)
                        totalBytes += (long)vt.Anonymous.Anonymous.Anonymous.lVal * 1024 * 1024;
                    else if (vt.vt == (ushort)VARENUM.VT_UI4)
                        totalBytes += (long)vt.Anonymous.Anonymous.Anonymous.ulVal * 1024 * 1024;

                    VariantClear(&vt);
                    pObj->Release();
                }
            }
        }
        catch
        {
            return 0;
        }
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Dispose();
            if (pServices.Get() is not null) pServices.Dispose();
            if (pLocator.Get() is not null) pLocator.Dispose();
        }

        return totalBytes;
    }

    public static unsafe long GetPageFileUsed()
    {
        HRESULT hr;
        long totalBytes = 0;
        ComPtr<IWbemLocator> pLocator = default;
        ComPtr<IWbemServices> pServices = default;
        ComPtr<IEnumWbemClassObject> pEnumerator = default;

        try
        {
            var clsid = CLSID.CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;
            hr = CoCreateInstance(&clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)pLocator.GetAddressOf());
            if (FAILED(hr)) return 0;

            fixed (char* pNamespace = "ROOT\\CIMV2")
            {
                hr = pLocator.Get()->ConnectServer(pNamespace, null, null, null, 0, null, null, pServices.GetAddressOf());
            }
            if (FAILED(hr)) return 0;

            hr = CoSetProxyBlanket((IUnknown*)pServices.Get(), RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                                   RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, null, EOAC_NONE);
            if (FAILED(hr)) return 0;

            fixed (char* pLanguage = "WQL")
            fixed (char* pQuery = "SELECT CurrentUsage FROM Win32_PageFileUsage")
            {
                hr = pServices.Get()->ExecQuery(pLanguage, pQuery,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY | WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null, pEnumerator.GetAddressOf());
            }
            if (FAILED(hr)) return 0;

            IWbemClassObject* pObj = null;
            uint returned = 0;

            fixed (char* pProperty = "CurrentUsage")
            {
                while (pEnumerator.Get()->Next((int)WBEM_TIMEOUT_TYPE.WBEM_INFINITE, 1, &pObj, &returned) == S.S_OK)
                {
                    VARIANT vt;
                    VariantInit(&vt);
                    pObj->Get(pProperty, 0, &vt, null, null);

                    // CurrentUsage comes back as VT_I4 (MB)
                    if (vt.vt == (ushort)VARENUM.VT_I4)
                        totalBytes += (long)vt.Anonymous.Anonymous.Anonymous.lVal * 1024 * 1024;
                    else if (vt.vt == (ushort)VARENUM.VT_UI4)
                        totalBytes += (long)vt.Anonymous.Anonymous.Anonymous.ulVal * 1024 * 1024;

                    VariantClear(&vt);
                    pObj->Release();
                }
            }
        }
        catch
        {
            return 0;
        }
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Dispose();
            if (pServices.Get() is not null) pServices.Dispose();
            if (pLocator.Get() is not null) pLocator.Dispose();
        }

        return totalBytes;
    }

    public static unsafe string GetTotalRam()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{roundedGb} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static unsafe string GetUsableRAM()
    {
        try
        {
            TerraFX.Interop.Windows.MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)sizeof(TerraFX.Interop.Windows.MEMORYSTATUSEX)
            };
            if (!TerraFX.Interop.Windows.Windows.GlobalMemoryStatusEx(&memStatus))
            {
                return $"Error: Failed to get memory info (Error code: {Marshal.GetLastWin32Error()})";
            }
            var totalBytes = memStatus.ullTotalPhys;
            var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

            // Round to nearest power of 2, or nearest multiple of 4 for larger sizes
            int roundedGb;
            if (totalGb <= 2)
                roundedGb = (int)Math.Pow(2, Math.Round(Math.Log2(totalGb)));
            else if (totalGb <= 16)
                roundedGb = (int)Math.Pow(2, Math.Ceiling(Math.Log2(totalGb)));
            else
                roundedGb = (int)Math.Round(totalGb / 8.0) * 8; // Round to nearest 8GB

            return $"{totalGb:F2} GB";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}