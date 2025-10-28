// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rebound.Forge.Cogs;

internal class StartupTaskCog : ICog
{
    public required string TargetPath { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool RequireAdmin { get; set; }

    private string TasksFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Windows", "Tasks", "Rebound");

    private string TaskFilePath => Path.Combine(TasksFolder, Name);

    public StartupTaskCog() { }

    public async Task ApplyAsync()
    {
        try
        {
            Directory.CreateDirectory(TasksFolder);

            var runLevel = RequireAdmin ? "HighestAvailable" : "LeastPrivilege";

            var taskXml = new XDocument(
                new XElement(XName.Get("Task", "http://schemas.microsoft.com/windows/2004/02/mit/task"),
                    new XAttribute("version", "1.4"),
                    new XElement("RegistrationInfo",
                        new XElement("Description", Description),
                        new XElement("Author", Environment.UserName)
                    ),
                    new XElement("Triggers",
                        new XElement("LogonTrigger",
                            new XElement("Enabled", "true")
                        )
                    ),
                    new XElement("Principals",
                        new XElement("Principal", new XAttribute("id", "Author"),
                            new XElement("RunLevel", runLevel)
                        )
                    ),
                    new XElement("Settings",
                        new XElement("AllowStartOnDemand", "true"),
                        new XElement("DisallowStartIfOnBatteries", "false"),
                        new XElement("StopIfGoingOnBatteries", "false"),
                        new XElement("MultipleInstancesPolicy", "IgnoreNew"),
                        new XElement("Enabled", "true"),
                        new XElement("Hidden", "false"),
                        new XElement("RunOnlyIfNetworkAvailable", "false"),
                        new XElement("StartWhenAvailable", "true")
                    ),
                    new XElement("Actions", new XAttribute("Context", "Author"),
                        new XElement("Exec",
                            new XElement("Command", TargetPath),
                            new XElement("Arguments", "")
                        )
                    )
                )
            );

            taskXml.Save(TaskFilePath);
            ReboundLogger.Log($"[StartupTaskCog] Task '{Name}' successfully applied.");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[StartupTaskCog] ApplyAsync failed for '{Name}'.", ex);
        }
    }

    public async Task RemoveAsync()
    {
        try
        {
            if (File.Exists(TaskFilePath))
            {
                File.Delete(TaskFilePath);
                ReboundLogger.Log($"[StartupTaskCog] Task '{Name}' successfully removed.");
            }
            else
            {
                ReboundLogger.Log($"[StartupTaskCog] Task '{Name}' does not exist, nothing to remove.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[StartupTaskCog] RemoveAsync failed for '{Name}'.", ex);
        }
    }

    public async Task<bool> IsAppliedAsync()
    {
        bool exists = File.Exists(TaskFilePath);
        ReboundLogger.Log($"[StartupTaskCog] Task '{Name}' {(exists ? "exists" : "does not exist")}.");
        return exists;
    }
}