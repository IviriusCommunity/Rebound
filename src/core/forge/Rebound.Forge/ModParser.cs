// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Forge.Cogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace Rebound.Forge;

/// <summary>
/// Helpers to parse sideloaded Rebound mods.
/// </summary>
public static class ModParser
{
    /// <summary>
    /// Parses all mods inside the Rebound sideloaded mods folder.
    /// </summary>
    /// <returns>A list of sideloaded mods.</returns>
#pragma warning disable CA1002 // Do not expose generic lists
    public static List<Mod> ParseMods()
#pragma warning restore CA1002 // Do not expose generic lists
    {
        var mods = new List<Mod>();

        try
        {
            if (!Directory.Exists(Variables.ReboundProgramFilesModsFolder))
            {
                ReboundLogger.Log($"[ModLoader] Mods folder does not exist: {Variables.ReboundProgramFilesModsFolder}");
                return mods; // return empty list
            }

            ReboundLogger.Log($"[ModLoader] Parsing mods in folder: {Variables.ReboundProgramFilesModsFolder}");

            foreach (var modFolder in Directory.GetDirectories(Variables.ReboundProgramFilesModsFolder))
            {
                try
                {
                    ReboundLogger.Log($"[ModLoader] Parsing mod folder: {modFolder}");
                    var mod = Parse(modFolder);
                    mods.Add(mod);
                    ReboundLogger.Log($"[ModLoader] Successfully loaded mod: {mod.Name}");
                }
                catch (Exception ex)
                {
                    ReboundLogger.Log($"[ModLoader] Failed to parse mod folder: {modFolder}", ex);
                    // continue with next mod
                }
            }
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ModLoader] Unexpected error while parsing mods", ex);
        }

        ReboundLogger.Log($"[ModLoader] Finished parsing mods. Total loaded: {mods.Count}");
        return mods;
    }

    /// <summary>
    /// Parses a Rebound mod from the given folder.
    /// </summary>
    /// <param name="path">Path to the folder containing Rebound mod files.</param>
    /// <returns>The parsed Rebound mod.</returns>
    public static Mod Parse(string path)
    {
        try
        {
            ReboundLogger.Log($"[ModParser] Parsing mod folder: {path}");

            var declarationFile = Path.Combine(path, "declaration.xml");
            if (!File.Exists(declarationFile))
            {
                ReboundLogger.Log($"[ModParser] declaration.xml missing in {path}");
                throw new FileNotFoundException("declaration.xml missing", declarationFile);
            }

            XDocument doc = XDocument.Load(declarationFile);
            var modElement = doc.Element("Mod") ?? throw new Exception("Root element <Mod> missing");

            ReboundLogger.Log($"[ModParser] Root <Mod> element found");

            // Initialize instructions inline
            var cogsElement = modElement.Element("Cogs");
            var instructions = new ObservableCollection<ICog>();
            if (cogsElement != null)
            {
                foreach (var cogElem in cogsElement.Elements())
                {
                    ICog? cog = cogElem.Name.LocalName switch
                    {
                        // DLLInjectionCog is prohibited here

                        "FileCopyCog" => new FileCopyCog
                        {
                            Path = GetString(cogElem, "Path", path),
                            TargetPath = GetString(cogElem, "TargetPath", path)
                        },

                        "FolderCog" => new FolderCog
                        {
                            Path = GetString(cogElem, "Path", path),
                            AllowPersistence = GetBool(cogElem, "AllowPersistence")
                        },

                        "IFEOCog" => new IFEOCog
                        {
                            OriginalExecutableName = GetString(cogElem, "OriginalExecutableName", path),
                            LauncherPath = GetString(cogElem, "LauncherPath", path)
                        },

                        "PackageCog" => new PackageCog
                        {
                            PackageURI = GetString(cogElem, "PackageURI", path),
                            PackageFamilyName = GetString(cogElem, "PackageFamilyName", path)
                        },

                        "PackageLaunchCog" => new PackageLaunchCog
                        {
                            PackageFamilyName = GetString(cogElem, "PackageFamilyName", path),
                        },

                        "ProcessKillCog" => new ProcessKillCog
                        {
                            ProcessName = GetString(cogElem, "ProcessName", path),
                        },

                        "ShortcutCog" => new ShortcutCog
                        {
                            ShortcutName = GetString(cogElem, "ShortcutName", path),
                            ExePath = GetString(cogElem, "ExePath", path)
                        },

                        "StartupPackageCog" => new StartupPackageCog
                        {
                            TargetPackageFamilyName = GetString(cogElem, "TargetPackageFamilyName", path),
                            Description = GetString(cogElem, "Description", path),
                            Name = GetString(cogElem, "Name", path),
                            RequireAdmin = GetBool(cogElem, "RequireAdmin")
                        },

                        "StartupTaskCog" => new StartupTaskCog
                        {
                            TargetPath = GetString(cogElem, "TargetPath", path),
                            Description = GetString(cogElem, "Description", path),
                            Name = GetString(cogElem, "Name", path),
                            RequireAdmin = GetBool(cogElem, "RequireAdmin")
                        },

                        "StorePackageCog" => new StorePackageCog
                        {
                            PackageFamilyName = GetString(cogElem, "PackageFamilyName", path),
                            StoreProductId = GetString(cogElem, "StoreProductId", path)
                        },

                        // TaskFolderCog is prohibited here.

                        _ => null
                    };

                    if (cog != null)
                    {
                        instructions.Add(cog);
                        ReboundLogger.Log($"[ModParser] Added instruction: {cogElem.Name.LocalName}");
                    }
                    else
                    {
                        ReboundLogger.Log($"[ModParser] Unknown instruction type: {cogElem.Name.LocalName}");
                    }
                }
            }
            else
            {
                ReboundLogger.Log("[ModParser] No <Cogs> element found");
            }

            // Construct Mod object with instructions inline
            Mod mod = new Mod()
            {              
                Name = modElement.Element("Name")?.Value ?? "Unnamed Mod",
                Description = modElement.Element("Description")?.Value ?? "",
                Icon = Path.Combine(path, "icon.png"),
                //Cogs = instructions, // inline instructions
                Category = ModCategory.Sideloaded,
                PreferredInstallationTemplate = InstallationTemplate.Extras
            };

            ReboundLogger.Log($"[ModParser] Finished parsing mod: {mod.Name}");
            return mod;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ModParser] Error parsing mod folder: " + path, ex);
            throw;
        }
    }

    private static string GetString(XElement parent, string name, string path)
    => Expand(parent.Element(name)?.Value ?? "", path);

    private static bool GetBool(XElement parent, string name)
        => bool.TryParse(parent.Element(name)?.Value, out var val) && val;

    /// <summary>
    /// Expands sideloaded Rebound mod environment variables for XML declaration files.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="currentPath">The path of the current working directory, preferably a sideloaded Rebound mod folder.</param>
    /// <returns></returns>
    public static string Expand(string input, string currentPath)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("$(ProgramFiles)", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.InvariantCulture)
            .Replace("$(System32)", Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), StringComparison.InvariantCulture)
            .Replace("$(UserProfile)", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.InvariantCulture)
            .Replace("$(Dependencies)", Path.Combine(currentPath, "dependencies"), StringComparison.InvariantCulture)
            .Replace("$(ReboundDataFolder)", Variables.ReboundDataFolder, StringComparison.InvariantCulture);
    }
}