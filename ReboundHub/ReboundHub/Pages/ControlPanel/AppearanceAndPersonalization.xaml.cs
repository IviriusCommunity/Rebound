using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundHub.ReboundHub.Pages.ControlPanel;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AppearanceAndPersonalization : Page
{
    public AppearanceAndPersonalization()
    {
        this.InitializeComponent();
        if (App.cpanelWin != null) App.cpanelWin.SetWindowIcon("Assets\\AppIcons\\imageres_197.ico");
        //Read();
        if (App.cpanelWin != null) App.cpanelWin.Title = "Appearance and Personalization";
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
        if (App.cpanelWin != null)
        {
            App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
    }
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
            App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
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
            App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
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

    private void OpenFileExplorerOptions()
    {
        // Constants for ShellExecute
        const int SW_SHOWNORMAL = 1;

        // Call ShellExecute to open the File Explorer Options dialog
        ShellExecute(IntPtr.Zero, "open", "control.exe", "/name Microsoft.FolderOptions", null, SW_SHOWNORMAL);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr ShellExecute(IntPtr hWnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

    private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem == TBAndNav)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:taskbar"));
        }
        if (sender.SelectedItem == Access)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess"));
        }
        if (sender.SelectedItem == ExpOptions)
        {
            OpenFileExplorerOptions();
        }
        if (sender.SelectedItem == Fonts)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:fonts"));
        }
        sender.SelectedItem = Rebound11Item;
    }
}
