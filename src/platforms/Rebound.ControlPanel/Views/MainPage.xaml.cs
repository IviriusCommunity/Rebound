using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Rebound.Dialer.ViewModels;

namespace Rebound.ControlPanel.Views;

public sealed partial class MainPage : Page
{
    internal MainViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(HomePage));
    }

    private async void AddressBar_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ViewModel.ShowEditableAddressBar = true;
        await Task.Delay(50).ConfigureAwait(true);
        _ = EditableAddressBar.Focus(Microsoft.UI.Xaml.FocusState.Pointer);
    }

    private void EditableAddressBar_LostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ShowEditableAddressBar = false;
        _ = AddressBar.Focus(Microsoft.UI.Xaml.FocusState.Pointer);
    }

    private void EditableAddressBar_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.ShowEditableAddressBar = false;
            _ = AddressBar.Focus(Microsoft.UI.Xaml.FocusState.Pointer);
        }
    }
}