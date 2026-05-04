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

public unsafe partial struct ITrigger
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_TRIGGER_TYPE2*, HRESULT> get_Type;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, IRepetitionPattern**, HRESULT> get_Repetition;
        public delegate* unmanaged[MemberFunction]<TSelf*, IRepetitionPattern*, HRESULT> put_Repetition;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_ExecutionTimeLimit;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_ExecutionTimeLimit;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_StartBoundary;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_StartBoundary;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_EndBoundary;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_EndBoundary;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_Enabled;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_Enabled;
    }

    [GuidRVAGen.Guid("09941815-EA89-4B5B-89E0-2A773801FAC3")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITrigger : ITrigger.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITrigger*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, uint>)lpVtbl[1])
            ((ITrigger*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, uint>)lpVtbl[2])
            ((ITrigger*)Unsafe.AsPointer(in this));

    public HRESULT get_Type(TASK_TRIGGER_TYPE2* p) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, TASK_TRIGGER_TYPE2*, HRESULT>)lpVtbl[7])
            ((ITrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Id(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort**, HRESULT>)lpVtbl[8])
            ((ITrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Id(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort*, HRESULT>)lpVtbl[9])
            ((ITrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_StartBoundary(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort**, HRESULT>)lpVtbl[15])
            ((ITrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_StartBoundary(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort*, HRESULT>)lpVtbl[16])
            ((ITrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_EndBoundary(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort**, HRESULT>)lpVtbl[17])
            ((ITrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_EndBoundary(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, ushort*, HRESULT>)lpVtbl[18])
            ((ITrigger*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Enabled(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, BOOL*, HRESULT>)lpVtbl[19])
            ((ITrigger*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Enabled(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<ITrigger*, BOOL, HRESULT>)lpVtbl[20])
            ((ITrigger*)Unsafe.AsPointer(in this), v);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Type(TASK_TRIGGER_TYPE2* p);
        HRESULT get_Id(ushort** p);
        HRESULT put_Id(ushort* v);
        HRESULT get_StartBoundary(ushort** p);
        HRESULT put_StartBoundary(ushort* v);
        HRESULT get_EndBoundary(ushort** p);
        HRESULT put_EndBoundary(ushort* v);
        HRESULT get_Enabled(BOOL* p);
        HRESULT put_Enabled(BOOL v);
    }
}
