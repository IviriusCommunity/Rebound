// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Settings;
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

    /// <summary>
    /// Writes a log message to the Rebound log file. 
    /// </summary>
    /// <param name="actionType">
    /// The type of the action being logged. For example, "IFEOCog Apply".
    /// </param>
    /// <param name="message">
    /// The message of the action being logged. For example, "Applied IFEOCog to winver.exe".
    /// </param>
    /// <param name="messageSeverity">
    /// The severity of the message.
    /// </param>
    /// <param name="ex">
    /// The exception, if any. Recommended to be used with <see cref="LogMessageSeverity.Error"/>.
    /// </param>
    /// <remarks>
    /// If the message severity is "Message" and Rebound verbosity is not enabled, the message will be ignored.
    /// </remarks>
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