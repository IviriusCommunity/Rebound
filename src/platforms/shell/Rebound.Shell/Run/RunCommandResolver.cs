// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Rebound.Shell.Run;

internal readonly record struct RunCommandResult(bool Success, string? Error = null);

internal static class RunCommandResolver
{
    public static RunCommandResult Execute(string input, bool runAsAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new(false, "No command specified.");

        input = Environment.ExpandEnvironmentVariables(input.Trim());

        if (input.StartsWith('~'))
        {
            input = string.Concat(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                input.AsSpan(1));
        }

        // First, try the entire input as a shell target.
        // This handles paths, directories, URLs, shell: targets,
        // registered applications, and file associations.

        if (TryExecute(input, string.Empty, runAsAdmin, out _))
            return new(true);

        // The input may contain executable arguments.
        if (!TrySplitCommand(input, out string executable, out string arguments))
            return new(false, "Unable to resolve command.");

        if (TryExecute(executable, arguments, runAsAdmin, out var error))
            return new(true);

        return new(false, error?.Message ?? "Unable to launch command.");
    }

    private static bool TryExecute(string fileName, string arguments, bool runAsAdmin, out Exception? error)
    {
        error = null;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true
            };

            if (runAsAdmin)
                startInfo.Verb = "runas";

            using var process = Process.Start(startInfo);

            return true;
        }
        catch (Exception ex) when (
            ex is Win32Exception or
            FileNotFoundException or
            DirectoryNotFoundException or
            InvalidOperationException or
            ArgumentException)
        {
            error = ex;
            return false;
        }
    }

    private static bool TrySplitCommand(string input, out string executable, out string arguments)
    {
        executable = string.Empty;
        arguments = string.Empty;

        input = input.Trim();

        if (input.Length == 0)
            return false;

        // Quoted executable path.
        if (input[0] == '"')
        {
            int closingQuote = input.IndexOf('"', 1);

            if (closingQuote < 0)
                return false;

            executable = input[1..closingQuote];
            arguments = input[(closingQuote + 1)..].Trim();

            return executable.Length > 0;
        }

        // Unquoted executable paths containing spaces.
        // Find the executable extension before splitting arguments.
        string[] extensions =
        [
            ".exe",
            ".com",
            ".bat",
            ".cmd"
        ];

        foreach (string extension in extensions)
        {
            int searchIndex = 0;

            while (true)
            {
                int index = input.IndexOf(
                    extension,
                    searchIndex,
                    StringComparison.OrdinalIgnoreCase);

                if (index < 0)
                    break;

                int end = index + extension.Length;

                if (end == input.Length || char.IsWhiteSpace(input[end]))
                {
                    executable = input[..end].Trim();
                    arguments = input[end..].Trim();

                    return true;
                }

                searchIndex = end;
            }
        }

        // Generic command: command arguments
        int spaceIndex = input.IndexOf(' ', StringComparison.InvariantCultureIgnoreCase);

        if (spaceIndex < 0)
        {
            executable = input;
            return true;
        }

        executable = input[..spaceIndex].Trim();
        arguments = input[(spaceIndex + 1)..].Trim();

        return executable.Length > 0;
    }
}