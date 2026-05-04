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

public unsafe partial struct IActionCollection
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_Count;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, IAction**, HRESULT> get_Item;
        public delegate* unmanaged[MemberFunction]<TSelf*, IUnknown**, HRESULT> get__NewEnum;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_ACTION_TYPE, IAction**, HRESULT> Create;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, HRESULT> Remove;
        public delegate* unmanaged[MemberFunction]<TSelf*, HRESULT> Clear;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Context;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Context;
    }

    [GuidRVAGen.Guid("02820E19-7B98-4ED2-B2E8-FDCCCEFF619B")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IActionCollection : IActionCollection.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IActionCollection*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, uint>)lpVtbl[1])
            ((IActionCollection*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, uint>)lpVtbl[2])
            ((IActionCollection*)Unsafe.AsPointer(in this));

    public HRESULT get_Count(int* p) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, int*, HRESULT>)lpVtbl[7])
            ((IActionCollection*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Item(int index, IAction** pp) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, int, IAction**, HRESULT>)lpVtbl[8])
            ((IActionCollection*)Unsafe.AsPointer(in this), index, pp);

    public HRESULT Create(TASK_ACTION_TYPE type, IAction** pp) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, TASK_ACTION_TYPE, IAction**, HRESULT>)lpVtbl[12])
            ((IActionCollection*)Unsafe.AsPointer(in this), type, pp);

    public HRESULT Remove(VARIANT index) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, VARIANT, HRESULT>)lpVtbl[13])
            ((IActionCollection*)Unsafe.AsPointer(in this), index);

    public HRESULT Clear() =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, HRESULT>)lpVtbl[14])
            ((IActionCollection*)Unsafe.AsPointer(in this));

    public HRESULT get_Context(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, ushort**, HRESULT>)lpVtbl[15])
            ((IActionCollection*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Context(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IActionCollection*, ushort*, HRESULT>)lpVtbl[16])
            ((IActionCollection*)Unsafe.AsPointer(in this), v);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Count(int* p);
        HRESULT get_Item(int index, IAction** pp);
        HRESULT Create(TASK_ACTION_TYPE type, IAction** pp);
        HRESULT Remove(VARIANT index);
        HRESULT Clear();
        HRESULT get_Context(ushort** p);
        HRESULT put_Context(ushort* v);
    }
}
