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

public unsafe partial struct IPrincipal
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_DisplayName;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_DisplayName;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_UserId;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_UserId;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_LOGON_TYPE*, HRESULT> get_LogonType;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_LOGON_TYPE, HRESULT> put_LogonType;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_GroupId;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_GroupId;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_RUNLEVEL_TYPE*, HRESULT> get_RunLevel;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_RUNLEVEL_TYPE, HRESULT> put_RunLevel;
    }

    [GuidRVAGen.Guid("D98D51E5-C9B4-496A-A9C1-18980261CF0F")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IPrincipal : IPrincipal.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IPrincipal*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, uint>)lpVtbl[1])
            ((IPrincipal*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, uint>)lpVtbl[2])
            ((IPrincipal*)Unsafe.AsPointer(in this));

    public HRESULT get_Id(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort**, HRESULT>)lpVtbl[7])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Id(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort*, HRESULT>)lpVtbl[8])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public HRESULT get_DisplayName(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort**, HRESULT>)lpVtbl[9])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_DisplayName(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort*, HRESULT>)lpVtbl[10])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public HRESULT get_UserId(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort**, HRESULT>)lpVtbl[11])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_UserId(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort*, HRESULT>)lpVtbl[12])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public HRESULT get_LogonType(TASK_LOGON_TYPE* p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, TASK_LOGON_TYPE*, HRESULT>)lpVtbl[13])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_LogonType(TASK_LOGON_TYPE v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, TASK_LOGON_TYPE, HRESULT>)lpVtbl[14])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public HRESULT get_GroupId(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort**, HRESULT>)lpVtbl[15])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_GroupId(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, ushort*, HRESULT>)lpVtbl[16])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public HRESULT get_RunLevel(TASK_RUNLEVEL_TYPE* p) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, TASK_RUNLEVEL_TYPE*, HRESULT>)lpVtbl[17])
            ((IPrincipal*)Unsafe.AsPointer(in this), p);

    public HRESULT put_RunLevel(TASK_RUNLEVEL_TYPE v) =>
        ((delegate* unmanaged[MemberFunction]<IPrincipal*, TASK_RUNLEVEL_TYPE, HRESULT>)lpVtbl[18])
            ((IPrincipal*)Unsafe.AsPointer(in this), v);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Id(ushort** p);
        HRESULT put_Id(ushort* v);
        HRESULT get_DisplayName(ushort** p);
        HRESULT put_DisplayName(ushort* v);
        HRESULT get_UserId(ushort** p);
        HRESULT put_UserId(ushort* v);
        HRESULT get_LogonType(TASK_LOGON_TYPE* p);
        HRESULT put_LogonType(TASK_LOGON_TYPE v);
        HRESULT get_GroupId(ushort** p);
        HRESULT put_GroupId(ushort* v);
        HRESULT get_RunLevel(TASK_RUNLEVEL_TYPE* p);
        HRESULT put_RunLevel(TASK_RUNLEVEL_TYPE v);
    }
}
