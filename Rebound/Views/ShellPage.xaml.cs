using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();
        Debug.WriteLine(@$"{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)}\Programs\Rebound 11 Tools");
        //NavigationFrame.Navigate(typeof(HomePage));
        NavigationViewControl.SelectedItem = HomeItem;

        CheckLaunch();
    }

    public async void CheckLaunch()
    {
        await Task.Delay(500);
        if (string.Join(" ", Environment.GetCommandLineArgs().Skip(1)).Contains("INSTALLREBOUND11"))
        {
            NavigationViewControl.SelectedItem = Rebound11Item;
        }
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
            /*var win = new ControlPanelWindow();
            App.cpanelWin = win;
            win.Show();
            win.CenterOnScreen();
            await Task.Delay(10);
            win.BringToFront();
            sender.SelectedItem = sender.MenuItems[2];
            App.m_window.Close();
            App.m_window = null;*/
        }
        //NavigationViewControl.Header = (string)(args.SelectedItem as NavigationViewItem).Tag;
    }

    private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        NavigationFrame.GoBack();
    }
}
