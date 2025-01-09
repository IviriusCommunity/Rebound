using System;
using System.Diagnostics;
using System.Linq;
using Rebound.Helpers;
using static Rebound.Defrag.MainWindow;

#nullable enable

namespace Rebound.Defrag.Controls;

public partial class DriveListViewItem
{
    public string? DriveName { get; set; }

    public string? ImagePath { get; set; }

    public string? DriveLetter { get; set; }

    public string? LastOptimized { get; set; }

    public bool? NeedsOptimization
    {
        get => CheckIfNeedsOptimization(); 
        set;
    }

    public string? MediaType { get; set; }

    public int OperationProgress { get; set; }

    public string? OperationInformation { get; set; }

    public Process? PowerShellProcess { get; set; }

    public bool IsChecked
    {
        // Use settings helper here to retrieve and write this value
        get;
        set;
    }

    public bool CheckIfNeedsOptimization()
    {
        var status = string.Empty;

        try
        {
            var i = DefragInfo.GetEventLogEntriesForID(258);

            var selI = i.Last(s => s.Contains($"({DriveLetter?.ToString().Remove(2, 1)})"));

            var localDate = DateTime.Parse(selI[..^4]);

            // Get the current local date and time
            var currentDate = DateTime.Now;

            // Calculate the days passed
            var timeSpan = currentDate - localDate;
            var daysPassed = timeSpan.Days;

            return daysPassed >= 50;
        }
        catch
        {
            return true;
        }
    }
}