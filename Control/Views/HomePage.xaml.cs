using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Control.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        App.ControlPanelWindow?.TitleBarEx.SetWindowIcon("Assets\\AppIcons\\rcontrol.ico");
        if (App.ControlPanelWindow != null)
        {
            App.ControlPanelWindow.Title = "Rebound Control Panel";
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => App.ControlPanelWindow?.RootFrame.GoBack();

    private void ForwardButton_Click(object sender, RoutedEventArgs e) => App.ControlPanelWindow?.RootFrame.GoForward();

    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        _ = await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        App.ControlPanelWindow.Close();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (App.ControlPanelWindow != null)
        {
            var oldHistory = App.ControlPanelWindow.RootFrame.ForwardStack;
            var newList = new List<PageStackEntry>();
            foreach (var item in oldHistory)
            {
                newList.Add(item);
            }
            _ = App.ControlPanelWindow.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            App.ControlPanelWindow.RootFrame.GoBack();
            App.ControlPanelWindow.RootFrame.ForwardStack.Clear();
            foreach (var item in newList)
            {
                App.ControlPanelWindow.RootFrame.ForwardStack.Add(item);
            }
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e) => App.ControlPanelWindow?.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as ComboBox).SelectedIndex == 0 && (App.ControlPanelWindow != null))
        {
            _ = App.ControlPanelWindow.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
        if ((sender as ComboBox).SelectedIndex == 1 && (App.ControlPanelWindow != null))
        {
            _ = App.ControlPanelWindow.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var info = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = "Start-Process -FilePath \"C:\\Windows\\System32\\control.exe\"",
            Verb = "runas",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _ = Process.Start(info);

        App.ControlPanelWindow.Close();
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) => App.ControlPanelWindow.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());

    private void Button_Click_1(object sender, RoutedEventArgs e) => App.ControlPanelWindow.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());

    private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e) => App.ControlPanelWindow.RootFrame.Navigate(typeof(SystemAndSecurity), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
}
