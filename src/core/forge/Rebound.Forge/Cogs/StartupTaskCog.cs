// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System.Xml.Linq;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.TaskScheduler;

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
            if (!TryGetTaskService(out var service))
                return Task.FromResult(new CogOperationResult(
                    false, "TASK_SERVICE_NOT_CREATED", false));

            using (service)
            {
                if (!TryGetOrCreateReboundFolder(service, out var folder))
                    return Task.FromResult(new CogOperationResult(
                        false, "FOLDER_NOT_CREATED", false));

                using (folder)
                {
                    using ComPtr<ITaskDefinition> taskDef = default;
                    HRESULT hr = service.Get()->NewTask(
                        0, taskDef.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "TASK_DEFINITION_NOT_CREATED", false));

                    // Registration information.
                    using ComPtr<IRegistrationInfo> registration = default;
                    hr = taskDef.Get()->get_RegistrationInfo(
                        registration.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "REGISTRATION_INFO_FAILED", false));

                    using StringPtr description = TaskDescription;
                    hr = registration.Get()->put_Description(
                        new BSTR(description.GetChars()));

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "DESCRIPTION_FAILED", false));

                    // Settings.
                    using ComPtr<ITaskSettings> settings = default;
                    hr = taskDef.Get()->get_Settings(
                        settings.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "SETTINGS_FAILED", false));

                    hr = settings.Get()->put_DisallowStartIfOnBatteries(false);

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "SETTINGS_UPDATE_FAILED", false));

                    // Principal / run level.
                    using ComPtr<IPrincipal> principal = default;
                    hr = taskDef.Get()->get_Principal(
                        principal.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "PRINCIPAL_FAILED", false));

                    hr = principal.Get()->put_RunLevel(
                        TaskRequiresElevation
                            ? TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_HIGHEST
                            : TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_LUA);

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "RUN_LEVEL_FAILED", false));

                    // Logon trigger.
                    using ComPtr<ITriggerCollection> triggers = default;
                    hr = taskDef.Get()->get_Triggers(
                        triggers.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "TRIGGERS_FAILED", false));

                    using ComPtr<ILogonTrigger> logonTrigger = default;
                    hr = triggers.Get()->Create(
                        TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON,
                        (ITrigger**)logonTrigger.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "LOGON_TRIGGER_FAILED", false));

                    // Executable action.
                    using ComPtr<IActionCollection> actions = default;
                    hr = taskDef.Get()->get_Actions(
                        actions.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "ACTIONS_FAILED", false));

                    using ComPtr<IExecAction> execAction = default;
                    hr = actions.Get()->Create(
                        TASK_ACTION_TYPE.TASK_ACTION_EXEC,
                        (IAction**)execAction.GetAddressOf());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "EXEC_ACTION_FAILED", false));

                    string executablePath;

                    switch (StartupTarget.TargetType)
                    {
                        case StartupTaskTargetType.ExecutablePath:
                            executablePath = StartupTarget.Target;
                            break;

                        case StartupTaskTargetType.PackageFamilyName:
                            // Resolve the package's installed executable
                            // for application ID "App" here.
                            executablePath = ResolvePackageExecutable(
                                StartupTarget.Target, "App");
                            break;

                        default:
                            return Task.FromResult(new CogOperationResult(
                                false, "INVALID_TARGET_TYPE", false));
                    }

                    using StringPtr path = executablePath;
                    hr = execAction.Get()->put_Path(
                        new BSTR(path.GetChars()));

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "EXECUTABLE_PATH_FAILED", false));

                    hr = execAction.Get()->put_Arguments(new BSTR());

                    if (hr.Failed)
                        return Task.FromResult(new CogOperationResult(
                            false, "EXECUTABLE_ARGUMENTS_FAILED", false));

                    // Register task.
                    using StringPtr taskName = TaskName;
                    using ComPtr<IRegisteredTask> registeredTask = default;

                    hr = folder.Get()->RegisterTaskDefinition(
                        new BSTR(taskName.GetChars()),
                        taskDef.Get(),
                        0x6, // TASK_CREATE_OR_UPDATE | TASK_IGNORE_REGISTRATION_TRIGGERS
                        new(),
                        new(),
                        TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                        new(),
                        registeredTask.GetAddressOf());

                    if (hr.Failed)
                    {
                        ReboundLogger.WriteToLog(
                            "StartupTaskCog Apply",
                            $"RegisterTaskDefinition failed. HRESULT=0x{hr.Value:X}",
                            LogMessageSeverity.Error);

                        return Task.FromResult(new CogOperationResult(
                            false, "TASK_REGISTRATION_FAILED", false));
                    }
                }
            }

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog Apply",
                "Failed to apply startup task.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogOperationResult(
                false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetTaskService(out var service))
                return Task.FromResult(new CogOperationResult(
                    false, "TASK_SERVICE_NOT_CREATED", false));

            using (service)
            {
                using StringPtr folderName = "Rebound";
                using StringPtr taskName = TaskName;
                using ComPtr<ITaskFolder> folder = default;

                HRESULT hr = service.Get()->GetFolder(
                    new BSTR(folderName.GetChars()),
                    folder.GetAddressOf());

                if (hr.Failed || folder.Get() is null)
                    return Task.FromResult(new CogOperationResult(
                        false, "TASK_FOLDER_NOT_FOUND", false));

                hr = folder.Get()->DeleteTask(
                    new BSTR(taskName.GetChars()), 0);

                if (hr.Failed)
                {
                    ReboundLogger.WriteToLog(
                        "StartupTaskCog Remove",
                        $"Failed to delete task '{TaskName}'. HRESULT=0x{hr.Value:X}",
                        LogMessageSeverity.Error);

                    return Task.FromResult(new CogOperationResult(
                        false, "TASK_DELETE_FAILED", false));
                }
            }

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog Remove",
                "Failed to remove startup task.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogOperationResult(
                false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public unsafe Task<CogStatus> GetStatusAsync()
    {
        try
        {
            if (!TryGetTaskService(out var service))
                return Task.FromResult(new CogStatus(
                    CogState.Unknown, "Could not create Task Scheduler service."));

            using (service)
            {
                using StringPtr folderName = "Rebound";
                using StringPtr taskName = TaskName;
                using ComPtr<ITaskFolder> folder = default;

                HRESULT hr = service.Get()->GetFolder(
                    new BSTR(folderName.GetChars()),
                    folder.GetAddressOf());

                if (hr.Failed || folder.Get() is null)
                    return Task.FromResult(new CogStatus(
                        CogState.NotInstalled, "Rebound folder not found."));

                using ComPtr<IRegisteredTask> registeredTask = default;
                hr = folder.Get()->GetTask(
                    new BSTR(taskName.GetChars()),
                    registeredTask.GetAddressOf());

                if (hr.Failed || registeredTask.Get() is null)
                    return Task.FromResult(new CogStatus(
                        CogState.NotInstalled, $"Task '{TaskName}' not found."));

                hr = registeredTask.Get()->get_Enabled(out var enabled);

                if (hr.Failed)
                    return Task.FromResult(new CogStatus(
                        CogState.Unknown, "Failed to read task enabled state."));

                if (enabled == 0)
                    return Task.FromResult(new CogStatus(
                        CogState.NotInstalled, "Task is disabled."));

                using ComPtr<ITaskDefinition> taskDef = default;
                hr = registeredTask.Get()->get_Definition(
                    taskDef.GetAddressOf());

                if (hr.Failed)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "Failed to read task definition."));

                using ComPtr<IActionCollection> actions = default;
                hr = taskDef.Get()->get_Actions(actions.GetAddressOf());

                if (hr.Failed)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "Failed to read task actions."));

                int count = 0;
                hr = actions.Get()->get_Count(&count);

                if (hr.Failed || count < 1)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "No task actions found."));

                using ComPtr<IAction> action = default;
                hr = actions.Get()->get_Item(1, action.GetAddressOf());

                if (hr.Failed || action.Get() is null)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "Failed to read task action."));

                hr = action.Get()->QueryInterface<IExecAction>(out var execAction);

                if (hr.Failed || execAction is null)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "Action is not an executable action."));

                BSTR pathPtr = default;
                hr = execAction->get_Path(ref pathPtr);

                if (hr.Failed)
                    return Task.FromResult(new CogStatus(
                        CogState.PartiallyInstalled, "Failed to read executable path."));

                string actualPath = new string((char*)pathPtr);

                string expectedPath;

                switch (StartupTarget.TargetType)
                {
                    case StartupTaskTargetType.ExecutablePath:
                        expectedPath = StartupTarget.Target;
                        break;

                    case StartupTaskTargetType.PackageFamilyName:
                        expectedPath = ResolvePackageExecutable(
                            StartupTarget.Target, "App");
                        break;

                    default:
                        return Task.FromResult(new CogStatus(
                            CogState.PartiallyInstalled, "Invalid target type."));
                }

                bool valid = string.Equals(
                    actualPath,
                    expectedPath,
                    StringComparison.OrdinalIgnoreCase);

                return Task.FromResult(valid
                    ? new CogStatus(CogState.Installed, null)
                    : new CogStatus(
                        CogState.PartiallyInstalled,
                        "Task executable path does not match."));
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog GetStatus",
                "Failed to check startup task status.",
                LogMessageSeverity.Error,
                ex);

            return Task.FromResult(new CogStatus(
                CogState.Unknown, "Exception during status check."));
        }
    }

    #region Native wrappers

    private unsafe bool TryGetTaskService(out ComPtr<ITaskService> service)
    {
        service = default;

        using ObjectPtr<Guid> iid = ITaskService.IID_Guid;
        Guid clsid = Guid.Parse(
            "0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

        HRESULT hr = PInvoke.CoCreateInstance(
            in clsid,
            null,
            CLSCTX.CLSCTX_INPROC_SERVER,
            out ITaskService* taskService);

        if (hr.Failed || taskService is null)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetTaskService",
                $"CoCreateInstance failed. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);

            return false;
        }

        service.Attach(taskService);

        hr = service.Get()->Connect(new(), new(), new(), new());

        if (hr.Failed)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetTaskService",
                $"Task Scheduler Connect failed. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);

            service.Dispose();
            service = default;
            return false;
        }

        return true;
    }

    private unsafe bool TryGetOrCreateReboundFolder(ComPtr<ITaskService> service, out ComPtr<ITaskFolder> reboundFolder)
    {
        reboundFolder = default;

        using StringPtr rootName = "\\";
        using StringPtr folderName = "Rebound";

        HRESULT hr = service.Get()->GetFolder(
            new BSTR(folderName.GetChars()),
            reboundFolder.GetAddressOf());

        if (hr.Succeeded && reboundFolder.Get() is not null)
            return true;

        using ComPtr<ITaskFolder> rootFolder = default;

        hr = service.Get()->GetFolder(
            new BSTR(rootName.GetChars()),
            rootFolder.GetAddressOf());

        if (hr.Failed || rootFolder.Get() is null)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetOrCreateReboundFolder",
                $"Failed to get root folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);

            return false;
        }

        hr = rootFolder.Get()->CreateFolder(
            new BSTR(folderName.GetChars()),
            new(),
            reboundFolder.GetAddressOf());

        if (hr.Failed || reboundFolder.Get() is null)
        {
            ReboundLogger.WriteToLog(
                "StartupTaskCog TryGetOrCreateReboundFolder",
                $"Failed to create Rebound folder. HRESULT=0x{hr.Value:X}",
                LogMessageSeverity.Error);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves the executable declared by an installed MSIX application's manifest.
    /// </summary>
    /// <param name="packageFamilyName">The package family name.</param>
    /// <param name="applicationId">The application ID declared in the package manifest.</param>
    /// <returns>The absolute path to the application's executable.</returns>
    /// <exception cref="InvalidOperationException">
    /// The package or application executable could not be resolved.
    /// </exception>
    private static string ResolvePackageExecutable(string packageFamilyName, string applicationId = "App")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFamilyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        var packageManager = new PackageManager();

        var packages = packageManager.FindPackagesForUser(
            string.Empty,
            packageFamilyName);

        foreach (var package in packages)
        {
            string installPath = package.InstalledLocation.Path;
            string manifestPath = Path.Combine(
                installPath,
                "AppxManifest.xml");

            if (!File.Exists(manifestPath))
                continue;

            XDocument manifest;

            try
            {
                manifest = XDocument.Load(manifestPath);
            }
            catch (System.Xml.XmlException)
            {
                continue;
            }

            XElement? root = manifest.Root;

            if (root is null)
                continue;

            XNamespace ns = root.Name.Namespace;

            XElement? application = root
                .Element(ns + "Applications")?
                .Elements(ns + "Application")
                .FirstOrDefault(element =>
                    string.Equals(
                        (string?)element.Attribute("Id"),
                        applicationId,
                        StringComparison.OrdinalIgnoreCase));

            string? executable = (string?)application?
                .Attribute("Executable");

            if (string.IsNullOrWhiteSpace(executable))
                continue;

            string executablePath = Path.GetFullPath(
                Path.Combine(installPath, executable));

            if (File.Exists(executablePath))
                return executablePath;
        }

        throw new InvalidOperationException(
            $"Could not resolve executable for application '{applicationId}' " +
            $"in package '{packageFamilyName}'.");
    }

    #endregion
}