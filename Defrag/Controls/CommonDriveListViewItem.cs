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

public partial class CommonDriveListViewItem : ObservableObject
{
    // The name of the drive
    public string? DriveName { get; set; }

    // Image path for the disk
    public string? ImagePath { get; set; }

    // Drive path (C:/ or {GUID}/)
    public string? DrivePath { get; set; }

    // Optical drive, SSD, HDD, etc.
    public string? MediaType { get; set; }

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
}