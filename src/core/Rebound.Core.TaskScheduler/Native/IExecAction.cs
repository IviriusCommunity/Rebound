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

public unsafe partial struct IExecAction
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        // IAction slots (7-9)
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Id;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_ACTION_TYPE*, HRESULT> get_Type;

        // IExecAction slots (10+)
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Path;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Path;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Arguments;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Arguments;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_WorkingDirectory;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_WorkingDirectory;
    }

    [GuidRVAGen.Guid("4C3D624D-FD6B-49A3-B9B7-09CB3CD3F047")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IExecAction : IExecAction.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IExecAction*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, uint>)lpVtbl[1])
            ((IExecAction*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, uint>)lpVtbl[2])
            ((IExecAction*)Unsafe.AsPointer(in this));

    // IAction forwarding
    public HRESULT get_Id(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort**, HRESULT>)lpVtbl[7])
            ((IExecAction*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Id(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort*, HRESULT>)lpVtbl[8])
            ((IExecAction*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Type(TASK_ACTION_TYPE* p) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, TASK_ACTION_TYPE*, HRESULT>)lpVtbl[9])
            ((IExecAction*)Unsafe.AsPointer(in this), p);

    // IExecAction members
    public HRESULT get_Path(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort**, HRESULT>)lpVtbl[10])
            ((IExecAction*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Path(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort*, HRESULT>)lpVtbl[11])
            ((IExecAction*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Arguments(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort**, HRESULT>)lpVtbl[12])
            ((IExecAction*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Arguments(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort*, HRESULT>)lpVtbl[13])
            ((IExecAction*)Unsafe.AsPointer(in this), v);

    public HRESULT get_WorkingDirectory(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort**, HRESULT>)lpVtbl[14])
            ((IExecAction*)Unsafe.AsPointer(in this), p);

    public HRESULT put_WorkingDirectory(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IExecAction*, ushort*, HRESULT>)lpVtbl[15])
            ((IExecAction*)Unsafe.AsPointer(in this), v);

    public interface Interface : IAction.Interface
    {
        HRESULT get_Path(ushort** p);
        HRESULT put_Path(ushort* v);
        HRESULT get_Arguments(ushort** p);
        HRESULT put_Arguments(ushort* v);
        HRESULT get_WorkingDirectory(ushort** p);
        HRESULT put_WorkingDirectory(ushort* v);
    }
}
