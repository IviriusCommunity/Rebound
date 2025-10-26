// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge;

internal class StartupTaskCog : ICog
{
    public required string TargetPath { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required bool RequireAdmin { get; set; }

    public StartupTaskCog()
    {

    }

    public unsafe void Apply()
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszDescription = PInvoke.SysAllocString(Description);
            using var pszTargetPath = PInvoke.SysAllocString(TargetPath);
            using var pszName = PInvoke.SysAllocString(Name);

            ReboundLogger.Log("[StartupTaskCog] Apply started.");

            // Create ITaskService instance
            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());
            }

            if (hr.Failed || taskService.Get() is null)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                return;
            }

            taskService.Get()->Connect(new(), new(), new(), new());
            ReboundLogger.Log("[StartupTaskCog] Connected to Task Scheduler.");

            // Get root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hr.Failed)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to get root folder. HRESULT=0x{hr.Value:X}");
                return;
            }

            // Get or create Rebound folder
            using ComPtr<ITaskFolder> reboundFolder = null;
            hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());
            if (hr.Failed || reboundFolder.Get() is null)
            {
                ReboundLogger.Log("[StartupTaskCog] Rebound folder not found, attempting to create it.");
                hr = rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
                if (hr.Failed)
                {
                    ReboundLogger.Log($"[StartupTaskCog] Failed to create Rebound folder. HRESULT=0x{hr.Value:X}");
                    return;
                }
                ReboundLogger.Log("[StartupTaskCog] Successfully created Rebound folder.");
            }

            // Create task definition
            using ComPtr<ITaskDefinition> taskDef = null;
            taskService.Get()->NewTask(0, taskDef.GetAddressOf());

            using ComPtr<IRegistrationInfo> registrationInfo = null;
            taskDef.Get()->get_RegistrationInfo(registrationInfo.GetAddressOf());
            registrationInfo.Get()->put_Description(pszDescription);

            using ComPtr<ITaskSettings> settings = null;
            taskDef.Get()->get_Settings(settings.GetAddressOf());
            settings.Get()->put_DisallowStartIfOnBatteries(Windows.Win32.Foundation.VARIANT_BOOL.VARIANT_FALSE);

            using ComPtr<IPrincipal> principal = null;
            taskDef.Get()->get_Principal(principal.GetAddressOf());
            principal.Get()->put_RunLevel(RequireAdmin
                ? TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_HIGHEST
                : TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_LUA);

            using ComPtr<ITriggerCollection> triggers = null;
            taskDef.Get()->get_Triggers(triggers.GetAddressOf());

            using ComPtr<ILogonTrigger> trigger = null;
            triggers.Get()->Create(TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON, (ITrigger**)trigger.GetAddressOf());

            using ComPtr<IActionCollection> actions = null;
            taskDef.Get()->get_Actions(actions.GetAddressOf());

            using ComPtr<IExecAction> action = null;
            actions.Get()->Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, (IAction**)action.GetAddressOf());
            action.Get()->put_Path(pszTargetPath);

            using ComPtr<IRegisteredTask> registeredTask = null;
            reboundFolder.Get()->RegisterTaskDefinition(
                pszName,
                taskDef,
                (int)Microsoft.Win32.TaskScheduler.TaskCreation.CreateOrUpdate,
                new(),
                new(),
                TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                new(),
                registeredTask.GetAddressOf()
            );

            ReboundLogger.Log("[StartupTaskCog] Task successfully applied.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[StartupTaskCog] Apply failed with exception.", ex);
        }
    }

    public unsafe void Remove()
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszName = PInvoke.SysAllocString(Name);

            ReboundLogger.Log("[StartupTaskCog] Remove started.");

            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());
            }

            if (hr.Failed || taskService.Get() is null)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                return;
            }

            taskService.Get()->Connect(new(), new(), new(), new());
            ReboundLogger.Log("[StartupTaskCog] Connected to Task Scheduler.");

            using ComPtr<ITaskFolder> rootFolder = null;
            hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hr.Failed)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to get root folder. HRESULT=0x{hr.Value:X}");
                return;
            }

            using ComPtr<ITaskFolder> reboundFolder = null;
            hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());
            if (hr.Failed || reboundFolder.Get() is null)
            {
                ReboundLogger.Log("[StartupTaskCog] Rebound folder not found, nothing to remove.");
                return;
            }

            using ComPtr<IRegisteredTask> registeredTask = null;
            hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());
            if (hr.Failed || registeredTask.Get() is null)
            {
                ReboundLogger.Log("[StartupTaskCog] Task does not exist, nothing to remove.");
                return;
            }

            reboundFolder.Get()->DeleteTask(pszName, 0);
            ReboundLogger.Log("[StartupTaskCog] Task successfully removed.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[StartupTaskCog] Remove failed with exception.", ex);
        }
    }

    public unsafe bool IsApplied()
    {
        try
        {
            HRESULT hr;
            using ComPtr<ITaskService> taskService = default;

            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszName = PInvoke.SysAllocString(Name);

            // Create ITaskService instance
            fixed (Guid* iidITaskService = &ITaskService.IID_Guid)
            {
                hr = PInvoke.CoCreateInstance(
                    CLSID.CLSID_TaskScheduler,
                    null,
                    CLSCTX.CLSCTX_INPROC_SERVER,
                    iidITaskService,
                    (void**)taskService.GetAddressOf());
            }

            if (hr.Failed || taskService.Get() is null)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to create ITaskService. HRESULT=0x{hr.Value:X}");
                return false;
            }

            // Connect to local Task Scheduler
            taskService.Get()->Connect(new(), new(), new(), new());
            ReboundLogger.Log("[StartupTaskCog] Connected to Task Scheduler.");

            // Get root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());
            if (hr.Failed)
            {
                ReboundLogger.Log($"[StartupTaskCog] Failed to get root folder. HRESULT=0x{hr.Value:X}");
                return false;
            }

            // Get or create Rebound folder
            using ComPtr<ITaskFolder> reboundFolder = null;
            hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());
            if (hr.Failed || reboundFolder.Get() is null)
            {
                ReboundLogger.Log("[StartupTaskCog] Rebound folder not found, attempting to create it.");
                hr = rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
                if (hr.Failed)
                {
                    ReboundLogger.Log($"[IsApplied] Failed to create Rebound folder. HRESULT=0x{hr.Value:X}");
                    return false;
                }
                ReboundLogger.Log("[StartupTaskCog] Successfully created Rebound folder.");
            }

            // Check if task exists
            using ComPtr<IRegisteredTask> registeredTask = null;
            hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());
            if (hr.Failed || registeredTask.Get() is null)
            {
                ReboundLogger.Log("[StartupTaskCog] Task does not exist.");
                return false;
            }

            registeredTask.Get()->get_Enabled(out var enabled);
            ReboundLogger.Log($"[StartupTaskCog] Task '{Name}' exists. Enabled={enabled}");
            return enabled;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[StartupTaskCog] Task check failed with exception.", ex);
            return false;
        }
    }
}