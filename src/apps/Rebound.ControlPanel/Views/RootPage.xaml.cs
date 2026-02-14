using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using System;
using System.Diagnostics;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Rebound.ControlPanel.Views;

[INotifyPropertyChanged]
public sealed partial class RootPage : Page
{
    [ObservableProperty]
    private partial string UserPicturePath { get; set; }

    public RootPage()
    {
        InitializeComponent();
        UIThreadQueue.QueueAction(() =>
        {
            UserPicturePath = UserInformation.GetUserPicturePath() ?? string.Empty;
        });
        RootFrame.Navigate(typeof(HomePage));
    }

    public void InvokeWithArguments(string args)
    {
        if (args == @"/name Microsoft.AdministrativeTools")
        {
            // Placeholder
        }
    }

    private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        switch (args.InvokedItem)
        {
            case "Windows Security Firewall":
                {
                    Process.Start("firewall.cpl");
                    break;
                }
            case "Windows Tools":
                {
                    RootFrame.Navigate(typeof(WindowsToolsPage));
                    break;
                }
            case "Appearance and Personalization":
                {
                    RootFrame.Navigate(typeof(Appearance));
                    break;
                }
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