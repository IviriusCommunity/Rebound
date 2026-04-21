// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.DLLInjection.Structure;

/// <summary>
/// Parsed representation of a <c>.dllmeta</c> file.
/// </summary>
public sealed record DllMeta(
    IReadOnlyList<string> TargetProcesses,
    string? DllHash)
{
    private const string ProcessesKey = "TARGET_PROCESSES=";
    private const string HashKey = "DLL_HASH=";

    public static DllMeta? TryParse(string content)
    {
        List<string>? processes = null;
        string? hash = null;

        foreach (var rawLine in content?.Split('\n', StringSplitOptions.RemoveEmptyEntries)!)
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