using CommunityToolkit.Mvvm.ComponentModel;

#nullable enable

namespace Rebound.Defrag.Controls;

/// <summary>
/// The drive items themselves seen in the scheduled defrag window
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
    public bool IsChecked { get; set; }
}