using System;
using System.Diagnostics;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Forge;

public class StartupTaskInstruction : IReboundAppInstruction
{
    public required string TargetPath { get; set; }

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
            td.RegistrationInfo.Description = "Rebound Shell Task";

            // Add Logon trigger
            td.Triggers.Add(new LogonTrigger());

            // Add Exec action
            td.Actions.Add(new ExecAction(TargetPath, null, null));

            // Register the task or overwrite if exists
            defragFolder.RegisterTaskDefinition("Shell", td, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);

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
            if (defragFolder.GetTasks().Exists("Shell"))
            {
                defragFolder.DeleteTask("Shell", false); // Delete the task
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
            if (!defragFolder.GetTasks().Exists("Shell")) return false;

            // Retrieve the scheduled task
            var task = defragFolder.GetTasks().Exists("Shell") ? defragFolder.GetTasks()["Shell"] : defragFolder.RegisterTaskDefinition(@"Rebound\Shell", default);

            return task.Enabled;
        }
        catch
        {
            return false;
        }
    }
}