using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;

namespace Rebound.ControlPanel.Views;

public sealed partial class ReboundSettingsPage : Page
{
    public ReboundSettingsViewModel ViewModel { get; } = new();

    public ReboundSettingsPage()
    {
        InitializeComponent();
    }
}
