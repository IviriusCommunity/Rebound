using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Windows.Storage;

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
        get => GetValue<bool>(ConvertStringToNumericRepresentation(DriveLetter));
        set => SetValue(ConvertStringToNumericRepresentation(DriveLetter), value);
    }

    public static string ConvertStringToNumericRepresentation(string? input)
    {
        input ??= "";

        // Create a StringBuilder to store the numeric representation
        StringBuilder numericRepresentation = new();

        // Iterate over each character in the string
        foreach (var c in input)
        {
            // Convert the character to its ASCII value and append it
            _ = numericRepresentation.Append((int)c);
        }

        // Return the numeric representation as a string
        return numericRepresentation.ToString();
    }

    public static T? GetValue<T>(string key)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            if ((T)userSettings.LocalSettings.Values[key] is not null) return (T)userSettings.LocalSettings.Values[key];
            else return default;
        }
        catch
        {
            return default;
        }
    }

    public static void SetValue<T>(string key, T newValue)
    {
        try
        {
            var userSettings = ApplicationData.GetDefault();
            userSettings.LocalSettings.Values[key] = newValue;
        }
        catch
        {
            return;
        }
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