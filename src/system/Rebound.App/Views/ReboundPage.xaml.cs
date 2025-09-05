using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.ViewModels;

namespace Rebound.Views;

public partial class ReboundPage : Page
{
    public ReboundViewModel ReboundViewModel { get; set; } = new();

    public ReboundPage()
    {
        DataContext = ReboundViewModel;
        InitializeComponent();
        if (!SettingsHelper.GetValue("ShowBranding", "rebound", true))
        {
            BKGImage.Visibility = CardsScrollViewer.Visibility = TitleGrid.Visibility = Visibility.Collapsed;
            Row1.Height = Row2.Height = new(0);
        }
    }
}