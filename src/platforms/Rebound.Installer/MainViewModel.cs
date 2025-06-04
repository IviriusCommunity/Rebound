using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.Installer;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial double Progress { get; set; } = 0;

    [ObservableProperty]
    public partial string Status { get; set; } = "Initializing...";

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsError { get; set; } = false;

    [ObservableProperty]
    public partial double Steps { get; set; } = 1;

    public double GetAllItemsForFolder()
    {
        return 0;
    }
}