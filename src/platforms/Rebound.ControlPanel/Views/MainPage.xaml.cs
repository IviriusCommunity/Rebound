using Microsoft.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(HomePage));
    }
}