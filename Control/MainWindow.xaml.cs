using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rebound.Control.Views;
using Rebound.Helpers;
using Windows.System;
using WinUIEx;

namespace Rebound.Control;

public sealed partial class MainWindow : WindowEx
{
    public const string CPL_HOME = @"Control Panel";
    public const string CPL_APPEARANCE_AND_PERSONALIZATION = @"Control Panel\Appearance and Personalization";
    public const string CPL_SYSTEM_AND_SECURITY = @"Control Panel\System and Security";
    public const string CPL_WINDOWS_TOOLS = @"Control Panel\System and Security\Windows Tools";

    public TitleBarService TitleBarEx;

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        Title = "Control Panel";
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(ModernHomePage));
        this.CenterOnScreen();
        this.SetWindowSize(1250, 750);

        Read();

        AddressBox.Text = "Control Panel";
        AddressBox.ItemsSource = new List<string>()
        {
            CPL_HOME,
            CPL_APPEARANCE_AND_PERSONALIZATION,
            CPL_SYSTEM_AND_SECURITY,
            CPL_WINDOWS_TOOLS
        };
        NavigateToPath();
        RootFrame.BackStack.Clear();
        RootFrame.ForwardStack.Clear();

        TitleBarEx = new TitleBarService(this, AccentStrip, TitleBarIcon, WindowTitle, Close, CrimsonMaxRes, Minimize, MaxResGlyph, WindowContent);
        TitleBarEx.SetWindowIcon("AppRT\\Products\\Associated\\rcontrol.ico");

        var rects = Display.GetDPIAwareDisplayRect(this);
        if (rects.Height < 900 || rects.Width < 1200)
        {
            this.SetWindowSize(rects.Width - 200, rects.Height - 200);
        }
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
        switch (CurrentPage())
        {
            case CPL_HOME:
                {
                    await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
                    App.ControlPanelWindow?.Close();
                    break;
                }
            case CPL_APPEARANCE_AND_PERSONALIZATION:
                {
                    AddressBox.Text = CPL_HOME;
                    NavigateToPath();
                    break;
                }
            case CPL_SYSTEM_AND_SECURITY:
                {
                    AddressBox.Text = CPL_HOME;
                    NavigateToPath();
                    break;
                }
            case CPL_WINDOWS_TOOLS:
                {
                    AddressBox.Text = CPL_SYSTEM_AND_SECURITY;
                    NavigateToPath();
                    break;
                }
            default:
                {
                    await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
                    App.ControlPanelWindow?.Close();
                    break;
                }
        }
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
        AddressBox.Text = CPL_HOME;
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = CPL_APPEARANCE_AND_PERSONALIZATION;
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = CPL_SYSTEM_AND_SECURITY;
        NavigateToPath();
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.ControlPanelWindow = null;
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
            case Views.AppearanceAndPersonalization:
                {
                    return CPL_APPEARANCE_AND_PERSONALIZATION;
                }
            case Views.SystemAndSecurity:
                {
                    return CPL_SYSTEM_AND_SECURITY;
                }
            case Views.WindowsTools:
                {
                    return CPL_WINDOWS_TOOLS;
                }
            case ModernHomePage:
                {
                    return CPL_HOME;
                }
            case HomePage:
                {
                    return CPL_HOME;
                }
            default:
                {
                    return CPL_HOME;
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

    public async void NavigateToPath(bool legacyHomePage = false)
    {
        HideAll();
        RootFrame.Focus(FocusState.Programmatic);
        switch (AddressBox.Text)
        {
            case CPL_APPEARANCE_AND_PERSONALIZATION:
                {
                    if (RootFrame.Content is not Views.AppearanceAndPersonalization)
                    {
                        App.ControlPanelWindow?.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    AppearanceAndPersonalization.Visibility = Visibility.Visible;
                    return;
                }
            case CPL_SYSTEM_AND_SECURITY:
                {
                    if (RootFrame.Content is not Views.SystemAndSecurity)
                    {
                        App.ControlPanelWindow?.RootFrame.Navigate(typeof(SystemAndSecurity), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    SystemAndSecurity.Visibility = Visibility.Visible;
                    return;
                }
            case CPL_WINDOWS_TOOLS:
                {
                    if (RootFrame.Content is not Views.WindowsTools)
                    {
                        App.ControlPanelWindow?.RootFrame.Navigate(typeof(WindowsTools), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    SystemAndSecurity.Visibility = Visibility.Visible;
                    WindowsTools.Visibility = Visibility.Visible;
                    return;
                }
            case CPL_HOME:
                {
                    if (legacyHomePage == false && RootFrame.Content is not ModernHomePage)
                    {
                        App.ControlPanelWindow?.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    else if (legacyHomePage != false && RootFrame.Content is not HomePage)
                    {
                        App.ControlPanelWindow?.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    return;
                }
            default:
                {
                    try
                    {
                        await Launcher.LaunchUriAsync(new Uri(AddressBox.Text));
                        Close();
                        return;
                    }
                    catch
                    {
                        AddressBox.Text = CurrentPage();
                    }
                    return;
                }
        }
    }

    private async void AddressBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        await Task.Delay(10);
        NavigateToPath();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr ShellExecute(IntPtr hWnd, string lpOperation, string lpFile, string lpParameters, string? lpDirectory, int nShowCmd);

    private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
    {
        // Constants for ShellExecute
        const int SW_SHOWNORMAL = 1;

        // Call ShellExecute to open the File Explorer Options dialog
        ShellExecute(IntPtr.Zero, "open", "control.exe", "/name Microsoft.FolderOptions", null, SW_SHOWNORMAL);
    }

    private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = CPL_WINDOWS_TOOLS;
        NavigateToPath();
    }
}