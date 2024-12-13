using Microsoft.UI.Xaml.Media.Animation;

namespace Rebound.SysInfo.Views;
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private void OnSettingCard_Click(object sender, RoutedEventArgs e)
    {
        var item = sender as SettingsCard;
        if (item.Tag != null)
        {
            var pageType = Application.Current.GetType().Assembly.GetType($"Rebound.SysInfo.Views.{item.Tag}");

            if (pageType != null)
            {
                var entranceNavigation = new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                };
                _ = App.Current.JsonNavigationViewService.NavigateTo(pageType, item.Header, false, entranceNavigation);
            }
        }
    }
}

