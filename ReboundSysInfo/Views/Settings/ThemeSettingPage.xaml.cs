using Windows.System;

namespace ReboundSysInfo.Views;
public sealed partial class ThemeSettingPage : Page
{
    public string BreadCrumbBarItemText { get; set; }

    public ThemeSettingPage()
    {
        this.InitializeComponent();
        Loaded += ThemeSettingPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }

    private void ThemeSettingPage_Loaded(object sender, RoutedEventArgs e)
    {
        App.Current.ThemeService.SetThemeComboBoxDefaultItem(CmbTheme);
        App.Current.ThemeService.SetBackdropComboBoxDefaultItem(CmbBackdrop);
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        App.Current.ThemeService.OnThemeComboBoxSelectionChanged(sender);
    }

    private void CmbBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        App.Current.ThemeService.OnBackdropComboBoxSelectionChanged(sender);
    }

    private async void OpenWindowsColorSettings(object sender, RoutedEventArgs e)
    {
        _ = await Launcher.LaunchUriAsync(new Uri("ms-settings:colors"));
    }
}


