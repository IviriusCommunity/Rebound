// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Forge.Cogs;
using System.Collections.ObjectModel;
using System.Xml.Linq;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Forge.Mods;

/// <summary>
/// Helpers to parse sideloaded Rebound mods from XML declaration files.
/// </summary>
/// <remarks>
/// Every cog element in the declaration file must carry a valid <c>&lt;Id&gt;</c> (GUID)
/// and a <c>&lt;CogName&gt;</c>. Any missing or malformed required field causes the
/// entire mod to be rejected — partial loads are never returned.
/// <c>CogDescription</c> is intentionally left empty here; it is auto-generated
/// from each cog's runtime properties by the cog implementations themselves.
/// </remarks>
public static class ModParser
{
    /// <summary>
    /// Parses all mods inside the Rebound sideloaded mods folder.
    /// Mods that fail validation are logged and skipped; the rest are returned.
    /// </summary>
    /// <returns>A list of successfully parsed sideloaded mods.</returns>
#pragma warning disable CA1002
    public static List<Mod> ParseMods()
#pragma warning restore CA1002
    {
        var mods = new List<Mod>();

        try
        {
            if (!Directory.Exists(Variables.ReboundModsFolder))
            {
                ReboundLogger.WriteToLog(
                    "ModParser ParseMods",
                    $"Mods folder does not exist: {Variables.ReboundModsFolder}",
                    LogMessageSeverity.Warning);
                return mods;
            }

            ReboundLogger.WriteToLog(
                "ModParser ParseMods",
                $"Parsing mods in folder: {Variables.ReboundModsFolder}");

            foreach (var modFolder in Directory.GetDirectories(Variables.ReboundModsFolder))
            {
                try
                {
                    ReboundLogger.WriteToLog(
                        "ModParser ParseMods",
                        $"Parsing mod folder: {modFolder}");

                    var mod = Parse(modFolder);
                    mods.Add(mod);

                    ReboundLogger.WriteToLog(
                        "ModParser ParseMods",
                        $"Successfully loaded mod: {mod.Name} ({mod.Id})");
                }
                catch (Exception ex)
                {
                    // One bad mod does not block the rest
                    ReboundLogger.WriteToLog(
                        "ModParser ParseMods",
                        $"Failed to parse mod folder: {modFolder} — mod rejected.",
                        LogMessageSeverity.Error,
                        ex);
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.WriteToLog(
                "ModParser ParseMods",
                "Unexpected error while enumerating mods folder.",
                LogMessageSeverity.Error,
                ex);
        }

        ReboundLogger.WriteToLog(
            "ModParser ParseMods",
            $"Finished parsing mods. Total loaded: {mods.Count}");

        return mods;
    }

    /// <summary>
    /// Parses a single Rebound mod from the given folder.
    /// Throws if any required field is missing, malformed, or if any cog fails validation.
    /// </summary>
    /// <param name="path">Path to the folder containing the mod's <c>declaration.xml</c>.</param>
    /// <returns>The fully parsed <see cref="Mod"/>.</returns>
    /// <exception cref="FileNotFoundException">The declaration file does not exist.</exception>
    /// <exception cref="InvalidOperationException">A required XML element is missing or malformed.</exception>
    public static Mod Parse(string path)
    {
        ReboundLogger.WriteToLog(
            "ModParser Parse",
            $"Parsing mod folder: {path}");

        var declarationFile = Path.Combine(path, "declaration.xml");

        if (!File.Exists(declarationFile))
        {
            throw new FileNotFoundException(
                $"declaration.xml missing in '{path}'.",
                declarationFile);
        }

        XDocument doc = XDocument.Load(declarationFile);
        var modElement = doc.Element("Mod")
            ?? throw new InvalidOperationException($"Root element <Mod> missing in '{declarationFile}'.");

        // <Id> is required on the mod itself
        var modId = RequireGuid(modElement, "Id", declarationFile);

        // <Name> is required
        var modName = modElement.Element("Name")?.Value;
        if (string.IsNullOrWhiteSpace(modName))
            throw new InvalidOperationException($"<Name> is missing or empty in '{declarationFile}'.");

        // Parse cogs — any failure here throws and rejects the whole mod
        var cogs = ParseCogs(modElement, path, declarationFile);

        var settings = ParseSettings(modElement, path, declarationFile);

        var mod = new Mod
        {
            Id = modId,
            Name = modName,
            Description = modElement.Element("Description")?.Value ?? string.Empty,
            Icon = Path.Combine(path, "icon.png"),
            Category = ModCategory.Sideloaded,
            PreferredInstallationTemplate = InstallationTemplate.Extras,
            Variants =
            [
                new ModVariant
                {
                    Name = modName,
                    Id = modId,
                    Cogs = cogs,
                    Settings = settings
                }
            ]
        };

        ReboundLogger.WriteToLog(
            "ModParser Parse",
            $"Finished parsing mod: {mod.Name} ({mod.Id}), cogs: {cogs.Count}");

        return mod;
    }

    private static ObservableCollection<ICog> ParseCogs(XElement modElement, string path, string declarationFile)
    {
        var cogs = new ObservableCollection<ICog>();
        var cogsElement = modElement.Element("Cogs");

        if (cogsElement is null)
        {
            ReboundLogger.WriteToLog(
                "ModParser ParseCogs",
                "No <Cogs> element found. Mod will have no cogs.");
            return cogs;
        }

        foreach (var cogElem in cogsElement.Elements())
        {
            // Every cog must carry a valid <Id> and <CogName>. Missing either → fail the mod.
            var cogId = RequireGuid(cogElem, "Id", declarationFile);
            var cogName = cogElem.Element("CogName")?.Value;

            if (string.IsNullOrWhiteSpace(cogName))
                throw new InvalidOperationException(
                    $"<CogName> is missing or empty on <{cogElem.Name.LocalName}> in '{declarationFile}'.");

            // CogDescription is intentionally left empty — each cog auto-generates it
            // from its own runtime properties. Do not populate it here.
            ICog cog = cogElem.Name.LocalName switch
            {
                "BoolSettingCog" => new BoolSettingCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    SettingsFileName = GetString(cogElem, "SettingsFileName", path),
                    Key = GetString(cogElem, "Key", path),
                    AppliedValue = GetRequiredBool(cogElem, "AppliedValue", declarationFile)
                },

                "DLLInjectionCog" => new DLLInjectionCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    DLLPathx64 = GetString(cogElem, "DLLPathx64", path),
                    DLLPathARM64 = GetString(cogElem, "DLLPathARM64", path),
                    TargetProcesses = GetStringList(cogElem, "TargetProcesses", "Process"),
                    ExcludedProcesses = GetStringList(cogElem, "ExcludedProcesses", "Process")
                },

                "FileCopyCog" => new FileCopyCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    Path = GetString(cogElem, "Path", path),
                    TargetPath = GetString(cogElem, "TargetPath", path),
                    IsDirectory = GetBool(cogElem, "IsDirectory"),
                    RequiresElevation = GetBool(cogElem, "RequiresElevation")
                },

                "FolderCog" => new FolderCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    Path = GetString(cogElem, "Path", path),
                    PersistAfterRemoving = GetBool(cogElem, "PersistAfterRemoving"),
                    RequiresElevation = GetBool(cogElem, "RequiresElevation"),
                    LockBehindAdmin = GetBool(cogElem, "LockBehindAdmin")
                },

                "PackageCog" => ParsePackageCog(cogElem, path, declarationFile),

                "ProcessKillCog" => new ProcessKillCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    ProcessName = GetString(cogElem, "ProcessName", path),
                    RequiresElevation = GetBool(cogElem, "RequiresElevation")
                },

                "ProcessLaunchCog" => new ProcessLaunchCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    RequiresElevation = GetBool(cogElem, "RequiresElevation"),
                    LaunchTarget = cogElem.Element("Executable") is XElement exe
                        ? new ProcessLaunchTarget(
                            Expand(exe.Attribute("Path")?.Value ?? string.Empty, path),
                            ProcessLaunchTargetType.ExecutablePath)
                        : cogElem.Element("Package") is XElement pkg
                            ? new ProcessLaunchTarget(
                                pkg.Attribute("FamilyName")?.Value ?? string.Empty,
                                ProcessLaunchTargetType.PackageFamilyName)
                            : throw new InvalidOperationException(
                                $"<ProcessLaunchCog> has no <Executable> or <Package> child in '{declarationFile}'.")
                },

                "ProgramAssociationCog" => ParseProgramAssociationCog(
                    cogElem, cogId, cogName, path, declarationFile),

                "ShortcutCog" => new ShortcutCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    ShortcutName = GetString(cogElem, "ShortcutName", path),
                    ExePath = GetString(cogElem, "ExePath", path),
                    RequiresElevation = GetBool(cogElem, "RequiresElevation")
                },

                "StartupTaskCog" => ParseStartupTaskCog(
                    cogElem, cogId, cogName, path, declarationFile),

                "TaskFolderCog" => new TaskFolderCog
                {
                    CogId = cogId,
                    CogName = cogName,
                    TaskFolderName = GetString(cogElem, "TaskFolderName", path)
                },

                var unknown => throw new InvalidOperationException(
                    $"Unknown cog type <{unknown}> in '{declarationFile}'. Mod rejected.")
            };

            cogs.Add(cog);

            ReboundLogger.WriteToLog(
                "ModParser ParseCogs",
                $"Parsed cog: {cogElem.Name.LocalName} '{cogName}' ({cogId})");
        }

        return cogs;
    }

    private static ObservableCollection<IModItem> ParseSettings(
        XElement parent,
        string path,
        string declarationFile)
    {
        XElement? settingsElement = parent.Element("Settings");

        if (settingsElement is null)
            return [];

        ObservableCollection<IModItem> settings = [];

        foreach (XElement element in settingsElement.Elements())
        {
            IModSetting setting = element.Name.LocalName switch
            {
                "ModBoolSetting" => ParseBoolSetting(element, path),
                "ModStringSetting" => ParseStringSetting(element, path),
                "ModEnumSetting" => ParseEnumSetting(element, path, declarationFile),

                _ => throw new InvalidOperationException(
                    $"Unknown setting type '{element.Name.LocalName}' in '{declarationFile}'.")
            };

            settings.Add(setting);
        }

        return settings;
    }

    private static ModBoolSetting ParseBoolSetting(
        XElement element,
        string path)
    {
        var setting = new ModBoolSetting(
            GetBool(element, "DefaultValue"))
        {
            Name = GetString(element, "Name", path),
            Description = GetString(element, "Description", path),
            Identifier = GetString(element, "Identifier", path),
            AppName = GetString(element, "AppName", path),
            IconGlyph = GetString(element, "IconGlyph", path)
        };

        return setting;
    }

    private static ModBoolSetting ParseStringSetting(
        XElement element,
        string path)
    {
        var setting = new ModBoolSetting(
            GetBool(element, "DefaultValue"))
        {
            Name = GetString(element, "Name", path),
            Description = GetString(element, "Description", path),
            Identifier = GetString(element, "Identifier", path),
            AppName = GetString(element, "AppName", path),
            IconGlyph = GetString(element, "IconGlyph", path)
        };

        return setting;
    }

    private static ModEnumSetting ParseEnumSetting(
        XElement element,
        string path,
        string declarationFile)
    {
        var setting = new ModEnumSetting(
            GetInt(element, "DefaultValue"))
        {
            Name = GetString(element, "Name", path),
            Description = GetString(element, "Description", path),
            Identifier = GetString(element, "Identifier", path),
            AppName = GetString(element, "AppName", path),
            IconGlyph = GetString(element, "IconGlyph", path)
        };

        XElement? optionsElement = element.Element("Options");

        if (optionsElement is null)
            throw new InvalidOperationException(
                $"Missing <Options> in '{declarationFile}'.");

        foreach (XElement option in optionsElement.Elements("Option"))
        {
            string value = option.Value.Trim();

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"Empty <Option> in '{declarationFile}'.");

            setting.Options.Add(value);
        }

        if (setting.Options.Count == 0)
            throw new InvalidOperationException(
                $"<Options> must contain at least one <Option> in '{declarationFile}'.");

        return setting;
    }

    private static StartupTaskCog ParseStartupTaskCog(
        XElement cogElem,
        Guid cogId,
        string cogName,
        string path,
        string declarationFile)
    {
        string target = GetString(cogElem, "Target", path);
        string rawTargetType = GetString(cogElem, "TargetType", path);

        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException(
                $"<Target> cannot be empty in '{declarationFile}'.");

        if (!Enum.TryParse<StartupTaskTargetType>(
                rawTargetType, ignoreCase: false, out var targetType)
            || !Enum.IsDefined(targetType))
        {
            throw new InvalidOperationException(
                $"Invalid <TargetType> '{rawTargetType}' in '{declarationFile}'.");
        }

        string taskName = GetString(cogElem, "TaskName", path);
        string taskDescription = GetString(cogElem, "TaskDescription", path);

        if (string.IsNullOrWhiteSpace(taskName))
            throw new InvalidOperationException(
                $"<TaskName> cannot be empty in '{declarationFile}'.");

        if (string.IsNullOrWhiteSpace(taskDescription))
            throw new InvalidOperationException(
                $"<TaskDescription> cannot be empty in '{declarationFile}'.");

        return new StartupTaskCog
        {
            CogId = cogId,
            CogName = cogName,
            StartupTarget = new StartupTaskTarget(target, targetType),
            TaskName = taskName,
            TaskDescription = taskDescription,
            TaskRequiresElevation = GetRequiredBool(
                cogElem, "TaskRequiresElevation", declarationFile)
        };
    }

    private static ProgramAssociationCog ParseProgramAssociationCog(
        XElement cogElem,
        Guid cogId,
        string cogName,
        string path,
        string declarationFile)
    {
        string originalExecutable = GetString(
            cogElem, "OriginalExecutable", path);

        if (string.IsNullOrWhiteSpace(originalExecutable))
            throw new InvalidOperationException(
                $"<OriginalExecutable> cannot be empty in '{declarationFile}'.");

        string destination = GetString(cogElem, "Destination", path);

        if (string.IsNullOrWhiteSpace(destination))
            throw new InvalidOperationException(
                $"<Destination> cannot be empty in '{declarationFile}'.");

        string rawType = GetString(cogElem, "TargetType", path);

        if (!Enum.TryParse<ProgramAssociationType>(
                rawType, ignoreCase: false, out var targetType)
            || !Enum.IsDefined(targetType))
        {
            throw new InvalidOperationException(
                $"Invalid <TargetType> '{rawType}' in '{declarationFile}'.");
        }

        return new ProgramAssociationCog
        {
            CogId = cogId,
            CogName = cogName,
            OriginalExecutable = originalExecutable,
            Target = new ProgramAssociationTarget(targetType, destination)
        };
    }

    private static PackageCog ParsePackageCog(
        XElement cogElem,
        string path,
        string declarationFile)
    {
        var packageUri = GetNullString(cogElem, "PackageURI", path);
        var storeProductId = GetNullString(cogElem, "StoreProductId", path);

        if ((packageUri is null) == (storeProductId is null))
        {
            throw new InvalidOperationException(
                $"<PackageCog> must contain exactly one of <PackageURI> or <StoreProductId> in '{declarationFile}'.");
        }

        var packageFamilyName = GetString(cogElem, "PackageFamilyName", path);

        if (string.IsNullOrWhiteSpace(packageFamilyName))
        {
            throw new InvalidOperationException(
                $"<PackageFamilyName> is missing or empty on <PackageCog> in '{declarationFile}'.");
        }

        var managementMode = PackageManagementTriggeredOn.FollowConfiguration;
        var managementElement = cogElem.Element("DoPackageManagementOn");

        if (managementElement is not null)
        {
            var raw = managementElement.Value.Trim();

            if (!Enum.TryParse<PackageManagementTriggeredOn>(
                    raw, ignoreCase: false, out managementMode)
                || !Enum.IsDefined(managementMode))
            {
                throw new InvalidOperationException(
                    $"Invalid <DoPackageManagementOn> value '{raw}' in '{declarationFile}'.");
            }
        }

        return new PackageCog
        {
            CogId = RequireGuid(cogElem, "Id", declarationFile),
            CogName = cogElem.Element("CogName")?.Value
                ?? throw new InvalidOperationException("<CogName> is missing."),
            Target = new PackageTarget(
                TargetType: packageUri is not null
                    ? PackageTargetType.Local
                    : PackageTargetType.Store,
                TargetPath: packageUri,
                StoreProductId: storeProductId,
                PackageFamilyName: packageFamilyName),
            DoPackageManagementOn = managementMode
        };
    }

    /// <summary>
    /// Reads a required <see cref="Guid"/> child element. Throws if missing or unparseable.
    /// </summary>
    private static Guid RequireGuid(XElement parent, string elementName, string declarationFile)
    {
        var raw = parent.Element(elementName)?.Value;

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                $"<{elementName}> is missing or empty on <{parent.Name.LocalName}> in '{declarationFile}'.");

        if (!Guid.TryParse(raw, out var guid))
            throw new InvalidOperationException(
                $"<{elementName}> value '{raw}' on <{parent.Name.LocalName}> is not a valid GUID in '{declarationFile}'.");

        return guid;
    }

    /// <summary>
    /// Reads a string child element, expanding known path variables.
    /// Returns an empty string if the element is absent.
    /// </summary>
    private static string GetString(XElement parent, string name, string path)
        => Expand(parent.Element(name)?.Value ?? string.Empty, path);

    private static string? GetNullString(XElement parent, string name, string path)
        => string.IsNullOrEmpty(Expand(parent.Element(name)?.Value ?? string.Empty, path)) ? null : Expand(parent.Element(name)?.Value ?? string.Empty, path);

    /// <summary>
    /// Reads a boolean child element. Returns <see langword="false"/> if absent or unparseable.
    /// </summary>
    private static bool GetBool(XElement parent, string name)
        => bool.TryParse(parent.Element(name)?.Value, out var val) && val;

    private static int GetInt(XElement parent, string name)
        => int.TryParse(parent.Element(name)?.Value, out var val) ? val : 0;

    private static bool GetRequiredBool(XElement parent, string name, string declarationFile)
    {
        var raw = parent.Element(name)?.Value;

        if (!bool.TryParse(raw, out var value))
            throw new InvalidOperationException(
                $"<{name}> is missing or contains an invalid boolean on <{parent.Name.LocalName}> in '{declarationFile}'.");

        return value;
    }

    /// <summary>
    /// Reads a list of string values from a named container element and a repeated child element name.
    /// Example: <c>&lt;TargetProcesses&gt;&lt;Process&gt;explorer.exe&lt;/Process&gt;&lt;/TargetProcesses&gt;</c>
    /// Returns an empty list if the container element is absent.
    /// </summary>
    private static List<string> GetStringList(XElement parent, string containerName, string itemName)
    {
        var container = parent.Element(containerName);
        if (container is null)
            return [];

        return [.. container
            .Elements(itemName)
            .Select(e => e.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    /// <summary>
    /// Expands sideloaded mod XML path variables to their runtime equivalents.
    /// </summary>
    /// <param name="input">The raw string value from the XML declaration.</param>
    /// <param name="currentPath">The mod folder path, used to resolve <c>$(Dependencies)</c>.</param>
    public static string Expand(string input, string currentPath)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("$(ProgramFiles)", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.InvariantCulture)
            .Replace("$(System32)", Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), StringComparison.InvariantCulture)
            .Replace("$(UserProfile)", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.InvariantCulture)
            .Replace("$(Dependencies)", Path.Combine(currentPath, "dependencies"), StringComparison.InvariantCulture)
            .Replace("$(ReboundDataFolder)", Variables.ReboundDataFolder, StringComparison.InvariantCulture)
            .Replace("$(ReboundInjectedDLLsFolder)", Variables.ReboundInjectedDLLsFolder, StringComparison.InvariantCulture)
            .Replace("$(ReboundLibsFolder)", Variables.ReboundLibsFolder, StringComparison.InvariantCulture)
            .Replace("$(ReboundStartMenuFolder)", Variables.ReboundStartMenuFolder, StringComparison.InvariantCulture)
            .Replace("$(ReboundProgramAssociationsFolder)", Variables.ReboundProgramAssociationsFolder, StringComparison.InvariantCulture);
    }
}