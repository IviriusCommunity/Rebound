// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Creates a startup task inside the Rebound folder in Task Scheduler to launch a package.
/// </summary>
public class StartupPackageCog : ICog
{
    /// <summary>
    /// Package Family Name of the MSIX app.
    /// Example: Rebound.Shell_rcz2tbwv5qzb8
    /// </summary>
    public required string TargetPackageFamilyName { get; set; }

    /// <summary>
    /// The name of the task.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The task's description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Whether this task should run elevated or not.
    /// </summary>
    public required bool RequireAdmin { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; }

    /// <inheritdoc/>
    public string TaskDescription => $"Register a startup task for {TargetPackageFamilyName} as {(RequireAdmin ? "administrator" : "user")}";

    /*private unsafe bool TryGetTaskService(out ComPtr<ITaskService> taskService)
    {
        taskService = default;

        HRESULT hr;
        fixed (Guid* iid = &ITaskService.IID_Guid)
        {
            hr = CoCreateInstance(
                CLSID.CLSID_TaskScheduler,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                iid,
                (void**)taskService.GetAddressOf());
        }

        if (hr.FAILED || taskService.Get() is null)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog",
                $"Failed to create ITaskService. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);
            return false;
        }

        taskService.Get()->Connect(new(), new(), new(), new());
        return true;
    }

    private unsafe bool TryGetOrCreateReboundFolder(ComPtr<ITaskService> taskService, out ComPtr<ITaskFolder> reboundFolder)
    {
        reboundFolder = default;

        using var pszRoot = SysAllocString("\\");
        using var pszRebound = SysAllocString("Rebound");

        var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

        if (hr.SUCCEEDED && reboundFolder.Get() is not null)
            return true;

        using ComPtr<ITaskFolder> rootFolder = default;
        hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());

        if (hr.FAILED)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog",
                $"Failed to get root Task Scheduler folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);
            return false;
        }

        hr = rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());

        if (hr.FAILED)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog",
                $"Failed to create Rebound Task Scheduler folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);
            return false;
        }

        return true;
    }*/

    /// <inheritdoc/>
    public unsafe Task ApplyAsync()
    {
        /*try
        {
            if (!TryGetTaskService(out var taskService))
                return Task.CompletedTask;

            using (taskService)
            {
                if (!TryGetOrCreateReboundFolder(taskService, out var reboundFolder))
                    return Task.CompletedTask;

                using (reboundFolder)
                {
                    using ComPtr<ITaskDefinition> taskDef = default;
                    taskService.Get()->NewTask(0, taskDef.GetAddressOf());

                    // Registration info
                    using ComPtr<IRegistrationInfo> registrationInfo = default;
                    taskDef.Get()->get_RegistrationInfo(registrationInfo.GetAddressOf());
                    using var pszDescription = SysAllocString(Description);
                    registrationInfo.Get()->put_Description(pszDescription);

                    // Settings
                    using ComPtr<ITaskSettings> settings = default;
                    taskDef.Get()->get_Settings(settings.GetAddressOf());
                    settings.Get()->put_DisallowStartIfOnBatteries(VARIANT_BOOL.VARIANT_FALSE);

                    // Principal / elevation
                    using ComPtr<IPrincipal> principal = default;
                    taskDef.Get()->get_Principal(principal.GetAddressOf());
                    principal.Get()->put_RunLevel(RequireAdmin
                        ? TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_HIGHEST
                        : TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_LUA);

                    // Logon trigger
                    using ComPtr<ITriggerCollection> triggers = default;
                    taskDef.Get()->get_Triggers(triggers.GetAddressOf());
                    using ComPtr<ILogonTrigger> trigger = default;
                    triggers.Get()->Create(TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON, (ITrigger**)trigger.GetAddressOf());

                    // Exec action
                    using ComPtr<IActionCollection> actions = default;
                    taskDef.Get()->get_Actions(actions.GetAddressOf());
                    using ComPtr<IExecAction> action = default;
                    actions.Get()->Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, (IAction**)action.GetAddressOf());
                    using var pszCommand = SysAllocString(Variables.ReboundLauncherPath);
                    using var pszArguments = SysAllocString($"--launchPackage {TargetPackageFamilyName}!App");
                    action.Get()->put_Path(pszCommand);
                    action.Get()->put_Arguments(pszArguments);

                    // Register
                    using ComPtr<IRegisteredTask> registeredTask = default;
                    using var pszName = SysAllocString(Name);
                    reboundFolder.Get()->RegisterTaskDefinition(
                        pszName,
                        taskDef,
                        0x6,
                        new(), new(),
                        TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                        new(),
                        registeredTask.GetAddressOf());
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog apply",
                "An exception occurred while applying the startup package cog.",
                LogMessageSeverity.Error,
                ex);
        }

        return Task.CompletedTask;*/
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public unsafe Task RemoveAsync()
    {
        /*try
        {
            if (!TryGetTaskService(out var taskService))
                return Task.CompletedTask;

            using (taskService)
            {
                using ComPtr<ITaskFolder> reboundFolder = default;
                using var pszRebound = SysAllocString("Rebound");
                var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

                if (hr.FAILED || reboundFolder.Get() is null)
                    return Task.CompletedTask;

                using var pszName = SysAllocString(Name);
                hr = reboundFolder.Get()->DeleteTask(pszName, 0);

                if (hr.FAILED)
                {
                    ReboundLogger.WriteToLog(
                        "StartupPackageCog remove",
                        $"Failed to delete task '{Name}'. HRESULT=0x{hr.Value:X}",
                        LogMessageSeverity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog remove",
                "An exception occurred while removing the startup package cog.",
                LogMessageSeverity.Error,
                ex);
        }

        return Task.CompletedTask;*/
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public unsafe Task<bool> IsAppliedAsync()
    {
        /*try
        {
            if (!TryGetTaskService(out var taskService))
                return Task.FromResult(false);

            using (taskService)
            {
                using ComPtr<ITaskFolder> reboundFolder = default;
                using var pszRebound = SysAllocString("Rebound");
                var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

                if (hr.FAILED || reboundFolder.Get() is null)
                    return Task.FromResult(false);

                using ComPtr<IRegisteredTask> registeredTask = default;
                using var pszName = SysAllocString(Name);
                hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());

                if (hr.FAILED || registeredTask.Get() is null)
                    return Task.FromResult(false);

                registeredTask.Get()->get_Enabled(out var enabled);
                return Task.FromResult((bool)enabled);
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupPackageCog check",
                "An exception occurred while checking the startup package cog.",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(false);
        }*/
        return Task.FromResult(true);
    }
}