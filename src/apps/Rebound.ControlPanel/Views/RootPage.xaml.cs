using System.Xml.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

public sealed partial class RootPage : Page
{
    public RootPage()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(HomePage));
    }

    public void InvokeWithArguments(string args)
    {
        if (args == @"/name Microsoft.AdministrativeTools")
        {
            // Placeholder
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        try
        {
            RootFrame.GoBack();
        }
        catch
        {

        }
    }
}