using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Rebound.Defrag.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Rebound.Defrag.Controls;

/// <summary>
/// The drive items themselves seen in the main window
/// </summary>

public partial class DriveListViewItem : ObservableObject
{
    // The name of the drive
    public string? DriveName { get; set; }

    // Image path for the disk
    public string? ImagePath { get; set; }

    // Drive path (C:/ or {GUID}/)
    public string? DrivePath { get; set; }

    // Optical drive, SSD, HDD, etc.
    public string? MediaType { get; set; }

    // The defragmentation process
    public Process? PowerShellProcess { get; set; }

    // Determines whether or not the drive should be optimized
    public bool NeedsOptimization => CheckNeedsOptimization();

    // Determines whether or not the drive can be optimized
    public bool CanBeOptimized => DriveName is not "EFI System Partition" and not "Recovery Partition" && MediaType is not "CD-ROM" and not "Removable";

    // The last time it was optimized
    [ObservableProperty]
    public partial string? LastOptimized { get; set; }

    // How much of the operation is finished
    [ObservableProperty]
    public partial int OperationProgress { get; set; }

    // Information to be displayed about the operation
    [ObservableProperty]
    public partial string? OperationInformation { get; set; }

    // Information to be displayed about the operation
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    // Item selected value
    public bool IsChecked
    {
        // Load the value from the settings
        get => SettingsHelper.GetValue<bool>(
            // Use the numerical representation of the drive letter to avoid conflicts
            GenericHelpers.ConvertStringToNumericRepresentation(DrivePath));

        // Set the value to the settings
        set => SettingsHelper.SetValue(
            // Use the numerical representation of the drive letter to avoid conflicts
            GenericHelpers.ConvertStringToNumericRepresentation(DrivePath), value);
    }

    // Defrag events
    private const int EventID = 258;

    // Caching
    private static List<string>? cachedEventMessages = null;
    private static DateTime lastCacheTime = DateTime.MinValue;
    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(1); // Cache duration of 1 minute

    // UI thread
    private readonly DispatcherQueue _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public void Cancel()
    {
        if (PowerShellProcess != null)
        {
            // Clear cached data
            cachedEventMessages = null;
            lastCacheTime = DateTime.MinValue;

            // Terminate the PowerShell process
            PowerShellProcess.Kill();
            PowerShellProcess = null;
        }
    }

    public async Task Optimize()
    {
        if (!CanBeOptimized)
        {
            return;
        }

        var volume = DrivePath?.DrivePathToSingleLetter();
        var command = $@"Optimize-Volume -DriveLetter {volume} {(MediaType?.Contains("HDD") == true ? "-Defrag" : "-Retrim")} -Verbose";

        var processInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };

        try
        {
            // Begin processing
            // Processing... [----====------------]
            IsLoading = true;
            OperationProgress = 0;
            OperationInformation = "Processing...";

            // Create process
            using var process = new Process { StartInfo = processInfo };
            PowerShellProcess = process;

            // Track already processed messages
            var alreadyProcessedMessages = new HashSet<string>();

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    _uiDispatcherQueue.TryEnqueue(() => ProcessOutput(args.Data));
                }
            };

            // Begin defrag
            await Task.Run(process.Start);
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            // Finish defrag
            // OK [--------------------]
            ClearCache();

            IsLoading = false;
            OperationProgress = 0;
            OperationInformation = "OK";
            LastOptimized = "Today";
            PowerShellProcess = null;

            void ProcessOutput(string data)
            {
                if (data.StartsWith("VERBOSE: ") && data.Contains(" complete"))
                {
                    var progressText = data.Replace("VERBOSE: ", string.Empty).Trim();

                    if (alreadyProcessedMessages.Add(progressText))
                    {
                        // Extract the percentage
                        var percentageMatch = MyRegex().Match(progressText);
                        if (percentageMatch.Success && int.TryParse(percentageMatch.Groups[1].Value, out var progress))
                        {
                            // Ongoing
                            // Trim: 50% done [==========----------]
                            IsLoading = false;
                            OperationProgress = progress;
                            OperationInformation = progressText;
                        }
                    }
                }
            }
        }
        catch
        {
            ResetState();
        }
    }

    private static void ClearCache()
    {
        cachedEventMessages = null;
        lastCacheTime = DateTime.MinValue;
    }

    private void ResetState()
    {
        IsLoading = false;
        OperationProgress = 0;
        OperationInformation = "Error";
        PowerShellProcess = null;
    }

    public string GetLastOptimized()
    {
        try
        {
            var lastOptimizedDate = GetLastOptimizationDate();

            // If no optimization date is available, assume optimization is needed
            if (!lastOptimizedDate.HasValue)
            {
                return "Never";
            }

            // Calculate days passed since the last optimization
            var daysPassed = (DateTime.Now - lastOptimizedDate.Value).Days;

            // Return the amount of days that have passed since the last optimization
            return daysPassed switch
            {
                0 => "Today",
                1 => "Yesterday",
                _ => $"{daysPassed} days ago"
            };
        }
        catch
        {
            // Assume optimization is needed on error
            return "Never";
        }
    }

    public bool CheckNeedsOptimization()
    {
        try
        {
            var lastOptimizedDate = GetLastOptimizationDate();

            // If no optimization date is available, assume optimization is needed
            if (!lastOptimizedDate.HasValue)
            {
                return true;
            }

            // Calculate days passed since the last optimization
            var daysPassed = (DateTime.Now - lastOptimizedDate.Value).Days;

            // Return true if 50 or more days have passed, otherwise false
            return daysPassed >= 50;
        }
        catch
        {
            // Assume optimization is needed on error
            return true; 
        }
    }

    private DateTime? GetLastOptimizationDate()
    {
        try
        {
            // Retrieve the most recent log entry matching the drive path
            var lastOptimizedEntry = GetEventLogEntriesForID(EventID)
                // Get the last entry
                .LastOrDefault(entry => entry.Contains($"({DrivePath?.DrivePathToLetter()})"));

            // If there's no entry return null
            if (string.IsNullOrWhiteSpace(lastOptimizedEntry))
            {
                return null;
            }

            // Extract and parse the date portion of the entry
            var datePart = lastOptimizedEntry[..^4];

            // If parse is successful return
            if (DateTime.TryParse(datePart, out var parsedDate))
            {
                return parsedDate;
            }

            // Return null if parsing fails
            return null; 
        }
        catch
        {
            // Return null if parsing fails
            return null;
        }
    }

    // Method to retrieve event log entries for a specific Event ID
    public static List<string> GetEventLogEntriesForID(int eventID)
    {
        // Cache the event log entries to reduce redundant queries
        if (cachedEventMessages != null && DateTime.Now - lastCacheTime < cacheDuration)
        {
            return cachedEventMessages; // Return cached result if still valid
        }

        List<string> logEntries = [];

        // Define the query
        var logName = "Application"; // Windows Logs > Application
        var queryStr = "*[System/EventID=" + eventID + "]";

        var query = new EventLogQuery(logName, PathType.LogName, queryStr);

        // Create the reader
        using (var reader = new EventLogReader(query))
        {
            // Read the events from the log
            for (var eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
            {
                // Extract and format the message from the event
                var sb = string.Concat(eventInstance.TimeCreated.ToString(), eventInstance.FormatDescription().ToString().AsSpan(eventInstance.FormatDescription().ToString().Length - 4));

                // Add the formatted message to the list
                logEntries.Add(sb.ToString());
            }
        }

        // Update the cache
        cachedEventMessages = logEntries;

        // Store the time when the cache was updated
        lastCacheTime = DateTime.Now; 

        // Return the log entries
        return logEntries;
    }

    [GeneratedRegex(@"(\d+)%")]
    private static partial Regex MyRegex();
}