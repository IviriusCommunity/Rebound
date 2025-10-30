// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Forge.Cogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace Rebound.Forge;

public static class ModParser
{
    public static List<Mod> ParseMods()
    {
        var mods = new List<Mod>();

        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var modsRoot = Path.Combine(programDataPath, "Rebound", "Mods");

            if (!Directory.Exists(modsRoot))
            {
                ReboundLogger.Log($"[ModLoader] Mods folder does not exist: {modsRoot}");
                return mods; // return empty list
            }

            ReboundLogger.Log($"[ModLoader] Parsing mods in folder: {modsRoot}");

            foreach (var modFolder in Directory.GetDirectories(modsRoot))
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
            var instructionsElement = modElement.Element("Instructions");
            var instructions = new ObservableCollection<ICog>();
            if (instructionsElement != null)
            {
                foreach (var instr in instructionsElement.Elements())
                {
                    ICog? cog = instr.Name.LocalName switch
                    {
                        "IFEOCog" => new IFEOCog
                        {
                            OriginalExecutableName = instr.Element("OriginalExecutableName")?.Value ?? "",
                            LauncherPath = Expand(instr.Element("LauncherPath")?.Value ?? "", path)
                        },
                        "ShortcutCog" => new ShortcutCog
                        {
                            ShortcutName = instr.Element("ShortcutName")?.Value ?? "",
                            ExePath = Expand(instr.Element("ExePath")?.Value ?? "", path)
                        },
                        "LauncherCog" => new LauncherCog
                        {
                            Path = Expand(instr.Element("Path")?.Value ?? "", path),
                            TargetPath = Expand(instr.Element("TargetPath")?.Value ?? "", path)
                        },
                        "StartupTaskCog" => new StartupTaskCog
                        {
                            TargetPath = Expand(instr.Element("TargetPath")?.Value ?? "", path),
                            Description = instr.Element("Description")?.Value ?? "",
                            Name = instr.Element("Name")?.Value ?? "",
                            RequireAdmin = bool.TryParse(instr.Element("RequireAdmin")?.Value, out var ra) && ra
                        },
                        _ => null
                    };

                    if (cog != null)
                    {
                        instructions.Add(cog);
                        ReboundLogger.Log($"[ModParser] Added instruction: {instr.Name.LocalName}");
                    }
                    else
                    {
                        ReboundLogger.Log($"[ModParser] Unknown instruction type: {instr.Name.LocalName}");
                    }
                }
            }
            else
            {
                ReboundLogger.Log("[ModParser] No <Instructions> element found");
            }

            // Construct Mod object with instructions inline
            Mod mod = new Mod(
                modElement.Element("Name")?.Value ?? "Unnamed Mod",
                modElement.Element("Description")?.Value ?? "",
                Path.Combine(path, "icon.png"),
                modElement.Element("InstallationSteps")?.Value ?? "",
                instructions, // inline instructions
                modElement.Element("ProcessName")?.Value ?? "",
                ModCategory.Sideloaded
            )
            {
                EntryExecutable = modElement.Element("EntryExecutable")?.Value ?? ""
            };

            // Parse enum
            var templateValue = modElement.Element("PreferredInstallationTemplate")?.Value;
            if (!Enum.TryParse<InstallationTemplate>(templateValue, ignoreCase: true, out var template))
            {
                template = InstallationTemplate.Extras; // fallback default
                ReboundLogger.Log($"[ModParser] Failed to parse PreferredInstallationTemplate '{templateValue}', defaulting to Extras");
            }
            mod.PreferredInstallationTemplate = template;
            ReboundLogger.Log($"[ModParser] PreferredInstallationTemplate={mod.PreferredInstallationTemplate}");

            ReboundLogger.Log($"[ModParser] Finished parsing mod: {mod.Name}");
            return mod;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ModParser] Error parsing mod folder: " + path, ex);
            throw;
        }
    }

    public static string Expand(string input, string currentPath)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("$(ProgramFiles)", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
            .Replace("$(LocalAppData)", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            .Replace("$(System32)", Environment.GetFolderPath(Environment.SpecialFolder.SystemX86))
            .Replace("$(UserProfile)", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .Replace("$(Dependencies)", Path.Combine(currentPath, "dependencies"));
    }
}