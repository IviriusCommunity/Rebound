// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

#pragma warning disable CA1051  // Do not declare visible instance fields
#pragma warning disable CA1815  // Override equals and operator equals on value types
#pragma warning disable CA1034  // Nested types should not be visible
#pragma warning disable CA1715  // Identifiers should have correct prefix
#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Rebound.Core.TaskScheduler.Native;

public unsafe partial struct ILogonTrigger
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public ITrigger.Vtbl<TSelf> Base;

        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_UserId;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_UserId;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Delay;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Delay;
    }

    [GuidRVAGen.Guid("72DADE38-FAE4-4D2E-8E1E-B6A3A5C1C83F")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ILogonTrigger : ILogonTrigger.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, uint>)lpVtbl[1])
            ((ILogonTrigger*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, uint>)lpVtbl[2])
            ((ILogonTrigger*)Unsafe.AsPointer(in this));

    public HRESULT get_Type(TASK_TRIGGER_TYPE2* p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, TASK_TRIGGER_TYPE2*, HRESULT>)lpVtbl[7])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Id(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort**, HRESULT>)lpVtbl[8])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Id(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort*, HRESULT>)lpVtbl[9])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_StartBoundary(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort**, HRESULT>)lpVtbl[10])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_StartBoundary(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort*, HRESULT>)lpVtbl[11])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_EndBoundary(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort**, HRESULT>)lpVtbl[12])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_EndBoundary(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort*, HRESULT>)lpVtbl[13])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Enabled(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, BOOL*, HRESULT>)lpVtbl[14])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Enabled(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, BOOL, HRESULT>)lpVtbl[15])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_UserId(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort**, HRESULT>)lpVtbl[16])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_UserId(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort*, HRESULT>)lpVtbl[17])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Delay(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort**, HRESULT>)lpVtbl[18])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Delay(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ILogonTrigger*, ushort*, HRESULT>)lpVtbl[19])
            ((ILogonTrigger*)Unsafe.AsPointer(in this), v);

    public interface Interface : ITrigger.Interface
    {
        HRESULT get_UserId(ushort** p);
        HRESULT put_UserId(ushort* v);
        HRESULT get_Delay(ushort** p);
        HRESULT put_Delay(ushort* v);
    }
}