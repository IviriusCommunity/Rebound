// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Rebound.Core;

/// <summary>
/// The severity of a log message.
/// </summary>
public enum LogMessageSeverity
{
    /// <summary>
    /// Minimum severity. Verbose status and debugging only. These are only saved if Rebound verbosity is enabled.
    /// </summary>
    Message,

    /// <summary>
    /// Everything that would require attention but is not fatal.
    /// </summary>
    Warning,

    /// <summary>
    /// Everything fatal for the mod or program in question.
    /// </summary>
    Error
}

public static class ReboundLogger
{
    private static readonly string _processName = Process.GetCurrentProcess().ProcessName;

    private static readonly Lock _lock = new();

    [Obsolete("Old log method. Use WriteToLog instead.")]
    public static void Log(string msg, Exception? ex = null)
    {
        WriteToLog("Old Log", msg, LogMessageSeverity.Message, ex);
    }

    public static void WriteToLog(string actionType, string message, LogMessageSeverity messageSeverity = LogMessageSeverity.Message, Exception? ex = null)
    {
        try
        {
            if (!SettingsManager.GetValue("Verbose", "rebound", false) && messageSeverity == LogMessageSeverity.Message)
                return;

            var line = $"[{messageSeverity}] [{_processName}] [{actionType}] [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            if (ex != null)
            {
                line += $" ***** {ex}";
            }
            lock (_lock)
            {
                try
                {
                    var dir = Path.GetDirectoryName(Variables.ReboundLogFile);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                        File.SetAttributes(dir, FileAttributes.Directory);
                    }
                    File.AppendAllText(Variables.ReboundLogFile, line + Environment.NewLine);
                }
                catch (IOException ioEx)
                {
                    Debug.WriteLine("ReboundLogger IO error: " + ioEx);
                }
            }
        }
        catch (Exception outerEx)
        {
            Debug.WriteLine("ReboundLogger: Logging failed: " + message + " - " + outerEx);
        }
    }
}