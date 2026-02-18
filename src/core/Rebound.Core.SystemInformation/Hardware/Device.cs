// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class Device
{
    private const uint RPC_C_AUTHN_WINNT = 10;
    private const uint RPC_C_AUTHZ_NONE = 0;
    private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
    private const uint EOAC_NONE = 0;

    public static unsafe string GetDeviceManufacturer()
    {
        HRESULT hr;
        string result = "Unknown";
        ComPtr<IWbemLocator> pLocator = default;
        ComPtr<IWbemServices> pServices = default;
        ComPtr<IEnumWbemClassObject> pEnumerator = default;

        try
        {
            var clsid = CLSID.CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;
            hr = CoCreateInstance(&clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)pLocator.GetAddressOf());
            if (FAILED(hr)) return result;

            fixed (char* pNamespace = "ROOT\\CIMV2")
                hr = pLocator.Get()->ConnectServer(pNamespace, null, null, null, 0, null, null, pServices.GetAddressOf());
            if (FAILED(hr)) return result;

            hr = CoSetProxyBlanket((IUnknown*)pServices.Get(), RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                                   RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, null, EOAC_NONE);
            if (FAILED(hr)) return result;

            fixed (char* pLanguage = "WQL")
            fixed (char* pQuery = "SELECT Manufacturer FROM Win32_ComputerSystem")
                hr = pServices.Get()->ExecQuery(pLanguage, pQuery,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY | WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null, pEnumerator.GetAddressOf());
            if (FAILED(hr)) return result;

            IWbemClassObject* pObj = null;
            uint returned = 0;

            fixed (char* pProperty = "Manufacturer")
            {
                if (pEnumerator.Get()->Next((int)WBEM_TIMEOUT_TYPE.WBEM_INFINITE, 1, &pObj, &returned) == S.S_OK)
                {
                    VARIANT vt;
                    VariantInit(&vt);
                    pObj->Get(pProperty, 0, &vt, null, null);
                    if (vt.vt == (ushort)VARENUM.VT_BSTR && vt.Anonymous.Anonymous.Anonymous.bstrVal != null)
                        result = new string(vt.Anonymous.Anonymous.Anonymous.bstrVal);
                    VariantClear(&vt);
                    pObj->Release();
                }
            }
        }
        catch { }
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Dispose();
            if (pServices.Get() is not null) pServices.Dispose();
            if (pLocator.Get() is not null) pLocator.Dispose();
        }

        return result;
    }

    public static string GetDeviceModel()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            if (key != null)
            {
                string? product = key.GetValue("SystemProductName") as string;
                string? family = key.GetValue("SystemFamily") as string;

                string? best = family is not null && !IsGenericOrSerial(family) ? family
                             : product is not null && !IsGenericOrSerial(product) ? product
                             : null;

                if (best is not null)
                    return best;
            }
        }
        catch { }

        return Native.WMI.QueryWmiString("SELECT Model FROM Win32_ComputerSystem", "Model")
            is string m && !IsGenericOrSerial(m) ? m :
               Native.WMI.QueryWmiString("SELECT Product FROM Win32_BaseBoard", "Product")
            is string b && !IsGenericOrSerial(b) ? b :
            "Unknown";
    }

    private static bool IsGenericOrSerial(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        return s.Length <= 6 && s.All(char.IsLetterOrDigit);
    }
}
