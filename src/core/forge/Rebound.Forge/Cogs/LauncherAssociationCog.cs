// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.Collections.ObjectModel;
using System.Xml;

namespace Rebound.Forge.Cogs;

public record LauncherTarget(string Value, LauncherTargetStub TargetStub);

public abstract class LauncherTargetStub
{
    public string? ArgumentOverride { get; set; }
}

public class LauncherTargetPackage : LauncherTargetStub
{
    public string FamilyName { get; set; } = string.Empty;

    public string EntryPoint { get; set; } = "App";
}

public class LauncherTargetExecutable : LauncherTargetStub
{
    public string ExecutablePath { get; set; } = string.Empty;
}

public class LauncherAssociationCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <inheritdoc/>
    public required bool RequiresElevation { get; set; }

    /// <inheritdoc/>
    public required string CogDescription { get; set; }

    public required string ConfigurationFileName { get; set; }

    public required string OriginalExecutable { get; set; }

    public required Collection<LauncherTarget> Targets { get; set; } = [];

    /// <inheritdoc/>
    public async Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog("LauncherAssociationCog", $"Applying launcher association for {OriginalExecutable} with {Targets.Count} targets.");

            var document = new XmlDocument();

            // Root
            var root = document.CreateElement("LauncherAssociation");
            document.AppendChild(root);

            // Attribute: ConfigurationFileName
            var configAttr = document.CreateAttribute("ConfigurationFileName");
            configAttr.Value = ConfigurationFileName;
            root.Attributes.Append(configAttr);

            // OriginalExecutable element
            var originalExe = document.CreateElement("OriginalExecutable");
            originalExe.InnerText = OriginalExecutable;
            root.AppendChild(originalExe);

            // LauncherTargets container
            var targetsElement = document.CreateElement("LauncherTargets");
            root.AppendChild(targetsElement);

            foreach (var target in Targets)
            {
                var targetElement = document.CreateElement("LauncherTarget");

                // Value attribute
                var valueAttr = document.CreateAttribute("Value");
                valueAttr.Value = target.Value;
                targetElement.Attributes.Append(valueAttr);

                switch (target.TargetStub)
                {
                    case LauncherTargetPackage pkg:
                        {
                            var packageElement = document.CreateElement("Package");

                            var familyAttr = document.CreateAttribute("FamilyName");
                            familyAttr.Value = pkg.FamilyName;
                            packageElement.Attributes.Append(familyAttr);

                            var entryAttr = document.CreateAttribute("EntryPoint");
                            entryAttr.Value = pkg.EntryPoint;
                            packageElement.Attributes.Append(entryAttr);

                            if (!string.IsNullOrEmpty(pkg.ArgumentOverride))
                            {
                                var argAttr = document.CreateAttribute("ArgumentOverride");
                                argAttr.Value = pkg.ArgumentOverride;
                                packageElement.Attributes.Append(argAttr);
                            }

                            targetElement.AppendChild(packageElement);
                            break;
                        }

                    case LauncherTargetExecutable exe:
                        {
                            var exeElement = document.CreateElement("Executable");

                            var pathAttr = document.CreateAttribute("Path");
                            pathAttr.Value = exe.ExecutablePath;
                            exeElement.Attributes!.Append(pathAttr);

                            if (!string.IsNullOrEmpty(exe.ArgumentOverride))
                            {
                                var argAttr = document.CreateAttribute("ArgumentOverride");
                                argAttr.Value = exe.ArgumentOverride;
                                exeElement.Attributes!.Append(argAttr);
                            }

                            targetElement.AppendChild(exeElement);
                            break;
                        }
                }

                targetsElement.AppendChild(targetElement);
            }

            // Save to the proper folder
            document.Save(Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{OriginalExecutable}.xml"));

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog",
                $"Failed to apply launcher association for {OriginalExecutable}: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return new(false, ex.Message, false);
        }
    }

    public async Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog("LauncherAssociationCog", $"Removing launcher association for {OriginalExecutable}.");

            File.Delete(Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{OriginalExecutable}.xml"));

            return new(true, null, true);
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog",
                $"Failed to remove launcher association for {OriginalExecutable}: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return new(false, "EXCEPTION", false);
        }
    }

    public async Task<CogStatus> GetStatusAsync()
    {
        var path = Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{OriginalExecutable}.xml");
        var exists = File.Exists(path);

        return new CogStatus(exists ? CogState.Installed : CogState.NotInstalled, exists ? null : "Not installed.");
    }
}