using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace Rebound.Helpers.Modding;

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

            // Specify the path to the task in Task Scheduler
            var defragFolder = ts.GetFolder(@"Rebound") ?? ts.RootFolder.CreateFolder(@"Rebound");

            // Retrieve the scheduled task
            var task = defragFolder.GetTasks().Exists("Shell") ? defragFolder.GetTasks()["Shell"] : defragFolder.RegisterTaskDefinition(@"Shell", default);

            task.Definition.Triggers.Clear();
            task.Definition.Triggers.Add(Trigger.CreateTrigger(TaskTriggerType.Logon));
            task.Definition.Actions.Clear();
            task.Definition.Actions.Add(Microsoft.Win32.TaskScheduler.Action.CreateAction(TaskActionType.Execute));
            if (task.Definition.Actions[0] is ExecAction execTask)
            {
                execTask.Path = TargetPath;
            }
            task.Enabled = true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
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
            var task = defragFolder.GetTasks().Exists("Shell") ? defragFolder.GetTasks()["Shell"] : defragFolder.RegisterTaskDefinition(@"Rebound\Shell", default);

            task.Enabled = false;
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