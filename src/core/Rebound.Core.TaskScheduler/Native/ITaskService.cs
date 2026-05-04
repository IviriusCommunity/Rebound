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

public unsafe partial struct ITaskService
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        // IDispatch slots (3-6) — kept as opaque pointers; callers use late-binding or skip them.
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, HRESULT> GetTypeInfoCount;
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint, void**, HRESULT> GetTypeInfo;
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, ushort***, int, uint, int*, HRESULT> GetIDsOfNames;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, Guid*, uint, ushort, VARIANT*, VARIANT*, EXCEPINFO*, uint*, HRESULT> Invoke;

        // ITaskService slots (7+)
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, ITaskFolder**, HRESULT> GetFolder;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, IRunningTaskCollection**, HRESULT> GetRunningTasks;
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, ITaskDefinition**, HRESULT> NewTask;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, VARIANT, VARIANT, VARIANT, HRESULT> Connect;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_Connected;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_TargetServer;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_ConnectedUser;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_ConnectedDomain;
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, HRESULT> get_HighestVersion;
    }

    [GuidRVAGen.Guid("2FABA4C7-4DA9-4013-9697-20CC3FD40F85")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITaskService : ITaskService.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITaskService*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, uint>)lpVtbl[1])
            ((ITaskService*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, uint>)lpVtbl[2])
            ((ITaskService*)Unsafe.AsPointer(in this));

    // Slots 3-6 are IDispatch; omitted from direct projection.

    public HRESULT GetFolder(ushort* path, ITaskFolder** ppFolder) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, ushort*, ITaskFolder**, HRESULT>)lpVtbl[7])
            ((ITaskService*)Unsafe.AsPointer(in this), path, ppFolder);

    public HRESULT NewTask(uint flags, ITaskDefinition** ppDefinition) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, uint, ITaskDefinition**, HRESULT>)lpVtbl[9])
            ((ITaskService*)Unsafe.AsPointer(in this), flags, ppDefinition);

    public HRESULT Connect(VARIANT server, VARIANT user, VARIANT domain, VARIANT password) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, VARIANT, VARIANT, VARIANT, VARIANT, HRESULT>)lpVtbl[10])
            ((ITaskService*)Unsafe.AsPointer(in this), server, user, domain, password);

    public HRESULT get_Connected(BOOL* pConnected) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, BOOL*, HRESULT>)lpVtbl[11])
            ((ITaskService*)Unsafe.AsPointer(in this), pConnected);

    public HRESULT get_TargetServer(ushort** pServer) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, ushort**, HRESULT>)lpVtbl[12])
            ((ITaskService*)Unsafe.AsPointer(in this), pServer);

    public HRESULT get_ConnectedUser(ushort** pUser) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, ushort**, HRESULT>)lpVtbl[13])
            ((ITaskService*)Unsafe.AsPointer(in this), pUser);

    public HRESULT get_ConnectedDomain(ushort** pDomain) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, ushort**, HRESULT>)lpVtbl[14])
            ((ITaskService*)Unsafe.AsPointer(in this), pDomain);

    public HRESULT get_HighestVersion(uint* pVersion) =>
        ((delegate* unmanaged[MemberFunction]<ITaskService*, uint*, HRESULT>)lpVtbl[15])
            ((ITaskService*)Unsafe.AsPointer(in this), pVersion);

    public interface Interface : IUnknown.Interface
    {
        HRESULT GetFolder(ushort* path, ITaskFolder** ppFolder);
        HRESULT NewTask(uint flags, ITaskDefinition** ppDefinition);
        HRESULT Connect(VARIANT server, VARIANT user, VARIANT domain, VARIANT password);
        HRESULT get_Connected(BOOL* pConnected);
        HRESULT get_TargetServer(ushort** pServer);
        HRESULT get_ConnectedUser(ushort** pUser);
        HRESULT get_ConnectedDomain(ushort** pDomain);
        HRESULT get_HighestVersion(uint* pVersion);
    }
}
