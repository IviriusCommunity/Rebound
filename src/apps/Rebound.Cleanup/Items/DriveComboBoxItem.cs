using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.Cleanup.Items;

internal partial class DriveComboBoxItem : ObservableObject
{
    [ObservableProperty]
    public partial string DriveName { get; set; }

    [ObservableProperty]
    public partial string DrivePath { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; }

    [ObservableProperty]
    public partial string MediaType { get; set; }
}