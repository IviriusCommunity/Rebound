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

public unsafe partial struct ITriggerCollection
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_Count;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, ITrigger**, HRESULT> get_Item;
        public delegate* unmanaged[MemberFunction]<TSelf*, IUnknown**, HRESULT> get__NewEnum;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_TRIGGER_TYPE2, ITrigger**, HRESULT> Create;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, HRESULT> Remove;
        public delegate* unmanaged[MemberFunction]<TSelf*, HRESULT> Clear;
    }

    [GuidRVAGen.Guid("85DF5081-1B24-4F32-878A-D9D14DF4CB77")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITriggerCollection : ITriggerCollection.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITriggerCollection*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, uint>)lpVtbl[1])
            ((ITriggerCollection*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, uint>)lpVtbl[2])
            ((ITriggerCollection*)Unsafe.AsPointer(in this));

    public HRESULT get_Count(int* p) =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, int*, HRESULT>)lpVtbl[7])
            ((ITriggerCollection*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Item(int index, ITrigger** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, int, ITrigger**, HRESULT>)lpVtbl[8])
            ((ITriggerCollection*)Unsafe.AsPointer(in this), index, pp);

    public HRESULT Create(TASK_TRIGGER_TYPE2 type, ITrigger** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, TASK_TRIGGER_TYPE2, ITrigger**, HRESULT>)lpVtbl[10])
            ((ITriggerCollection*)Unsafe.AsPointer(in this), type, pp);

    public HRESULT Remove(VARIANT index) =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, VARIANT, HRESULT>)lpVtbl[11])
            ((ITriggerCollection*)Unsafe.AsPointer(in this), index);

    public HRESULT Clear() =>
        ((delegate* unmanaged[MemberFunction]<ITriggerCollection*, HRESULT>)lpVtbl[12])
            ((ITriggerCollection*)Unsafe.AsPointer(in this));

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Count(int* p);
        HRESULT get_Item(int index, ITrigger** pp);
        HRESULT Create(TASK_TRIGGER_TYPE2 type, ITrigger** pp);
        HRESULT Remove(VARIANT index);
        HRESULT Clear();
    }
}
