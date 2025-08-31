using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Rebound.Forge;

internal static class ReboundLogger
{
    public static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rebound",
        "Temp",
        ".log");

    private static readonly object _lock = new();

    public static void Log(string message, Exception? ex = null)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " +
                       $"[T{Thread.CurrentThread.ManagedThreadId}] {message}";

            if (ex != null)
            {
                line += $"{Environment.NewLine}    EXCEPTION: {ex}";
            }

            lock (_lock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // If logging itself fails, at least write to debug output
            Debug.WriteLine("Logging failed: " + message);
        }
    }
}