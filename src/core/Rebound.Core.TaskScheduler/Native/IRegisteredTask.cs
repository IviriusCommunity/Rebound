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

public unsafe partial struct IRegisteredTask
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Name;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Path;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_STATE*, HRESULT> get_State;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_Enabled;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_Enabled;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, IRunningTask**, HRESULT> Run;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, int, int, ushort*, IRunningTask**, HRESULT> RunEx;
        public delegate* unmanaged[MemberFunction]<TSelf*, IRunningTaskCollection**, HRESULT> GetInstances;
        public delegate* unmanaged[MemberFunction]<TSelf*, double*, HRESULT> get_LastRunTime;
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_LastTaskResult;
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_NumberOfMissedRuns;
        public delegate* unmanaged[MemberFunction]<TSelf*, double*, HRESULT> get_NextRunTime;
        public delegate* unmanaged[MemberFunction]<TSelf*, ITaskDefinition**, HRESULT> get_Definition;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Xml;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, ushort**, HRESULT> GetSecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, int, HRESULT> SetSecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, HRESULT> Stop;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT*, VARIANT*, uint*, double*, HRESULT> GetRunTimes;
    }

    [GuidRVAGen.Guid("9C86F320-DEE3-4DD1-B972-A303F26B061E")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IRegisteredTask : IRegisteredTask.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, uint>)lpVtbl[1])
            ((IRegisteredTask*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, uint>)lpVtbl[2])
            ((IRegisteredTask*)Unsafe.AsPointer(in this));

    public HRESULT get_Name(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, ushort**, HRESULT>)lpVtbl[7])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Path(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, ushort**, HRESULT>)lpVtbl[8])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_State(TASK_STATE* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, TASK_STATE*, HRESULT>)lpVtbl[9])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Enabled(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, BOOL*, HRESULT>)lpVtbl[10])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Enabled(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, BOOL, HRESULT>)lpVtbl[11])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), v);

    public HRESULT Run(VARIANT parameters, IRunningTask** ppRunningTask) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, VARIANT, IRunningTask**, HRESULT>)lpVtbl[12])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), parameters, ppRunningTask);

    public HRESULT GetInstances(IRunningTaskCollection** ppRunningTasks) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, IRunningTaskCollection**, HRESULT>)lpVtbl[14])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), ppRunningTasks);

    public HRESULT get_LastRunTime(double* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, double*, HRESULT>)lpVtbl[15])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_LastTaskResult(int* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, int*, HRESULT>)lpVtbl[16])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_NumberOfMissedRuns(int* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, int*, HRESULT>)lpVtbl[17])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_NextRunTime(double* p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, double*, HRESULT>)lpVtbl[18])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Definition(ITaskDefinition** pp) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, ITaskDefinition**, HRESULT>)lpVtbl[19])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), pp);

    public HRESULT get_Xml(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, ushort**, HRESULT>)lpVtbl[20])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), p);

    public HRESULT Stop(int flags) =>
        ((delegate* unmanaged[MemberFunction]<IRegisteredTask*, int, HRESULT>)lpVtbl[23])
            ((IRegisteredTask*)Unsafe.AsPointer(in this), flags);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Name(ushort** p);
        HRESULT get_Path(ushort** p);
        HRESULT get_State(TASK_STATE* p);
        HRESULT get_Enabled(BOOL* p);
        HRESULT put_Enabled(BOOL v);
        HRESULT Run(VARIANT parameters, IRunningTask** ppRunningTask);
        HRESULT GetInstances(IRunningTaskCollection** ppRunningTasks);
        HRESULT get_LastRunTime(double* p);
        HRESULT get_LastTaskResult(int* p);
        HRESULT get_NumberOfMissedRuns(int* p);
        HRESULT get_NextRunTime(double* p);
        HRESULT get_Definition(ITaskDefinition** pp);
        HRESULT get_Xml(ushort** p);
        HRESULT Stop(int flags);
    }
}
