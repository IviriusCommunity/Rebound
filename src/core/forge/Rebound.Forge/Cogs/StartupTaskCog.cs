// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.TaskScheduler.Native;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Specifies whether the target is an executable path or an application package family name.
/// </summary>
public enum StartupTaskTargetType
{
    /// <summary>
    /// Target is an executable file.
    /// </summary>
    ExecutablePath,
    /// <summary>
    /// Target is an application package.
    /// </summary>
    PackageFamilyName
}

/// <summary>
/// Represents the startup target of the corresponding startup task operation.
/// </summary>
/// <param name="Target">
/// The target path itself. Can be an executable path or a package family name.
/// </param>
/// <param name="TargetType">
/// The type of the target path.
/// </param>
public record StartupTaskTarget(string Target, StartupTaskTargetType TargetType);

/// <summary>
/// Creates a startup task inside the Rebound folder in Task Scheduler to launch a package.
/// </summary>
public class StartupTaskCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public string CogDescription { get => $"Create startup task '{TaskName}' to launch {(StartupTarget.TargetType == StartupTaskTargetType.ExecutablePath ? "executable" : "package")} {StartupTarget.Target}."; }

    /// <summary>
    /// Task Scheduler APIs always require elevation.
    /// </summary>
    public bool RequiresElevation => true;

    /// <summary>
    /// The startup target. Either a full executable path or a package family name.
    /// </summary>
    public required StartupTaskTarget StartupTarget { get; set; }

    /// <summary>
    /// The name of the task in Task Scheduler.
    /// </summary>
    public required string TaskName { get; set; }

    /// <summary>
    /// The description of the task in Task Scheduler.
    /// </summary>
    public required string TaskDescription { get; set; }

    /// <summary>
    /// Gets or sets whether the task requires elevation. 
    /// If true, the task will be registered with the highest run level, and will trigger a UAC prompt on startup. 
    /// If false, the task will run with the same privileges as the current user.
    /// </summary>
    public required bool TaskRequiresElevation { get; set; }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetTaskService(out var taskService))
            {
                ReboundLogger.WriteToLog(
                    "StartupTaskCog Apply",
                    "Failed to create Task Scheduler service.",
                    LogMessageSeverity.Error);
                return Task.FromResult(new CogOperationResult(false, "Failed to create Task Scheduler service.", false));
            }

            using (taskService)
            {
                if (!TryGetOrCreateReboundFolder(taskService, out var reboundFolder))
                    return Task.FromResult(new CogOperationResult(false, "Failed to create or get Rebound folder.", false));

                using (reboundFolder)
                {
                    using ComPtr<ITaskDefinition> taskDef = default;
                    taskService.Get()->NewTask(0, taskDef.GetAddressOf());

                    // Registration info
                    using ComPtr<IRegistrationInfo> registrationInfo = default;
                    taskDef.Get()->get_RegistrationInfo(registrationInfo.GetAddressOf());
                    using ManagedPtr<ushort> description = TaskDescription;
                    registrationInfo.Get()->put_Description(description);

                    // Settings
                    using ComPtr<ITaskSettings> settings = default;
                    taskDef.Get()->get_Settings(settings.GetAddressOf());
                    settings.Get()->put_DisallowStartIfOnBatteries(false);

                    // Principal / elevation
                    using ComPtr<IPrincipal> principal = default;
                    taskDef.Get()->get_Principal(principal.GetAddressOf());
                    principal.Get()->put_RunLevel(TaskRequiresElevation
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
                    switch (StartupTarget.TargetType)
                    {
                        case StartupTaskTargetType.ExecutablePath:
                            {
                                using ManagedPtr<ushort> pszCommand = StartupTarget.Target;
                                action.Get()->put_Path(pszCommand);
                                action.Get()->put_Arguments(null);
                                break;
                            }

                        case StartupTaskTargetType.PackageFamilyName:
                            {
                                using ManagedPtr<ushort> pszCommand = Variables.ReboundLauncherPath;
                                using ManagedPtr<ushort> pszArguments = $"--launchPackage {StartupTarget.Target}!App";

                                action.Get()->put_Path(pszCommand);
                                action.Get()->put_Arguments(pszArguments);
                                break;
                            }

                        default:
                            return Task.FromResult(new CogOperationResult(false, "INVALID_TARGET_TYPE", false));
                    }

                    // Register
                    using ComPtr<IRegisteredTask> registeredTask = default;
                    using ManagedPtr<ushort> pszName = TaskName;
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
                "StartupTaskCog Apply",
                "An exception occurred while applying the startup task cog.",
                LogMessageSeverity.Error,
                ex);
        }

        return Task.FromResult(new CogOperationResult(true, null, true));
    }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetTaskService(out var taskService))
                return Task.FromResult(new CogOperationResult(false, "Failed to get task service.", false));

            using (taskService)
            {
                using ComPtr<ITaskFolder> reboundFolder = default;
                using ManagedPtr<ushort> pszRebound = "Rebound";
                var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

                if (hr.FAILED || reboundFolder.Get() is null)
                    return Task.FromResult(new CogOperationResult(false, "Failed to get rebound folder.", false));

                using ManagedPtr<ushort> pszName = TaskName;
                hr = reboundFolder.Get()->DeleteTask(pszName, new());

                if (hr.FAILED)
                {
                    ReboundLogger.WriteToLog(
                        "StartupTaskCog Remove",
                        $"Failed to delete task '{TaskName}'. HRESULT=0x{hr.Value:X}",
                        LogMessageSeverity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog Remove",
                "An exception occurred while removing the startup task cog.",
                LogMessageSeverity.Error,
                ex);
        }

        return Task.FromResult(new CogOperationResult(true, null, true));
    }

    /// <inheritdoc/>
    public unsafe Task<CogStatus> GetStatusAsync()
    {
        try
        {
            if (!TryGetTaskService(out var taskService))
                return Task.FromResult(new CogStatus(CogState.Unknown, "Couldn't access Task Scheduler service."));

            using (taskService)
            {
                using ComPtr<ITaskFolder> reboundFolder = default;
                using ManagedPtr<ushort> pszRebound = "Rebound";

                var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr.FAILED || reboundFolder.Get() is null)
                    return Task.FromResult(new CogStatus(CogState.NotInstalled, "Rebound folder not found."));

                using ComPtr<IRegisteredTask> registeredTask = default;
                using ManagedPtr<ushort> pszName = TaskName;

                hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());
                if (hr.FAILED || registeredTask.Get() is null)
                    return Task.FromResult(new CogStatus(CogState.NotInstalled, $"Task '{TaskName}' not found."));

                // Enabled state
                BOOL enabled;
                registeredTask.Get()->get_Enabled(&enabled);
                if (!(bool)enabled)
                    return Task.FromResult(new CogStatus(CogState.NotInstalled, "Task is disabled."));

                // Pull definition
                using ComPtr<ITaskDefinition> taskDef = default;
                registeredTask.Get()->get_Definition(taskDef.GetAddressOf());

                using ComPtr<IActionCollection> actions = default;
                taskDef.Get()->get_Actions(actions.GetAddressOf());

                int count;
                if (actions.Get()->get_Count(&count).FAILED)
                    return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "Failed to read actions."));

                if (count < 1)
                    return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "No actions found."));

                ComPtr<IAction> actionPtr = default;
                actions.Get()->get_Item(1, actionPtr.GetAddressOf());

                if (actions.Get()->get_Item(1, actionPtr.GetAddressOf()).FAILED || actionPtr.Get() is null)
                    return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "No action found."));

                using ComPtr<IExecAction> execAction = default;
                if (actionPtr.Get()->QueryInterface(IExecAction.IID, (void**)execAction.GetAddressOf()).FAILED)
                    return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "Action is not an exec action."));

                ushort* pathPtr;
                ushort* argsPtr;

                execAction.Get()->get_Path(&pathPtr);
                execAction.Get()->get_Arguments(&argsPtr);

                string actualPath = pathPtr != null ? new string((char*)pathPtr) : string.Empty;
                string actualArgs = argsPtr != null ? new string((char*)argsPtr) : string.Empty;

                if (pathPtr != null) CoTaskMemFree(pathPtr);
                if (argsPtr != null) CoTaskMemFree(argsPtr);

                bool valid = StartupTarget.TargetType switch
                {
                    StartupTaskTargetType.ExecutablePath =>
                        string.Equals(actualPath, StartupTarget.Target, StringComparison.OrdinalIgnoreCase),

                    StartupTaskTargetType.PackageFamilyName =>
                        string.Equals(actualPath, Variables.ReboundLauncherPath, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(actualArgs, $"--launchPackage {StartupTarget.Target}!App", StringComparison.Ordinal),

                    _ => false
                };

                if (!valid)
                    return Task.FromResult(new CogStatus(CogState.PartiallyInstalled, "Task configuration mismatch."));

                return Task.FromResult(new CogStatus(CogState.Installed, null));
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog GetStatus",
                "An exception occurred while checking the startup task cog.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogStatus(CogState.Unknown, "Exception during status check."));
        }
    }

    #region Native wrappers

    private unsafe bool TryGetTaskService(out ComPtr<ITaskService> taskService)
    {
        taskService = default;

        HRESULT hr;
        using ManagedPtr<Guid> iid = *ITaskService.IID;
        using ManagedPtr<Guid> clsid = new("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");
        hr = CoCreateInstance(
            clsid,
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            iid,
            (void**)taskService.GetAddressOf());

        if (hr.FAILED || taskService.Get() is null)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetTaskService",
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

        using ManagedPtr<ushort> pszRoot = "\\";
        using ManagedPtr<ushort> pszRebound = "Rebound";

        var hr = taskService.Get()->GetFolder(pszRebound, reboundFolder.GetAddressOf());

        if (hr.SUCCEEDED && reboundFolder.Get() is not null)
            return true;

        using ComPtr<ITaskFolder> rootFolder = default;
        hr = taskService.Get()->GetFolder(pszRoot, rootFolder.GetAddressOf());

        if (hr.FAILED)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetOrCreateReboundFolder",
                $"Failed to get root Task Scheduler folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);
            return false;
        }

        hr = rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());

        if (hr.FAILED)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetOrCreateReboundFolder",
                $"Failed to create Rebound Task Scheduler folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);
            return false;
        }

        return true;
    }

    #endregion
}