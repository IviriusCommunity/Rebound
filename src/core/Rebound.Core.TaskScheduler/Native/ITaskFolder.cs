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

public unsafe partial struct ITaskFolder
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        // IDispatch (3-6)
        public void* _disp0, _disp1, _disp2, _disp3;

        // ITaskFolder (7+)
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Name;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Path;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, ITaskFolder**, HRESULT> GetFolder;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, ITaskFolderCollection**, HRESULT> GetFolders;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, IRegisteredTask**, HRESULT> GetTask;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, IRegisteredTaskCollection**, HRESULT> GetTasks;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, long*, HRESULT> GetNumberOfMissedRuns;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, ushort*, int, VARIANT, VARIANT, TASK_LOGON_TYPE, IRegisteredTask**, HRESULT> RegisterTask;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, ITaskDefinition*, int, VARIANT, VARIANT, TASK_LOGON_TYPE, VARIANT, IRegisteredTask**, HRESULT> RegisterTaskDefinition;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, VARIANT, HRESULT> DeleteTask;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, VARIANT, HRESULT> DeleteFolder;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, int, HRESULT> SetSecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, ushort**, HRESULT> GetSecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, VARIANT, ITaskFolder**, HRESULT> CreateFolder;
    }

    [GuidRVAGen.Guid("8CFAC062-A080-4C15-9A88-AA7C2AF80DFC")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITaskFolder : ITaskFolder.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITaskFolder*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, uint>)lpVtbl[1])
            ((ITaskFolder*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, uint>)lpVtbl[2])
            ((ITaskFolder*)Unsafe.AsPointer(in this));

    public HRESULT get_Name(ushort** pName) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort**, HRESULT>)lpVtbl[7])
            ((ITaskFolder*)Unsafe.AsPointer(in this), pName);

    public HRESULT get_Path(ushort** pPath) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort**, HRESULT>)lpVtbl[8])
            ((ITaskFolder*)Unsafe.AsPointer(in this), pPath);

    public HRESULT GetTask(ushort* path, IRegisteredTask** ppTask) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort*, IRegisteredTask**, HRESULT>)lpVtbl[11])
            ((ITaskFolder*)Unsafe.AsPointer(in this), path, ppTask);

    public HRESULT RegisterTaskDefinition(
        ushort* path,
        ITaskDefinition* pDefinition,
        int flags,
        VARIANT userId,
        VARIANT password,
        TASK_LOGON_TYPE logonType,
        VARIANT sddl,
        IRegisteredTask** ppTask) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort*, ITaskDefinition*, int, VARIANT, VARIANT, TASK_LOGON_TYPE, VARIANT, IRegisteredTask**, HRESULT>)lpVtbl[15])
            ((ITaskFolder*)Unsafe.AsPointer(in this), path, pDefinition, flags, userId, password, logonType, sddl, ppTask);

    public HRESULT DeleteTask(ushort* name, VARIANT flags) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort*, VARIANT, HRESULT>)lpVtbl[16])
            ((ITaskFolder*)Unsafe.AsPointer(in this), name, flags);

    public HRESULT DeleteFolder(ushort* name, VARIANT flags) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort*, VARIANT, HRESULT>)lpVtbl[17])
            ((ITaskFolder*)Unsafe.AsPointer(in this), name, flags);

    public HRESULT CreateFolder(ushort* subFolderName, VARIANT sddl, ITaskFolder** ppFolder) =>
        ((delegate* unmanaged[MemberFunction]<ITaskFolder*, ushort*, VARIANT, ITaskFolder**, HRESULT>)lpVtbl[20])
            ((ITaskFolder*)Unsafe.AsPointer(in this), subFolderName, sddl, ppFolder);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Name(ushort** pName);
        HRESULT get_Path(ushort** pPath);
        HRESULT GetTask(ushort* path, IRegisteredTask** ppTask);
        HRESULT RegisterTaskDefinition(ushort* path, ITaskDefinition* pDefinition, int flags,
            VARIANT userId, VARIANT password, TASK_LOGON_TYPE logonType, VARIANT sddl, IRegisteredTask** ppTask);
        HRESULT DeleteTask(ushort* name, VARIANT flags);
        HRESULT DeleteFolder(ushort* name, VARIANT flags);
        HRESULT CreateFolder(ushort* subFolderName, VARIANT sddl, ITaskFolder** ppFolder);
    }
}
