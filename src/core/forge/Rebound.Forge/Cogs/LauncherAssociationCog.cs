// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using System.Collections.ObjectModel;
using System.Xml;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Cogs;

/// <summary>
/// Represents a target for a launcher operation, including its identifier and associated stub information.
/// </summary>
/// <param name="Value">
/// The unique identifier or value representing the launcher target.
/// </param>
/// <param name="TargetStub">
/// The stub information associated with the launcher target. Provides additional metadata or configuration for the
/// target.
/// </param>
public record LauncherTarget(string Value, LauncherTargetStub TargetStub);

/// <summary>
/// Represents a base class for defining a target that can be launched with optional argument overrides.
/// </summary>
public abstract class LauncherTargetStub
{
    /// <summary>
    /// Gets or sets the command-line argument value that overrides the default setting.
    /// </summary>
    /// <remarks>
    /// To be used when a different argument is meant to be passed to an app in order to
    /// trigger specialized functionality. For example, if `winver.exe` is pointed at Rebound Control Panel,
    /// the default argument would simply open the control panel's home page, so we override it with
    /// `winver` to tell the control panel to show the "About" page instead.
    /// </remarks>
    public string? ArgumentOverride { get; set; }
}

/// <summary>
/// Represents a launcher target that specifies a package by its family name and entry point.
/// </summary>
public class LauncherTargetPackage : LauncherTargetStub
{
    /// <summary>
    /// The family name of the target package. For example: "Microsoft.WindowsCalculator_8wekyb3d8bbwe".
    /// </summary>
    public string FamilyName { get; set; } = string.Empty;

    /// <summary>
    /// The entry point of the package to launch (the string after "!" in a package URI). For example: "App" for the calculator's main application.
    /// </summary>
    public string EntryPoint { get; set; } = "App";
}

/// <summary>
/// Represents the launcher target that specifies an executable by its file path.
/// </summary>
public class LauncherTargetExecutable : LauncherTargetStub
{
    /// <summary>
    /// The path to a local executable to be launched.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a cog that manages launcher associations, enabling configuration and management of executable launch
/// targets through XML-based association files.
/// </summary>
/// <remarks>
/// This class provides functionality to apply, remove, and query the status of launcher associations for
/// Rebound Launcher to handle. Every file managed by this cog will be read by the launcher and interpreted
/// into launch instructions that can point to either a package or an executable, with optional argument overrides. This allows
/// Rebound apps to redirect launch by using a centralized executable instead of relying on a stub for each individual package.
/// </remarks>
public class LauncherAssociationCog : ICog
{
    /// <inheritdoc/>
    public required string CogName { get; set; }

    /// <inheritdoc/>
    public required Guid CogId { get; set; }

    /// <summary>
    /// Requires elevation to register files in the ProgramData folder, which is necessary for Rebound Launcher to read them on startup before the user logs in.
    /// </summary>
    public bool RequiresElevation { get; } = true;

    /// <inheritdoc/>
    public string CogDescription { get => $"Configure launcher association for {OriginalExecutable} with {Targets.Count} targets."; }

    /// <summary>
    /// Gets or sets the name of the configuration file to be used.
    /// </summary>
    public required string ConfigurationFileName { get; set; }

    /// <summary>
    /// The file name of the original executable that the association is for.
    /// </summary>
    public required string OriginalExecutable { get; set; }

    /// <summary>
    /// Gets or sets the collection of launcher targets associated with the original executable.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public required Collection<LauncherTarget> Targets { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    /// <inheritdoc/>
    public Task<CogOperationResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog Apply",
                $"Applying launcher association for {OriginalExecutable} with {Targets.Count} targets.");

            var document = new XmlDocument();

            // Why is the abstraction for XML like this
            var root = document.CreateElement("LauncherAssociation");
            document.AppendChild(root);

            // LauncherAssociation > ConfigurationFileName
            SetAttribute(
                document,
                root,
                "ConfigurationFileName",
                ConfigurationFileName);

            // LauncherAssociation > OriginalExecutable
            SetAttribute(
                document,
                root,
                "OriginalExecutable",
                OriginalExecutable);

            var targetsElement = document.CreateElement("LauncherTargets");
            root.AppendChild(targetsElement);

            foreach (var target in Targets)
            {
                var targetElement = CreateElement(
                    document, 
                    "LauncherTarget",
                    targetsElement);

                // LauncherAssociation > LauncherTargets > LauncherTarget (Value)
                SetAttribute(
                    document,
                    targetElement,
                    "Value",
                    target.Value);

                switch (target.TargetStub)
                {
                    case LauncherTargetPackage pkg:
                        {
                            var packageElement = CreateElement(
                                document,
                                "Package",
                                targetElement);

                            SetAttribute(
                                document,
                                packageElement,
                                "FamilyName", 
                                pkg.FamilyName);
                            SetAttribute(
                                document,
                                packageElement, 
                                "EntryPoint", 
                                pkg.EntryPoint);

                            if (!string.IsNullOrEmpty(pkg.ArgumentOverride))
                                SetAttribute(
                                    document,
                                    packageElement,
                                    "ArgumentOverride",
                                    pkg.ArgumentOverride);

                            break;
                        }

                    case LauncherTargetExecutable exe:
                        {
                            var exeElement = CreateElement(
                                document, 
                                "Executable", 
                                targetElement);

                            SetAttribute(
                                document, 
                                exeElement, 
                                "Path", 
                                exe.ExecutablePath);

                            if (!string.IsNullOrEmpty(exe.ArgumentOverride))
                                SetAttribute(
                                    document, 
                                    exeElement, 
                                    "ArgumentOverride",
                                    exe.ArgumentOverride);

                            break;
                        }
                }
            }

            var fileName = Path.GetFileNameWithoutExtension(OriginalExecutable);
            var path = Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{fileName}.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            document.Save(path);

            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog Apply",
                $"Failed to apply launcher association for {OriginalExecutable}: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, ex.Message, false));
        }
    }

    /// <inheritdoc/>
    public Task<CogOperationResult> RemoveAsync(CancellationToken cancellationToken)
    {
        try
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog Remove",
                $"Removing launcher association for {OriginalExecutable}.");

            var fileName = Path.GetFileNameWithoutExtension(OriginalExecutable);
            File.Delete(Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{fileName}.xml"));

            ReboundLogger.WriteToLog(
                "LauncherAssociationCog Remove",
                $"Successfully removed launcher association for {OriginalExecutable}.");
            return Task.FromResult(new CogOperationResult(true, null, true));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog Remove",
                $"Failed to remove launcher association for {OriginalExecutable}: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogOperationResult(false, "EXCEPTION", false));
        }
    }

    /// <inheritdoc/>
    public Task<CogStatus> GetStatusAsync()
    {
        try
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog GetStatus",
                $"Getting status for launcher association of {OriginalExecutable}.");

            var fileName = Path.GetFileNameWithoutExtension(OriginalExecutable);
            var path = Path.Combine(Variables.ReboundLauncherAssociationsFolder, $"{fileName}.xml");
            var exists = File.Exists(path);

            ReboundLogger.WriteToLog(
                "LauncherAssociationCog GetStatus",
                $"Launcher association for {OriginalExecutable} is {(exists ? "installed" : "not installed")}.");
            return Task.FromResult(new CogStatus(exists ? CogState.Installed : CogState.NotInstalled, exists ? null : "Not installed."));
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "LauncherAssociationCog GetStatus",
                $"Failed to get status for launcher association of {OriginalExecutable}: {ex.Message}",
                LogMessageSeverity.Error,
                ex);
            return Task.FromResult(new CogStatus(CogState.Unknown, "EXCEPTION"));
        }
    }

    // Without these the code just looks ugly
    #region XML helpers
    private static XmlElement CreateElement(XmlDocument document, string name, XmlNode? parent = null)
    {
        var element = document.CreateElement(name);
        parent?.AppendChild(element);
        return element;
    }

    private static void SetAttribute(XmlDocument doc, XmlElement element, string name, string value)
    {
        var attr = doc.CreateAttribute(name);
        attr.Value = value;
        element.Attributes.Append(attr);
    }

    #endregion
}