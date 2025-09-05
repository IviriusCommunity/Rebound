using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.ControlPanel.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowEditableAddressBar { get; set; }

    public ObservableCollection<string> Paths =
    [
        "Control Panel"
    ];
}