using System;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.TaskScheduler;

namespace Rebound.Forge;

public class StartupTaskInstruction : IReboundAppInstruction
{
    private ComPtr<ITaskService> _taskService = default;

    public required string TargetPath { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required bool RequireAdmin { get; set; }

    public StartupTaskInstruction()
    {

    }

    public unsafe void Apply()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszDescription = PInvoke.SysAllocString(Description);
            using var pszTargetPath = PInvoke.SysAllocString(TargetPath);
            using var pszName = PInvoke.SysAllocString(Name);

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
            }
            catch (InvalidOperationException)
            {
                rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
            }

            // Get the task definition
            using ComPtr<ITaskDefinition> taskDef = null;
            taskService->NewTask(0, taskDef.GetAddressOf());

            // Description
            using ComPtr<IRegistrationInfo> registrationInfo = null;
            taskDef.Get()->get_RegistrationInfo(registrationInfo.GetAddressOf());
            registrationInfo.Get()->put_Description(pszDescription);

            // Run on battery allowed
            using ComPtr<ITaskSettings> settings = null;
            taskDef.Get()->get_Settings(settings.GetAddressOf());
            settings.Get()->put_DisallowStartIfOnBatteries(Windows.Win32.Foundation.VARIANT_BOOL.VARIANT_FALSE);

            // Run with highest privileges
            using ComPtr<IPrincipal> principal = null;
            taskDef.Get()->get_Principal(principal.GetAddressOf());
            principal.Get()->put_RunLevel(RequireAdmin
                ? TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_HIGHEST
                : TASK_RUNLEVEL_TYPE.TASK_RUNLEVEL_LUA);

            // Triggers
            using ComPtr<ITriggerCollection> triggers = null;
            taskDef.Get()->get_Triggers(triggers.GetAddressOf());

            // Set logon trigger
            using ComPtr<ILogonTrigger> trigger = null;
            triggers.Get()->Create(TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON, (ITrigger**)trigger.GetAddressOf());

            // Get actions
            using ComPtr<IActionCollection> actions = null;
            taskDef.Get()->get_Actions(actions.GetAddressOf());

            // Set exec action
            using ComPtr<IExecAction> action = null;
            actions.Get()->Create(TASK_ACTION_TYPE.TASK_ACTION_EXEC, (IAction**)action.GetAddressOf());
            action.Get()->put_Path(pszTargetPath);

            // Register the task definition
            using ComPtr<IRegisteredTask> registeredTask = null;
            reboundFolder.Get()->RegisterTaskDefinition(
                pszName,
                taskDef,
                (int)Microsoft.Win32.TaskScheduler.TaskCreation.CreateOrUpdate,
                new(), // user
                new(), // password
                TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                new(),  // security descriptor
                registeredTask.GetAddressOf()
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error: {ex.Message}");
        }
    }

    public unsafe void Remove()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszName = PInvoke.SysAllocString(Name);

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
            }
            catch (InvalidOperationException)
            {
                rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
            }

            // Check if task exists and delete it
            try
            {
                // Will throw if task does not exist
                using ComPtr<IRegisteredTask> registeredTask = null;
                var hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
                reboundFolder.Get()->DeleteTask(pszName, 0); // 0 = no flags
            }
            catch (InvalidOperationException)
            {
                // Task does not exist, nothing to delete
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error (Remove): {ex.Message}");
        }
    }

    public unsafe bool IsApplied()
    {
        try
        {
            using var pszRoot = PInvoke.SysAllocString("\\");
            using var pszRebound = PInvoke.SysAllocString("Rebound");
            using var pszName = PInvoke.SysAllocString(Name);

            // Get the task scheduler service for the current instance
            var taskService = _taskService.Get();

            // Connect to local Task Scheduler
            taskService->Connect(new(), new(), new(), new());

            // Get the root folder
            using ComPtr<ITaskFolder> rootFolder = null;
            taskService->GetFolder(pszRoot, rootFolder.GetAddressOf());

            // Get the Rebound folder, or create if missing
            using ComPtr<ITaskFolder> reboundFolder = null;
            try
            {
                var hr = taskService->GetFolder(pszRebound, reboundFolder.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
            }
            catch (InvalidOperationException)
            {
                rootFolder.Get()->CreateFolder(pszRebound, new(), reboundFolder.GetAddressOf());
            }

            // Check if task exists and delete it
            try
            {
                // Will throw if task does not exist
                using ComPtr<IRegisteredTask> registeredTask = null;
                var hr = reboundFolder.Get()->GetTask(pszName, registeredTask.GetAddressOf());
                if (hr < 0)
                    throw new InvalidOperationException("Failed to get or create Rebound folder in Task Scheduler.");
                registeredTask.Get()->get_Enabled(out var enabled);
                return enabled;
            }
            catch (InvalidOperationException)
            {
                // Task does not exist, nothing to delete
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsApplied] Task check failed: {ex}");
            return false;
        }
        finally
        {
            PInvoke.CoUninitialize();
        }
    }
}