using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace Rebound.ControlPanel.Views;

internal sealed partial class HomePage : Page
{
    internal HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        InitializeComponent();
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
            case "Rebound Settings":
                {
                    (Parent as Frame)?.Navigate(typeof(ReboundSettingsPage));
                    break;
                }
            case "Windows Tools":
                {
                    (Parent as Frame)?.Navigate(typeof(WindowsToolsPage));
                    break;
                }
            case "Appearance and Personalization":
                {
                    (Parent as Frame)?.Navigate(typeof(Appearance));
                    break;
                }
        }
    }

    [RelayCommand]
    public void LaunchReboundHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub", "Rebound Hub.exe"),
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ReboundHub"),
                Verb = "runas"
            });
        }
        catch
        {

        }
    }

    [RelayCommand]
    public void LaunchPath(string path)
    {
        try
        {
            Process.Start(path);
        }
        catch
        {

        }
    }

    private void WinverHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        Process.Start("winver");
    }
}