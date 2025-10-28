// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rebound.Forge.Cogs;

internal class StartupPackageCog : ICog
{
    /// <summary>
    /// Package Family Name of the UWP / MSIX app.
    /// Example: Microsoft.MSPaint_8wekyb3d8bbwe
    /// </summary>
    public required string TargetPackageFamilyName { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required bool RequireAdmin { get; set; }

    /// <summary>
    /// Path to the XML task file that will be created.
    /// </summary>
    private string TaskXmlPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft", "Windows", "Start Menu", "Programs", "Startup", $"{Name}.xml"
    );

    public StartupPackageCog() { }

    /// <summary>
    /// Generates the XML string for the task scheduler task.
    /// </summary>
    private string GenerateTaskXml()
    {
        // Arguments: use explorer.exe to launch UWP app
        string command = "explorer.exe";
        string arguments = $"shell:AppsFolder\\{TargetPackageFamilyName}!App";

        // Run level
        string runLevel = RequireAdmin ? "HighestAvailable" : "LeastPrivilege";

        return $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>{System.Security.SecurityElement.Escape(Description)}</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <RunLevel>{runLevel}</RunLevel>
      <LogonType>InteractiveToken</LogonType>
    </Principal>
  </Principals>
  <Settings>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <AllowStartOnDemand>true</AllowStartOnDemand>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{command}</Command>
      <Arguments>{arguments}</Arguments>
    </Exec>
  </Actions>
</Task>";
    }

    /// <summary>
    /// Applies (creates) the scheduled task XML file.
    /// </summary>
    public async Task ApplyAsync()
    {
        try
        {
            string xml = GenerateTaskXml();
            Directory.CreateDirectory(Path.GetDirectoryName(TaskXmlPath)!);
            await File.WriteAllTextAsync(TaskXmlPath, xml, Encoding.Unicode);

            ReboundLogger.Log($"[StartupTaskCog] Task XML created at {TaskXmlPath} for {TargetPackageFamilyName}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[StartupTaskCog] ApplyAsync failed.", ex);
        }
    }

    /// <summary>
    /// Removes the scheduled task XML file.
    /// </summary>
    public async Task RemoveAsync()
    {
        try
        {
            if (File.Exists(TaskXmlPath))
            {
                File.Delete(TaskXmlPath);
                ReboundLogger.Log($"[StartupTaskCog] Task XML removed: {TaskXmlPath}");
            }
            else
            {
                ReboundLogger.Log("[StartupTaskCog] Task XML does not exist, nothing to remove.");
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[StartupTaskCog] RemoveAsync failed.", ex);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the task XML exists.
    /// </summary>
    public async Task<bool> IsAppliedAsync()
    {
        bool exists = File.Exists(TaskXmlPath);
        ReboundLogger.Log($"[StartupTaskCog] Task XML exists: {exists}");
        return await Task.FromResult(exists);
    }
}