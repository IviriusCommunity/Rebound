// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

public class CDefragClient
{
    public unsafe HRESULT ReRegisterEngine(IDefragEnginePriv* enginePtr)
    {
        // Initialize COM for this thread
        HRESULT hrInit;

        var clsid_DefragEngine = CLSID.CLSID_DefragEngine;
        var iid_IDefragEnginePriv = IDefragEnginePriv.Guid;

        // CoCreateInstance to create the COM object instance
        hrInit = PInvoke.CoCreateInstance(
            clsid_DefragEngine,
            null,
            CLSCTX.CLSCTX_LOCAL_SERVER,
            &iid_IDefragEnginePriv,
            (void**)&enginePtr);

        if (hrInit.Succeeded)
        {
            hrInit = Unregister(enginePtr);
            if (hrInit.Succeeded)
            {
                hrInit = Register(enginePtr);
            }
        }

        if (enginePtr != (nint*)0)
        {
            ((IUnknown*)enginePtr)->Release();
        }

        return hrInit;
    }

    public unsafe HRESULT Register(IDefragEnginePriv* enginePtr)
    {
        /*if (enginePtr == null)
            return HRESULT.E_POINTER;

        // Step 1: AddRef enginePtr
        ((IUnknown*)enginePtr)->AddRef();

        // Step 3: QueryInterface IID_IDefragClient on that interface
        IDefragClient* defragClient = null;
        void* clientPtr;
        HRESULT hr = someInterface->QueryInterface(in IDefragClient.Guid, out clientPtr);
        if (hr.Succeeded)
        {
            defragClient = (IDefragClient*)clientPtr;
        }
        else
        {
            someInterface->Release();
            enginePtr->Release();
            return hr;
        }

        // Step 4: Call Register on enginePtr
        // Assuming `this.RegisterDataPtr` corresponds to (this + 0x140) in native
        IntPtr registerDataPtr = this.RegisterDataPtr;

        hr = enginePtr->Register(defragClient, registerDataPtr);

        // Step 5: Cleanup: release interfaces
        if (defragClient != null)
        {
            defragClient->Release();
        }

        someInterface->Release();
        enginePtr->Release();

        return hr;*/
        return new();
    }

    public unsafe HRESULT Unregister(IDefragEnginePriv* enginePtr)
    {
        // Unregister the engine
        HRESULT hr = new((int)enginePtr->Unregister(0, null).Value);
        return hr;
    }

    public unsafe void LoadDefragCOM()
    {
        // Initialize COM for this thread
        var hrInit = PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED);
        if (hrInit.Failed)
        {
            Debug.WriteLine($"CoInitializeEx failed: 0x{hrInit.Value:X8}");
            return;
        }

        object pAuthList;
        object pa2;

        try
        {
            unsafe
            {
                PInvoke.CoInitializeSecurity(
                    PSECURITY_DESCRIPTOR.Null,
                    -1,
                    null,
                    RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_PKT_PRIVACY,
                    RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE,
                    null,
                    EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE
                    );

                IDefragEnginePriv* enginePtr = null;

                var clsid_DefragEngine = CLSID.CLSID_DefragEngine;
                var iid_IDefragEnginePriv = IDefragEnginePriv.Guid;

                // CoCreateInstance to create the COM object instance
                hrInit = PInvoke.CoCreateInstance(
                    clsid_DefragEngine,
                    null,
                    CLSCTX.CLSCTX_LOCAL_SERVER,
                    &iid_IDefragEnginePriv,
                    (void**)&enginePtr);

                if (hrInit.Failed)
                {
                    Debug.WriteLine($"CoCreateInstance failed: 0x{hrInit.Value:X8}");
                    return;
                }

                Debug.WriteLine($"CoCreateInstance succeeded. Interface pointer: 0x{(nint)enginePtr:X}");

                var instanceGuid = Guid.NewGuid();

                Guid partitionGuid = new Guid("4d5f1423-15bf-4e63-9db1-d365ba0d1470");
                Guid diskGUID = new Guid("6077191f-0022-40df-b12b-7771d45d519f");

                HRESULT hr;

                fixed (char* pVol = "\\\\?\\Volume{4d5f1423-15bf-4e63-9db1-d365ba0d1470}\\")
                {
                    hr = enginePtr->Analyze(pVol, &partitionGuid, &diskGUID);
                }

                Debug.WriteLine(hr);

                var hr3 = enginePtr->Cancel(instanceGuid);

                Debug.WriteLine(hr3);
            }
        }
        finally
        {

        }
    }
}