using System;
using System.Diagnostics;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Forge;

public class StartupTaskInstruction : IReboundAppInstruction
{
    public required string TargetPath { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required bool RequireAdmin { get; set; }

    public StartupTaskInstruction()
    {

    }

    public void Apply()
    {
        try
        {
            using TaskService ts = new();

            // Create or get "Rebound" folder
            TaskFolder defragFolder;
            try
            {
                defragFolder = ts.GetFolder("Rebound");
            }
            catch
            {
                defragFolder = ts.RootFolder.CreateFolder("Rebound");
            }

            // Create new task definition
            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Description = Description;

            // Allow task to run on battery
            td.Settings.DisallowStartIfOnBatteries = false;

            // Run with highest privileges (admin)
            td.Principal.RunLevel = RequireAdmin ? TaskRunLevel.Highest : TaskRunLevel.LUA;

            // Add Logon trigger
            td.Triggers.Add(new LogonTrigger());

            // Add Exec action
            td.Actions.Add(new ExecAction(TargetPath, null, null));

            // Register the task or overwrite if exists
            defragFolder.RegisterTaskDefinition(Name, td, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Task Scheduler error: {ex.Message}");
        }
    }

    public void Remove()
    {
        try
        {
            using TaskService ts = new();

            // Specify the path to the task in Task Scheduler
            var defragFolder = ts.GetFolder(@"Rebound") ?? ts.RootFolder.CreateFolder(@"Rebound");

            // Retrieve the scheduled task
            if (defragFolder.GetTasks().Exists(Name))
            {
                defragFolder.DeleteTask(Name, false); // Delete the task
            }
            else
            {
                return;
            }
        }
        catch
        {

        }
    }

    public bool IsApplied()
    {
        try
        {
            using TaskService ts = new();

            // Specify the path to the task in Task Scheduler
            var defragFolder = ts.GetFolder(@"Rebound");
            if (defragFolder is null) return false;
            // Retrieve the scheduled task
            if (!defragFolder.GetTasks().Exists(Name)) return false;

            // Retrieve the scheduled task
            var task = defragFolder.GetTasks().Exists(Name) ? defragFolder.GetTasks()[Name] : defragFolder.RegisterTaskDefinition(@$"Rebound\{Name}", default);

            return task.Enabled;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsApplied] Task check failed: {ex}");
            return false;
        }
    }
}