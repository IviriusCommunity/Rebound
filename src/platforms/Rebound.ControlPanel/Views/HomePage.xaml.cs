using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;

namespace Rebound.ControlPanel.Views;

internal sealed partial class HomePage : Page
{
    internal HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
    }
}