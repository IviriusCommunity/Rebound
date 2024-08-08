using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundHub.ReboundHub.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();
        NavigationFrame.Navigate(typeof(HomePage));
        NavigationViewControl.SelectedItem = HomeItem;
    }

    private async void NavigationViewControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if ((string)(args.SelectedItem as NavigationViewItem).Tag == "Home")
        {
            NavigationFrame.Navigate(typeof(HomePage));
        }
        if ((string)(args.SelectedItem as NavigationViewItem).Tag == "Rebound 11")
        {
            NavigationFrame.Navigate(typeof(Rebound11Page));
        }
        if ((string)(args.SelectedItem as NavigationViewItem).Tag == "Control Panel")
        {
            var win = new ControlPanelWindow();
            App.cpanelWin = win;
            win.Show();
            win.SetWindowSize(1250, 750);
            win.CenterOnScreen();
            await Task.Delay(10);
            win.BringToFront();
            sender.SelectedItem = sender.MenuItems[2];
            App.m_window.Close();
            App.m_window = null;
        }
        //NavigationViewControl.Header = (string)(args.SelectedItem as NavigationViewItem).Tag;
    }
}
