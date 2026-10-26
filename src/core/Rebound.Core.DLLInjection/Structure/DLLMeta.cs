// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.DLLInjection.Structure;

/// <summary>
/// Parsed representation of a <c>.dllmeta</c> file. Used for declaring a DLL for injection.
/// </summary>
/// <param name="TargetProcesses">
/// Target processes for DLL injection. Use "*" to target all processes.
/// </param>
/// <param name="ExcludedProcesses">
/// Processes to exclude from the DLL injection.
/// </param>
public sealed record DllMeta(IReadOnlyList<string> TargetProcesses, IReadOnlyList<string> ExcludedProcesses)
{
    private const string ProcessesKey = "TARGET_PROCESSES=";
    private const string ExcludedProcessesKey = "EXCLUDED_PROCESSES=";

    /// <summary>
    /// Attempts to parse the specified string into a DllMeta instance.
    /// </summary>
    /// <remarks>
    /// The input string should contain lines starting with recognized keys for included processes and optionally 
    /// excluded processes. If the required process information is missing or cannot be parsed, the method returns
    /// null.
    /// </remarks>
    /// <param name="content">
    /// The string content to parse. Must contain included process and optionally excluded processes
    /// format.
    /// </param>
    /// <returns>
    /// A DllMeta instance if parsing is successful; otherwise, null.
    /// </returns>
    public static DllMeta? TryParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        List<string>? processes = null;
        List<string>? excludedProcesses = null;

        foreach (var rawLine in content.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (line.StartsWith(ProcessesKey, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[ProcessesKey.Length..].Trim();

                processes = value == "*"
                    ? ["*"]
                    : [.. value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)];
            }
            else if (line.StartsWith(ExcludedProcessesKey, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[ExcludedProcessesKey.Length..];

                excludedProcesses = [.. value.Split(
                    ';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)];
            }
        }

        if (processes is null || processes.Count == 0)
            return null;

        return new DllMeta(processes, excludedProcesses ?? []);
    }

    /// <summary>
    /// Serializes the target and excluded process lists into the <c>.dllmeta</c> format.
    /// </summary>
    /// <remarks>
    /// Target processes are separated by semicolons. Excluded processes are included
    /// only when the exclusion list is not empty.
    /// </remarks>
    /// <returns>
    /// A string containing the serialized target processes and, if present, excluded processes.
    /// </returns>
    public string Serialize()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ProcessesKey);
        sb.AppendJoin(';', TargetProcesses);
        sb.AppendLine();

        if (ExcludedProcesses.Count > 0)
        {
            sb.Append(ExcludedProcessesKey);
            sb.AppendJoin(';', ExcludedProcesses);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}