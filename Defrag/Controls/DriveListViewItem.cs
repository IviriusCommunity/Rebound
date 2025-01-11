using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Rebound.Defrag.Helpers;
using static Rebound.Defrag.MainWindow;

#nullable enable

namespace Rebound.Defrag.Controls;

public partial class DriveListViewItem
{
    // The name of the drive
    public string? DriveName { get; set; }

    // Image path for the disk
    public string? ImagePath { get; set; }

    // Drive path (C:/ or {GUID}/)
    public string? DrivePath { get; set; }

    // The last time it was optimized
    public string? LastOptimized
    {
        get => GetLastOptimized(); 
        set;
    }

    // Determines whether or not the drive should be optimized
    public bool NeedsOptimization
    {
        get => CheckNeedsOptimization();
        set;
    }

    // Determines whether or not the drive can be optimized
    public bool CanBeOptimized
    {
        get => DriveName is not "EFI System Partition" and not "Recovery Partition" && MediaType is not "CD-ROM"; 
        set;
    }

    // Optical drive, SSD, HDD, etc.
    public string? MediaType { get; set; }

    // How much of the operation is finished
    public int OperationProgress { get; set; }

    // Information to be displayed about the operation
    public string? OperationInformation { get; set; }

    // The defragmentation process
    public Process? PowerShellProcess { get; set; }

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

    private const int EventID = 258;
    private static List<string> cachedEventMessages = null;
    private static DateTime lastCacheTime = DateTime.MinValue;
    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(5); // Cache duration of 5 minutes

    public string GetLastOptimized()
    {
        try
        {
            var lastOptimizedDate = GetLastOptimizationDate();
            if (lastOptimizedDate == null) return "Never";

            var daysPassed = (DateTime.Now - lastOptimizedDate.Value).Days;

            return daysPassed switch
            {
                0 => "Today",
                1 => "Yesterday",
                _ => $"{daysPassed} days ago"
            };
        }
        catch
        {
            return "Never";
        }
    }

    public bool CheckNeedsOptimization()
    {
        try
        {
            var lastOptimizedDate = GetLastOptimizationDate();
            if (lastOptimizedDate == null) return true;

            var daysPassed = (DateTime.Now - lastOptimizedDate.Value).Days;

            return daysPassed >= 50;
        }
        catch
        {
            return true;
        }
    }

    private DateTime? GetLastOptimizationDate()
    {
        try
        {
            var lastOptimizedEntry = GetEventLogEntriesForID(EventID)
                .LastOrDefault(s => s.Contains($"({DrivePath?.DrivePathToLetter()})"));

            if (string.IsNullOrEmpty(lastOptimizedEntry))
                return null;

            // Parse date and return
            return DateTime.Parse(lastOptimizedEntry[..^4]);
        }
        catch
        {
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

        List<string> eventMessages = new List<string>();

        // Define the query
        var logName = "Application"; // Windows Logs > Application
        var queryStr = "*[System/EventID=" + eventID + "]";

        EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryStr);

        // Create the reader
        using (EventLogReader reader = new EventLogReader(query))
        {
            // Read the events from the log
            for (var eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
            {
                // Extract and format the message from the event
                var sb = string.Concat(eventInstance.TimeCreated.ToString(), eventInstance.FormatDescription().ToString().AsSpan(eventInstance.FormatDescription().ToString().Length - 4));

                // Add the formatted message to the list
                eventMessages.Add(sb.ToString());
            }
        }

        // Update the cache
        cachedEventMessages = eventMessages;
        lastCacheTime = DateTime.Now; // Store the time when the cache was updated

        return eventMessages;
    }
}