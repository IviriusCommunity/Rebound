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

public unsafe partial struct ITaskSettings
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_AllowDemandStart;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_AllowDemandStart;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_RestartInterval;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_RestartInterval;
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_RestartCount;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, HRESULT> put_RestartCount;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_INSTANCES_POLICY*, HRESULT> get_MultipleInstances;
        public delegate* unmanaged[MemberFunction]<TSelf*, TASK_INSTANCES_POLICY, HRESULT> put_MultipleInstances;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_StopIfGoingOnBatteries;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_StopIfGoingOnBatteries;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_DisallowStartIfOnBatteries;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_DisallowStartIfOnBatteries;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_AllowHardTerminate;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_AllowHardTerminate;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_StartWhenAvailable;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_StartWhenAvailable;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_RunOnlyIfNetworkAvailable;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_RunOnlyIfNetworkAvailable;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_ExecutionTimeLimit;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_ExecutionTimeLimit;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_Enabled;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_Enabled;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_DeleteExpiredTaskAfter;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_DeleteExpiredTaskAfter;
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_Priority;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, HRESULT> put_Priority;
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, HRESULT> get_Compatibility;
        public delegate* unmanaged[MemberFunction]<TSelf*, int, HRESULT> put_Compatibility;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_Hidden;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_Hidden;
        public delegate* unmanaged[MemberFunction]<TSelf*, IIdleSettings**, HRESULT> get_IdleSettings;
        public delegate* unmanaged[MemberFunction]<TSelf*, IIdleSettings*, HRESULT> put_IdleSettings;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_RunOnlyIfIdle;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_RunOnlyIfIdle;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL*, HRESULT> get_WakeToRun;
        public delegate* unmanaged[MemberFunction]<TSelf*, BOOL, HRESULT> put_WakeToRun;
        public delegate* unmanaged[MemberFunction]<TSelf*, INetworkSettings**, HRESULT> get_NetworkSettings;
        public delegate* unmanaged[MemberFunction]<TSelf*, INetworkSettings*, HRESULT> put_NetworkSettings;
    }

    [GuidRVAGen.Guid("8FD4711D-2D02-4C8C-87E3-EFF699DE127E")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITaskSettings : ITaskSettings.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITaskSettings*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, uint>)lpVtbl[1])
            ((ITaskSettings*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, uint>)lpVtbl[2])
            ((ITaskSettings*)Unsafe.AsPointer(in this));

    public HRESULT get_AllowDemandStart(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL*, HRESULT>)lpVtbl[7])
            ((ITaskSettings*)Unsafe.AsPointer(in this), p);

    public HRESULT put_AllowDemandStart(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL, HRESULT>)lpVtbl[8])
            ((ITaskSettings*)Unsafe.AsPointer(in this), v);

    public HRESULT get_MultipleInstances(TASK_INSTANCES_POLICY* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, TASK_INSTANCES_POLICY*, HRESULT>)lpVtbl[13])
            ((ITaskSettings*)Unsafe.AsPointer(in this), p);

    public HRESULT put_MultipleInstances(TASK_INSTANCES_POLICY v) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, TASK_INSTANCES_POLICY, HRESULT>)lpVtbl[14])
            ((ITaskSettings*)Unsafe.AsPointer(in this), v);
    public HRESULT put_DisallowStartIfOnBatteries(BOOL v) =>
    ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL, HRESULT>)lpVtbl[16])
        ((ITaskSettings*)Unsafe.AsPointer(in this), v);

    public HRESULT get_ExecutionTimeLimit(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, ushort**, HRESULT>)lpVtbl[27])
            ((ITaskSettings*)Unsafe.AsPointer(in this), p);

    public HRESULT put_ExecutionTimeLimit(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, ushort*, HRESULT>)lpVtbl[28])
            ((ITaskSettings*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Enabled(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL*, HRESULT>)lpVtbl[29])
            ((ITaskSettings*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Enabled(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL, HRESULT>)lpVtbl[30])
            ((ITaskSettings*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Hidden(BOOL* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL*, HRESULT>)lpVtbl[37])
            ((ITaskSettings*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Hidden(BOOL v) =>
        ((delegate* unmanaged[MemberFunction]<ITaskSettings*, BOOL, HRESULT>)lpVtbl[38])
            ((ITaskSettings*)Unsafe.AsPointer(in this), v);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_AllowDemandStart(BOOL* p);
        HRESULT put_AllowDemandStart(BOOL v);
        HRESULT get_MultipleInstances(TASK_INSTANCES_POLICY* p);
        HRESULT put_MultipleInstances(TASK_INSTANCES_POLICY v);
        HRESULT put_DisallowStartIfOnBatteries(BOOL v);
        HRESULT get_ExecutionTimeLimit(ushort** p);
        HRESULT put_ExecutionTimeLimit(ushort* v);
        HRESULT get_Enabled(BOOL* p);
        HRESULT put_Enabled(BOOL v);
        HRESULT get_Hidden(BOOL* p);
        HRESULT put_Hidden(BOOL v);
    }
}
