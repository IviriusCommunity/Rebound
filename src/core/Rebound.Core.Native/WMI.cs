// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Globalization;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native;

/// <summary>
/// Manages a lazy, auto-recovering WMI connection to a given namespace.
/// Call <see cref="ExecuteWmiQuery"/> to run WQL and iterate results;
/// the connection is (re-)established transparently on demand.
/// Thread-safe - concurrent callers are serialized internally.
/// </summary>
/// <param name="wmiNamespace">
/// The WMI namespace to connect to, e.g. <c>"ROOT\\CIMV2"</c>.
/// </param>
public sealed unsafe class WmiConnection(string wmiNamespace = "ROOT\\CIMV2")
{
    /// <summary>
    /// A shared, lazily-initialized instance connected to <c>ROOT\CIMV2</c>.
    /// Suitable for use across multiple classes without any lifetime management.
    /// </summary>
    public static WmiConnection Shared { get; } = new WmiConnection();

    private readonly string _namespace = wmiNamespace;
    private readonly Lock _lock = new();
    private ComPtr<IWbemServices> _services;

    private const uint RPC_C_AUTHN_WINNT = 10;
    private const uint RPC_C_AUTHZ_NONE = 0;
    private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
    private const uint EOAC_NONE = 0;

    /// <summary>
    /// Executes a WQL query and calls <paramref name="rowCallback"/> for every
    /// returned object. If the connection is stale it is automatically
    /// re-established before the query runs.
    /// </summary>
    /// <param name="wql">The WQL SELECT statement to execute.</param>
    /// <param name="rowCallback">
    /// Receives each <see cref="IWbemClassObject"/> row. The pointer is only
    /// valid for the duration of the callback — do not cache it.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the query completed (even with zero rows);
    /// <see langword="false"/> if the connection could not be established or
    /// the query itself failed.
    /// </returns>
    public bool ExecuteWmiQuery(string wql, Action<nint> rowCallback)
    {
        lock (_lock)
        {
            if (!EnsureConnected()) return false;

            ComPtr<IEnumWbemClassObject> enumerator = default;

            try
            {
                using ManagedPtr<char> lang = "WQL";
                using ManagedPtr<char> query = wql;

                var hr = _services.Get()->ExecQuery(
                    lang, query,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY |
                          WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null,
                    enumerator.GetAddressOf());

                if (FAILED(hr))
                {
                    // Stale connection — drop it so the next call reconnects.
                    DropConnection();
                    return false;
                }

                uint returned = 0;

                while (true)
                {
                    using ComPtr<IWbemClassObject> obj = default;

                    if (enumerator.Get()->Next(3000, 1, obj.GetAddressOf(), &returned) != S.S_OK)
                        break;

                    rowCallback?.Invoke((nint)obj.Get());
                }

                return true;
            }
            finally
            {
                if (enumerator.Get() != null)
                    enumerator.Dispose();
            }
        }
    }

    /// <summary>
    /// Reads the property from the WMI class object.
    /// </summary>
    /// <param name="obj">
    /// A pointer to the current <see cref="IWbemClassObject"/>.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property you want to retrieve the value of.
    /// </param>
    /// <returns>
    /// The property's value as the correct type.
    /// </returns>
    private static object? ReadVariant(IWbemClassObject* obj, string propertyName)
    {
        VARIANT v;
        VariantInit(&v);

        try
        {
            using ManagedPtr<char> prop = propertyName;
            obj->Get(prop, 0, &v, null, null);

            return v.vt switch
            {
                (ushort)VARENUM.VT_BSTR when v.Anonymous.Anonymous.Anonymous.bstrVal != null
                    => new string(v.Anonymous.Anonymous.Anonymous.bstrVal),
                (ushort)VARENUM.VT_R8 => v.Anonymous.Anonymous.Anonymous.dblVal,
                (ushort)VARENUM.VT_R4 => (double)v.Anonymous.Anonymous.Anonymous.fltVal,
                (ushort)VARENUM.VT_I4 => (double)v.Anonymous.Anonymous.Anonymous.intVal,
                (ushort)VARENUM.VT_UI4 => (double)v.Anonymous.Anonymous.Anonymous.uintVal,
                (ushort)VARENUM.VT_I2 => (double)v.Anonymous.Anonymous.Anonymous.iVal,
                (ushort)VARENUM.VT_BOOL => v.Anonymous.Anonymous.Anonymous.boolVal != 0,
                (ushort)VARENUM.VT_NULL => null,
                (ushort)VARENUM.VT_EMPTY => null,
                _ => null
            };
        }
        finally
        {
            VariantClear(&v);
        }
    }

    /// <summary>
    /// Reads the property from the WMI class object.
    /// </summary>
    /// <param name="obj">
    /// A pointer to the current <see cref="IWbemClassObject"/>.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property you want to retrieve the value of.
    /// </param>
    /// <returns>
    /// The property's value as <see langword="string"/>.
    /// </returns>
    public static string? GetString(IWbemClassObject* obj, string propertyName)
        => ReadVariant(obj, propertyName) switch
        {
            string s => s,
            _ => null
        };

    /// <summary>
    /// Reads the property from the WMI class object.
    /// </summary>
    /// <param name="obj">
    /// A pointer to the current <see cref="IWbemClassObject"/>.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property you want to retrieve the value of.
    /// </param>
    /// <returns>
    /// The property's value as <see langword="double"/>.
    /// </returns>
    public static double GetDouble(IWbemClassObject* obj, string propertyName)
        => ReadVariant(obj, propertyName) switch
        {
            double d => d,
            string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0,
            _ => 0
        };

    /// <summary>
    /// Reads the property from the WMI class object.
    /// </summary>
    /// <param name="obj">
    /// A pointer to the current <see cref="IWbemClassObject"/>.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property you want to retrieve the value of.
    /// </param>
    /// <returns>
    /// The property's value as <see langword="bool"/>.
    /// </returns>
    public static bool GetBool(IWbemClassObject* obj, string propertyName)
        => ReadVariant(obj, propertyName) switch
        {
            bool b => b,
            double d => d != 0,
            string s => s is "1" or "true" or "True",
            _ => false
        };

    /// <summary>
    /// Reads the property from the WMI class object.
    /// </summary>
    /// <param name="obj">
    /// A pointer to the current <see cref="IWbemClassObject"/>.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property you want to retrieve the value of.
    /// </param>
    /// <returns>
    /// The property's value as <see langword="int"/>.
    /// </returns>
    public static int GetInt(IWbemClassObject* obj, string propertyName)
        => ReadVariant(obj, propertyName) switch
        {
            double d => (int)d,
            string s => int.TryParse(s, out var i) ? i : 0,
            _ => 0
        };

    private bool EnsureConnected()
    {
        if (_services.Get() != null) return true;

        ComPtr<IWbemLocator> locator = default;
        using var clsid = new ManagedPtr<Guid>(CLSID.CLSID_WbemLocator);
        using var iid = new ManagedPtr<Guid>(IID.IID_IWbemLocator);

        if (FAILED(CoCreateInstance(clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, iid, (void**)locator.GetAddressOf())))
            return false;

        try
        {
            using ManagedPtr<char> ns = _namespace;

            if (FAILED(locator.Get()->ConnectServer(ns, null, null, null, 0, null, null, _services.GetAddressOf())))
                return false;

            if (FAILED(CoSetProxyBlanket(
                    (IUnknown*)_services.Get(),
                    RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                    RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE,
                    null, EOAC_NONE)))
            {
                DropConnection();
                return false;
            }

            return true;
        }
        finally
        {
            locator.Dispose();
        }
    }

    private void DropConnection()
    {
        if (_services.Get() != null)
            _services.Dispose();
    }
}

public static class WMI
{
    private const uint RPC_C_AUTHN_WINNT = 10;
    private const uint RPC_C_AUTHZ_NONE = 0;
    private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
    private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
    private const uint EOAC_NONE = 0;

    [Obsolete("Deprecated. Use WmiConnection.ExecuteQuery instead.")]
    public static unsafe string? QueryWmiString(string query, string property)
    {
        HRESULT hr;
        string? result = null;
        ComPtr<IWbemLocator> pLocator = default;
        ComPtr<IWbemServices> pServices = default;
        ComPtr<IEnumWbemClassObject> pEnumerator = default;

        try
        {
            var clsid = CLSID.CLSID_WbemLocator;
            var iid = IID.IID_IWbemLocator;
            hr = CoCreateInstance(&clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)pLocator.GetAddressOf());
            if (FAILED(hr)) return null;

            fixed (char* pNamespace = "ROOT\\CIMV2")
                hr = pLocator.Get()->ConnectServer(pNamespace, null, null, null, 0, null, null, pServices.GetAddressOf());
            if (FAILED(hr)) return null;

            hr = CoSetProxyBlanket((IUnknown*)pServices.Get(), RPC_C_AUTHN_WINNT, RPC_C_AUTHZ_NONE, null,
                                   RPC_C_AUTHN_LEVEL_CALL, RPC_C_IMP_LEVEL_IMPERSONATE, null, EOAC_NONE);
            if (FAILED(hr)) return null;

            fixed (char* pLanguage = "WQL")
            fixed (char* pQuery = query)  // NOTE: query must be a local variable for fixed to work, see below
                hr = pServices.Get()->ExecQuery(pLanguage, pQuery,
                    (int)(WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY | WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY),
                    null, pEnumerator.GetAddressOf());
            if (FAILED(hr)) return null;

            IWbemClassObject* pObj = null;
            uint returned = 0;

            fixed (char* pProperty = property)
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
        finally
        {
            if (pEnumerator.Get() is not null) pEnumerator.Dispose();
            if (pServices.Get() is not null) pServices.Dispose();
            if (pLocator.Get() is not null) pLocator.Dispose();
        }

        return result;
    }
}
