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
                try
                {
                    // Defensive: ensure directory still exists (someone may have deleted it).
                    var dir = Path.GetDirectoryName(LogFile);
                    var dir2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rebound";
                    if (!string.IsNullOrEmpty(dir2))
                    {
                        Directory.CreateDirectory(dir2);
                    }
                    File.SetAttributes(dir2, FileAttributes.Directory);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    File.SetAttributes(dir, FileAttributes.Directory);

                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
                catch (IOException ioEx)
                {
                    // Could be file locked or disk issue — at least surface to debug output.
                    Debug.WriteLine("ReboundLogger IO error: " + ioEx);
                }
            }
        }
        catch (Exception outerEx)
        {
            // If logging itself fails, at least write to debug output
            Debug.WriteLine("ReboundLogger: Logging failed: " + message + " — " + outerEx);
        }
    }
}