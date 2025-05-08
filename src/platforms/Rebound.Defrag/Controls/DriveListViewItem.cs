using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Rebound.Defrag.Helpers;
using Rebound.Helpers;

namespace Rebound.Defrag.Controls;

internal partial class DriveListViewItem : ObservableObject
{
    [ObservableProperty]
    public partial string DriveName { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; }

    [ObservableProperty]
    public partial string DrivePath { get; set; }

    [ObservableProperty]
    public partial string MediaType { get; set; }

    [ObservableProperty]
    public partial Process? PowerShellProcess { get; set; }

    [ObservableProperty]
    public partial bool NeedsOptimization { get; set; }

    [ObservableProperty]
    public partial bool CanBeOptimized { get; set; }

    [ObservableProperty]
    public partial string LastOptimized { get; set; }

    [ObservableProperty]
    public partial int OperationProgress { get; set; }

    [ObservableProperty]
    public partial string OperationInformation { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue(GenericHelpers.ConvertStringToSafeKey(DrivePath), "dfrgui", newValue);
    }

    internal DriveListViewItem(string driveName, string drivePath, string image, string mediaType)
    {
        DriveName = driveName;
        DrivePath = drivePath;
        ImagePath = image;
        MediaType = mediaType;
        IsChecked = SettingsHelper.GetValue(GenericHelpers.ConvertStringToSafeKey(DrivePath), "dfrgui", false);
        NeedsOptimization = CheckNeedsOptimization();
        CanBeOptimized = DriveName is not "EFI System Partition" and not "Recovery Partition" && MediaType is not "CD-ROM" and not "Removable";
        getstuff();
        /*OperationInformation = !CanBeOptimized ? "Cannot be optimized" : NeedsOptimization ? "Needs optimization" : "OK";*/
    }

    async void getstuff()
    {
            var analysis = GetVolumeFragmentationAnalysis(DrivePath?.DrivePathToLetter());

            OperationInformation = analysis.ToString();
    }

    public class Fragmentation
    {
        public double Percent { get; set; }
        public string VolumeName { get; set; }
        public bool DefragNeeded { get; set; }

        // Optionally, you can add more properties depending on what you want to track
        public long TotalClusters { get; set; }
        public long FragmentedClusters { get; set; }

        // Constructor for initialization
        public Fragmentation()
        {
            Percent = 0.0;
            DefragNeeded = false;
            TotalClusters = 0;
            FragmentedClusters = 0;
        }

        // Optionally, a method to display the fragmentation details
        public void DisplayDetails()
        {
            Console.WriteLine("Volume: {0}", VolumeName);
            Console.WriteLine("Fragmentation Percentage: {0}%", Percent);
            Console.WriteLine("Defrag Needed: {0}", DefragNeeded ? "Yes" : "No");
            Console.WriteLine("Total Clusters: {0}", TotalClusters);
            Console.WriteLine("Fragmented Clusters: {0}", FragmentedClusters);
        }
    }

    public static string GetVolumeFragmentationAnalysis(string drive)
    {
        // For some reason this is one of the most well kept secrets at Microsoft apparently
        /*var comType = Type.GetTypeFromCLSID(new Guid("87CB4E0D-2E2F-4235-BC0A-7C62308011F6"));
        var instance = Activator.CreateInstance(comType);*/
        return "A";
        /*try
        {
            // Query for the specific volume by DriveLetter
            var query = new ObjectQuery($@"SELECT * FROM Win32_Volume WHERE DriveLetter = '{drive}'");

            using var searcher = new ManagementObjectSearcher(query);

            var moVolume = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                if (moVolume is ManagementObject obj)
                {
                    // Prepare output arguments for DefragAnalysis method
                    var outputArgs = new object[2];  // [0] - DefragRecommended, [1] - DefragAnalysis

                    // Call DefragAnalysis method on the volume
                    var result = (uint)obj.InvokeMethod("DefragAnalysis", outputArgs);

                    // Check if the method call was successful (result == 0)
                    if (result == 0)
                    {
                        if (outputArgs[1] is ManagementBaseObject mboDefragAnalysis)
                        {
                            // Extract fragmentation information
                            var fragmentationPercent = mboDefragAnalysis["TotalPercentFragmentation"]?.ToString() ?? "Unknown";
                            return $"Fragmentation Percentage: {fragmentationPercent}";
                        }
                        else
                        {
                            return "Failed to cast DefragAnalysis result.";
                        }
                    }
                    else
                    {
                        return $"DefragAnalysis failed with result code {result}.";
                    }
                }
            return "a";
        }
        catch (ManagementException ex)
        {
            // WMI specific exception
            return $"WMI Error: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission issue (e.g., not enough privileges)
            return $"Permission Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            // General exception handler
            return $"Error: {ex.Message}";
        }*/
    }

    // Defrag events
    private const int EventID = 258;

    // Caching
    private static List<string>? cachedEventMessages;
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
            var analysis = GetVolumeFragmentationAnalysis(DrivePath?.DrivePathToLetter());
            return true;
        }
        catch
        {
            // Assume optimization is needed on error
        }

        return false;
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