using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rebound.Helpers;
using Rebound.Pages.ControlPanel;
using Windows.System;
using WinUIEx;

namespace Rebound;

public sealed partial class ControlPanelWindow : WindowEx
{
    public TitleBarService TitleBarEx;

    public ControlPanelWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        Title = "Control Panel";
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(ModernHomePage));

        Read();

        AddressBox.Text = "Control Panel";
        AddressBox.ItemsSource = new List<string>()
        {
            @"Control Panel",
            @"Control Panel\Appearance and Personalization",
            @"Control Panel\System and Security",
        };
        NavigateToPath();
        RootFrame.BackStack.Clear();
        RootFrame.ForwardStack.Clear();

        TitleBarEx = new TitleBarService(this, AccentStrip, TitleBarIcon, WindowTitle, Close, CrimsonMaxRes, Minimize, MaxResGlyph, WindowContent);
        TitleBarEx.SetWindowIcon("Assets\\AppIcons\\rcontrol.ico");
    }

    private async void Read()
    {
        await Task.Delay(50);
        try
        {
            BackButton.IsEnabled = RootFrame.CanGoBack;
            ForwardButton.IsEnabled = RootFrame.CanGoForward;

            Read();
        }
        catch
        {

        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.GoBack();
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.GoForward();
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        App.cpanelWin.Close();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var oldHistory = RootFrame.ForwardStack;
        var newList = new List<PageStackEntry>();
        foreach (var item in oldHistory)
        {
            newList.Add(item);
        }
        RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        RootFrame.GoBack();
        RootFrame.ForwardStack.Clear();
        foreach (var item in newList)
        {
            RootFrame.ForwardStack.Add(item);
        }
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel";
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel\Appearance and Personalization";
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel\System and Security";
        NavigateToPath();
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.cpanelWin = null;
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.Visibility = Visibility.Visible;
        AddressBox.Visibility = Visibility.Collapsed;
    }

    private async void AddressBar_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        AddressBar.Visibility = Visibility.Collapsed;
        AddressBox.Visibility = Visibility.Visible;
        await Task.Delay(10);
        AddressBox.Focus(FocusState.Programmatic);
    }

    public void HideAll()
    {
        AppearanceAndPersonalization.Visibility = Visibility.Collapsed;
        SystemAndSecurity.Visibility = Visibility.Collapsed;
        WindowsTools.Visibility = Visibility.Collapsed;
    }

    public string CurrentPage()
    {
        switch (RootFrame.Content)
        {
            case Rebound.Pages.ControlPanel.AppearanceAndPersonalization:
                {
                    return @"Control Panel\Appearance and Personalization";
                }
            case Rebound.Pages.ControlPanel.SystemAndSecurity:
                {
                    return @"Control Panel\System and Security";
                }
            case Rebound.Pages.ControlPanel.WindowsTools:
                {
                    return @"Control Panel\System and Security\Windows Tools";
                }
            case ModernHomePage:
                {
                    return @"Control Panel";
                }
            case HomePage:
                {
                    return @"Control Panel";
                }
            default:
                {
                    return @"Control Panel";
                }
        }
    }

    private void AddressBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            NavigateToPath();
        }
    }

    public void NavigateToPath(bool legacyHomePage = false)
    {
        HideAll();
        RootFrame.Focus(FocusState.Programmatic);
        switch (AddressBox.Text)
        {
            case @"Control Panel\Appearance and Personalization":
                {
                    if (RootFrame.Content is not Rebound.Pages.ControlPanel.AppearanceAndPersonalization)
                    {
                        App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    AppearanceAndPersonalization.Visibility = Visibility.Visible;
                    return;
                }
            case @"Control Panel\System and Security":
                {
                    if (RootFrame.Content is not Rebound.Pages.ControlPanel.SystemAndSecurity)
                    {
                        App.cpanelWin.RootFrame.Navigate(typeof(SystemAndSecurity), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    SystemAndSecurity.Visibility = Visibility.Visible;
                    return;
                }
            case @"Control Panel\System and Security\Windows Tools":
                {
                    if (RootFrame.Content is not Rebound.Pages.ControlPanel.WindowsTools)
                    {
                        App.cpanelWin.RootFrame.Navigate(typeof(WindowsTools), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    SystemAndSecurity.Visibility = Visibility.Visible;
                    WindowsTools.Visibility = Visibility.Visible;
                    return;
                }
            case @"Control Panel":
                {
                    if (legacyHomePage == false && RootFrame.Content is not ModernHomePage)
                    {
                        App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    else if (legacyHomePage != false && RootFrame.Content is not HomePage)
                    {
                        App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    return;
                }
            default:
                {
                    AddressBox.Text = CurrentPage();
                    return;
                }
        }
    }

    private async void AddressBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        await Task.Delay(10);
        NavigateToPath();
    }
}