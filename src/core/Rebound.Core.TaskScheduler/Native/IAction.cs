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

public unsafe partial struct IAction
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_ACTION_TYPE*, HRESULT> get_Type;
    }

    [GuidRVAGen.Guid("BAE54997-48B1-4CBE-9965-D6BE263EBEA4")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IAction : IAction.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IAction*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IAction*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IAction*, uint>)lpVtbl[1])
            ((IAction*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IAction*, uint>)lpVtbl[2])
            ((IAction*)Unsafe.AsPointer(in this));

    public HRESULT get_Id(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IAction*, ushort**, HRESULT>)lpVtbl[7])
            ((IAction*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Id(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IAction*, ushort*, HRESULT>)lpVtbl[8])
            ((IAction*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Type(TASK_ACTION_TYPE* p) =>
        ((delegate* unmanaged[MemberFunction]<IAction*, TASK_ACTION_TYPE*, HRESULT>)lpVtbl[9])
            ((IAction*)Unsafe.AsPointer(in this), p);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Id(ushort** p);
        HRESULT put_Id(ushort* v);
        HRESULT get_Type(TASK_ACTION_TYPE* p);
    }
}
