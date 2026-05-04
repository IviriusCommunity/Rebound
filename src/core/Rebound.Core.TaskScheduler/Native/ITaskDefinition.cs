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

public unsafe partial struct ITaskDefinition
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        // IDispatch (3-6)
        public void* _disp0, _disp1, _disp2, _disp3;

        // ITaskDefinition (7+)
        public delegate* unmanaged[MemberFunction]<TSelf*, IRegistrationInfo**, HRESULT> get_RegistrationInfo;
        public delegate* unmanaged[MemberFunction]<TSelf*, IRegistrationInfo*, HRESULT> put_RegistrationInfo;
        public delegate* unmanaged[MemberFunction]<TSelf*, ITriggerCollection**, HRESULT> get_Triggers;
        public delegate* unmanaged[MemberFunction]<TSelf*, ITriggerCollection*, HRESULT> put_Triggers;
        public delegate* unmanaged[MemberFunction]<TSelf*, ITaskSettings**, HRESULT> get_Settings;
        public delegate* unmanaged[MemberFunction]<TSelf*, ITaskSettings*, HRESULT> put_Settings;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Data;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Data;
        public delegate* unmanaged[MemberFunction]<TSelf*, IPrincipal**, HRESULT> get_Principal;
        public delegate* unmanaged[MemberFunction]<TSelf*, IPrincipal*, HRESULT> put_Principal;
        public delegate* unmanaged[MemberFunction]<TSelf*, IActionCollection**, HRESULT> get_Actions;
        public delegate* unmanaged[MemberFunction]<TSelf*, IActionCollection*, HRESULT> put_Actions;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_XmlText;
    }

    [GuidRVAGen.Guid("F5BC8FC5-536D-4F77-B852-FBC1356FDEB6")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct ITaskDefinition : ITaskDefinition.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, uint>)lpVtbl[1])
            ((ITaskDefinition*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, uint>)lpVtbl[2])
            ((ITaskDefinition*)Unsafe.AsPointer(in this));

    public HRESULT get_RegistrationInfo(IRegistrationInfo** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IRegistrationInfo**, HRESULT>)lpVtbl[7])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pp);

    public HRESULT put_RegistrationInfo(IRegistrationInfo* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IRegistrationInfo*, HRESULT>)lpVtbl[8])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Triggers(ITriggerCollection** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ITriggerCollection**, HRESULT>)lpVtbl[9])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pp);

    public HRESULT put_Triggers(ITriggerCollection* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ITriggerCollection*, HRESULT>)lpVtbl[10])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Settings(ITaskSettings** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ITaskSettings**, HRESULT>)lpVtbl[11])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pp);

    public HRESULT put_Settings(ITaskSettings* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ITaskSettings*, HRESULT>)lpVtbl[12])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Data(ushort** pData) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ushort**, HRESULT>)lpVtbl[13])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pData);

    public HRESULT put_Data(ushort* data) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ushort*, HRESULT>)lpVtbl[14])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), data);

    public HRESULT get_Principal(IPrincipal** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IPrincipal**, HRESULT>)lpVtbl[15])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pp);

    public HRESULT put_Principal(IPrincipal* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IPrincipal*, HRESULT>)lpVtbl[16])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), p);

    public HRESULT get_Actions(IActionCollection** pp) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IActionCollection**, HRESULT>)lpVtbl[17])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pp);

    public HRESULT put_Actions(IActionCollection* p) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, IActionCollection*, HRESULT>)lpVtbl[18])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), p);

    public HRESULT get_XmlText(ushort** pXml) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ushort**, HRESULT>)lpVtbl[19])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), pXml);

    public HRESULT put_XmlText(ushort* xml) =>
        ((delegate* unmanaged[MemberFunction]<ITaskDefinition*, ushort*, HRESULT>)lpVtbl[20])
            ((ITaskDefinition*)Unsafe.AsPointer(in this), xml);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_RegistrationInfo(IRegistrationInfo** pp);
        HRESULT put_RegistrationInfo(IRegistrationInfo* p);
        HRESULT get_Triggers(ITriggerCollection** pp);
        HRESULT put_Triggers(ITriggerCollection* p);
        HRESULT get_Settings(ITaskSettings** pp);
        HRESULT put_Settings(ITaskSettings* p);
        HRESULT get_Data(ushort** pData);
        HRESULT put_Data(ushort* data);
        HRESULT get_Principal(IPrincipal** pp);
        HRESULT put_Principal(IPrincipal* p);
        HRESULT get_Actions(IActionCollection** pp);
        HRESULT put_Actions(IActionCollection* p);
        HRESULT get_XmlText(ushort** pXml);
        HRESULT put_XmlText(ushort* xml);
    }
}

/// <summary>
/// Task-creation flags used by ITaskService.NewTask and ITaskFolder.RegisterTaskDefinition.
/// </summary>
public enum TASK_CREATION : uint
{
    TASK_VALIdouble_ONLY = 0x1,
    TASK_CREATE = 0x2,
    TASK_UPdouble = 0x4,
    TASK_CREATE_OR_UPdouble = 0x6,
    TASK_DISABLE = 0x8,
    TASK_DONT_ADD_PRINCIPAL_ACE = 0x10,
    TASK_IGNORE_REGISTRATION_TRIGGERS = 0x20,
}

/// <summary>
/// Logon type used by IPrincipal.
/// </summary>
public enum TASK_LOGON_TYPE : int
{
    TASK_LOGON_NONE = 0,
    TASK_LOGON_PASSWORD = 1,
    TASK_LOGON_S4U = 2,
    TASK_LOGON_INTERACTIVE_TOKEN = 3,
    TASK_LOGON_GROUP = 4,
    TASK_LOGON_SERVICE_ACCOUNT = 5,
    TASK_LOGON_INTERACTIVE_TOKEN_OR_PASSWORD = 6,
}

/// <summary>
/// Run-level used by IPrincipal.
/// </summary>
public enum TASK_RUNLEVEL_TYPE : int
{
    TASK_RUNLEVEL_LUA = 0,
    TASK_RUNLEVEL_HIGHEST = 1,
}

/// <summary>
/// Instance policy used by ITaskSettings.
/// </summary>
public enum TASK_INSTANCES_POLICY : int
{
    TASK_INSTANCES_PARALLEL = 0,
    TASK_INSTANCES_QUEUE = 1,
    TASK_INSTANCES_IGNORE_NEW = 2,
    TASK_INSTANCES_STOP_EXISTING = 3,
}

/// <summary>
/// State used by IRegisteredTask.
/// </summary>
public enum TASK_STATE : int
{
    TASK_STATE_UNKNOWN = 0,
    TASK_STATE_DISABLED = 1,
    TASK_STATE_QUEUED = 2,
    TASK_STATE_READY = 3,
    TASK_STATE_RUNNING = 4,
}

// ─────────────────────────────────────────────────────────────────────────────
// ITrigger  (IID: 09941815-EA89-4B5B-89E0-2A773801FAC3)  — base for all triggers
// Vtable (7+): get/put Type  get/put Id  get/put Repetition  get/put ExecutionTimeLimit
//              get/put StartBoundary  get/put EndBoundary  get/put Enabled
// ─────────────────────────────────────────────────────────────────────────────

public enum TASK_TRIGGER_TYPE2 : int
{
    TASK_TRIGGER_EVENT = 0,
    TASK_TRIGGER_TIME = 1,
    TASK_TRIGGER_DAILY = 2,
    TASK_TRIGGER_WEEKLY = 3,
    TASK_TRIGGER_MONTHLY = 4,
    TASK_TRIGGER_MONTHLYDOW = 5,
    TASK_TRIGGER_IDLE = 6,
    TASK_TRIGGER_REGISTRATION = 7,
    TASK_TRIGGER_BOOT = 8,
    TASK_TRIGGER_LOGON = 9,
    TASK_TRIGGER_SESSION_STATE_CHANGE = 11,
    TASK_TRIGGER_CUSTOM_TRIGGER_01 = 12,
}

// ─────────────────────────────────────────────────────────────────────────────
// ITriggerCollection  (IID: 85DF5081-1B24-4F32-878A-D9D14DF4CB77)
// Vtable (7+): get_Count  get_Item  get__NewEnum  Create  Remove  Clear
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// IAction  (IID: BAE54997-48B1-4CBE-9965-D6BE263EBEA4)  — base for all actions
// Vtable (7+): get/put Id  get_Type
// ─────────────────────────────────────────────────────────────────────────────

public enum TASK_ACTION_TYPE : int
{
    TASK_ACTION_EXEC = 0,
    TASK_ACTION_COM_HANDLER = 5,
    TASK_ACTION_SEND_EMAIL = 6,
    TASK_ACTION_SHOW_MESSAGE = 7,
}

// ─────────────────────────────────────────────────────────────────────────────
// IActionCollection  (IID: 02820E19-7B98-4ED2-B2E8-FDCCCEFF619B)
// Vtable (7+): get_Count  get_Item  get__NewEnum  get/put XmlText
//              Create  Remove  Clear  get/put Context
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// IExecAction  (IID: 4C3D624D-FD6B-49A3-B9B7-09CB3CD3F047)
// Inherits IAction; adds get/put Path, Arguments, WorkingDirectory
// Vtable (10+): get/put Path  get/put Arguments  get/put WorkingDirectory
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// IRegisteredTask  (IID: 9C86F320-DEE3-4DD1-B972-A303F26B061E)
// Vtable (7+): get_Name  get_Path  get_Definition  get_Xml  get_SecurityDescriptor
//              SetSecurityDescriptor  get_Status  get_LastRunTime  get_LastTaskResult
//              get_NumberOfMissedRuns  get_NextRunTime  get_Enabled  put_Enabled
//              Run  RunEx  GetInstances  Stop  GetRunTimes  get_Definition
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// Forward-declared opaque stubs for collection / helper interfaces referenced
// above but not fully projected in this file. Callers that need them should
// add full projections following the same pattern.
// ─────────────────────────────────────────────────────────────────────────────

// IRunningTask          (IID: 653758FB-7B9A-4F1E-A471-BEEB8E9B834E)
// IRunningTaskCollection(IID: 6A67D02B-0A0E-4FA8-B3C3-10A3F4B174D5)
// ITaskFolderCollection (IID: 79184A66-8664-423F-97F5-73EB5BA71AE9)
// IRegisteredTaskCollection(IID: 86627EC4-3D89-48A6-A7C0-B16A956FEAB3)
// IRepetitionPattern    (IID: 7FB9ACE3-AC75-4D58-8560-5DB8AA80D77D)
// IIdleSettings         (IID: 84594461-0053-4342-A8FD-088FABF11F32)
// INetworkSettings      (IID: 9F7DEA84-C932-4B19-BD9E-83D75F8A82A3)

public unsafe partial struct IRunningTask { public void** lpVtbl; }
public unsafe partial struct IRunningTaskCollection { public void** lpVtbl; }
public unsafe partial struct ITaskFolderCollection { public void** lpVtbl; }
public unsafe partial struct IRegisteredTaskCollection { public void** lpVtbl; }
public unsafe partial struct IRepetitionPattern { public void** lpVtbl; }
public unsafe partial struct IIdleSettings { public void** lpVtbl; }
public unsafe partial struct INetworkSettings { public void** lpVtbl; }