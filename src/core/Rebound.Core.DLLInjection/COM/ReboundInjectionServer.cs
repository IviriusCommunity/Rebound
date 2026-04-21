// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1715 // Identifiers should have correct prefix

namespace Rebound.Core.DLLInjection.COM;

public unsafe partial struct IReboundInjectionServer
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public delegate* unmanaged[MemberFunction]<TSelf*, HRESULT> Uninject;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> IsIdle;
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, HRESULT> GetHostPid;
    }

    [GuidRVAGen.Guid("DA3FE8C7-7248-4DF4-B698-14872C6B1660")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IReboundInjectionServer : IReboundInjectionServer.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;

    public void** lpVtbl;

    // IUnknown forwarding methods (matching TerraFX convention)
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, Guid*, void**, HRESULT>)
            lpVtbl[0])((IReboundInjectionServer*)Unsafe.AsPointer(in this), riid, ppvObject);
    }

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, uint>)lpVtbl[1])
            ((IReboundInjectionServer*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, uint>)lpVtbl[2])
            ((IReboundInjectionServer*)Unsafe.AsPointer(in this));

    // Custom methods
    public HRESULT Uninject() =>
        ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, HRESULT>)lpVtbl[3])
            ((IReboundInjectionServer*)Unsafe.AsPointer(in this));

    public HRESULT IsIdle(BOOL* pIdle) =>
        ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, BOOL*, HRESULT>)lpVtbl[4])
            ((IReboundInjectionServer*)Unsafe.AsPointer(in this), pIdle);

    public HRESULT GetHostPid(uint* pPid) =>
        ((delegate* unmanaged[MemberFunction]<IReboundInjectionServer*, uint*, HRESULT>)lpVtbl[5])
            ((IReboundInjectionServer*)Unsafe.AsPointer(in this), pPid);
    public interface Interface : IUnknown.Interface
    {
        HRESULT Uninject();
        HRESULT IsIdle(BOOL* pIdle);
        HRESULT GetHostPid(uint* pPid);
    }
}

public sealed unsafe class ReboundInjectionServerProxy(IReboundInjectionServer* ptr) : IDisposable
{
    private IReboundInjectionServer* _ptr = ptr;
    private bool _disposed;

    public HRESULT Uninject()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _ptr->Uninject();
    }

    public bool IsIdle()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        BOOL idle;
        return SUCCEEDED(_ptr->IsIdle(&idle)) && idle != 0;
    }

    public uint GetHostPid()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        uint pid;
        _ptr->GetHostPid(&pid);
        return pid;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_ptr != null) { _ptr->Release(); _ptr = null; }
    }
}

public static unsafe class ReboundInjectionROT
{
    public static ReboundInjectionServerProxy? TryGetForPid(uint pid, string dllStem)
    {
        using ComPtr<IRunningObjectTable> pROT = default;
        using ComPtr<IMoniker> pMoniker = default;
        using ComPtr<IBindCtx> pBindCtx = default;
        using ComPtr<IUnknown> pObj = default;
        using ComPtr<IReboundInjectionServer> pServer = default;

        HRESULT hr = GetRunningObjectTable(0, pROT.GetAddressOf());
        if (FAILED(hr)) return null;

        hr = CreateBindCtx(0, pBindCtx.GetAddressOf());
        if (FAILED(hr)) return null;

        using ManagedPtr<char> displayName = $"Rebound.Injection.{pid}.{dllStem}";
        using ManagedPtr<char> exclamationMark = "!";

        hr = CreateItemMoniker(exclamationMark, displayName, pMoniker.GetAddressOf());
        if (FAILED(hr)) return null;

        hr = pROT.Get()->GetObject(pMoniker.Get(), pObj.GetAddressOf());
        if (FAILED(hr)) return null;

        hr = pObj.Get()->QueryInterface(IReboundInjectionServer.NativeGuid, (void**)pServer.GetAddressOf());
        if (FAILED(hr)) return null;

        pServer.Get()->AddRef();
        return new ReboundInjectionServerProxy(pServer.Get());
    }
}