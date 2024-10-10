using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Control.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            if (App.cpanelWin != null) App.cpanelWin.TitleBarEx.SetWindowIcon("Assets\\AppIcons\\rcontrol.ico");
            if (App.cpanelWin != null) App.cpanelWin.Title = "Rebound Control Panel";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.cpanelWin != null)
            {
                App.cpanelWin.RootFrame.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.cpanelWin != null)
            {
                App.cpanelWin.RootFrame.GoForward();
            }
        }

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
            App.cpanelWin.Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.cpanelWin != null)
            {
                var oldHistory = App.cpanelWin.RootFrame.ForwardStack;
                var newList = new List<PageStackEntry>();
                foreach (var item in oldHistory)
                {
                    newList.Add(item);
                }
                App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
                App.cpanelWin.RootFrame.GoBack();
                App.cpanelWin.RootFrame.ForwardStack.Clear();
                foreach (var item in newList)
                {
                    App.cpanelWin.RootFrame.ForwardStack.Add(item);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (App.cpanelWin != null)
            {
                App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedIndex == 0 && (App.cpanelWin != null))
            {
                App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            }
            if ((sender as ComboBox).SelectedIndex == 1 && (App.cpanelWin != null))
            {
                App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
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

            var process = Process.Start(info);

            App.cpanelWin.Close();
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            App.cpanelWin.RootFrame.Navigate(typeof(SystemAndSecurity), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }
    }
}
