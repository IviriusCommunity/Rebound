using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Rebound.Core.Helpers;
using Rebound.ViewModels;

namespace Rebound.Views;

public partial class ReboundPage : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Get Started",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/Glyphs/DesktopVerify.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/Glyphs/WinUI.png",
            Link = "https://learn.microsoft.com/en-us/windows/apps/winui/winui3/"
        },
        new LinkCard
        {
            Title = "Windows updates",
            Description = "Rebound does not disable Windows updates so you can enjoy fresh patches and releases.",
            IconPath = "/Assets/Glyphs/Update.ico",
            Link = "https://support.microsoft.com/en-us/windows/install-windows-updates-3c5ae7fc-9fb6-9af1-1984-b5e0412c556a"
        },
        new LinkCard
        {
            Title = "Rebound updates",
            Description = "All Rebound updates are easy to install via the \"Update or Repair all\" option.",
            IconPath = "/Assets/Glyphs/Restart.ico",
            Link = "https://ivirius.com/rebound"
        },
        new LinkCard
        {
            Title = "GitHub",
            Description = "Star the repo and contribute to the project!",
            IconPath = "/Assets/Glyphs/GitHub.png",
            Link = "https://github.com/IviriusCommunity/Rebound"
        }
    ];

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