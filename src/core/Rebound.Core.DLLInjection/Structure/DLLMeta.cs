// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.DLLInjection.Structure;

/// <summary>
/// Parsed representation of a <c>.dllmeta</c> file. Used for declaring a DLL for injection.
/// </summary>
/// <param name="TargetProcesses">
/// Target processes for the DLL injection. Leave empty to target all processes.
/// </param>
/// <param name="DllHash">
/// The DLL's hash.
/// </param>
public sealed record DllMeta(
    IReadOnlyList<string> TargetProcesses,
    string? DllHash)
{
    private const string ProcessesKey = "TARGET_PROCESSES=";
    private const string HashKey = "DLL_HASH=";

    /// <summary>
    /// Attempts to parse the specified string into a DllMeta instance.
    /// </summary>
    /// <remarks>
    /// The input string should contain lines starting with recognized keys for processes, hash, and
    /// optionally signature. If the required process information is missing or cannot be parsed, the method returns
    /// null.
    /// </remarks>
    /// <param name="content">
    /// The string content to parse. Must contain process, hash, and optionally signature information in the expected
    /// format.
    /// </param>
    /// <returns>
    /// A DllMeta instance if parsing is successful; otherwise, null.
    /// </returns>
    public static DllMeta? TryParse(string content)
    {
        List<string>? processes = null;
        string? hash = null;

        foreach (var rawLine in content?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)!)
        {
            var line = rawLine.Trim();

            if (line.StartsWith(ProcessesKey, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[ProcessesKey.Length..];
                processes = [.. value.Split('\\', StringSplitOptions.RemoveEmptyEntries)];
            }
            else if (line.StartsWith(HashKey, StringComparison.OrdinalIgnoreCase))
            {
                hash = line[HashKey.Length..].Trim();
            }
        }

        if (processes is null)
            return null;

        return new DllMeta(processes, hash);
    }

    /// <summary>
    /// Serializes the current object's process and hash information into a string format, excluding any signature data.
    /// </summary>
    /// <remarks>
    /// The output string includes the list of target processes separated by backslashes, followed by
    /// the DLL hash if it is present.
    /// </remarks>
    /// <returns>
    /// A string containing the serialized representation of the target processes and, if available, the DLL hash.
    /// </returns>
    public string Serialize()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ProcessesKey);
        sb.AppendJoin('\\', TargetProcesses);
        sb.AppendLine();

        if (DllHash is not null)
        {
            sb.Append(HashKey);
            sb.AppendLine(DllHash);
        }

        return sb.ToString();
    }
}