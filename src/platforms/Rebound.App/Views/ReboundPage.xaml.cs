using System.Collections.ObjectModel;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Rebound.Helpers;
using Rebound.ViewModels;
using Color = Windows.UI.Color;
using Windows.UI.Core;
using Windows.UI.Xaml.Hosting;

namespace Rebound.Views;

public partial class ReboundPage : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Get Started",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/AppIcons/WerFault_100.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/AppIcons/WinUI.png",
            Link = "https://learn.microsoft.com/en-us/windows/apps/winui/winui3/"
        },
        new LinkCard
        {
            Title = "Windows updates",
            Description = "Rebound does not disable Windows updates so you can enjoy fresh patches and releases.",
            IconPath = "/Assets/AppIcons/shell32_16739.ico",
            Link = "https://support.microsoft.com/en-us/windows/install-windows-updates-3c5ae7fc-9fb6-9af1-1984-b5e0412c556a"
        },
        new LinkCard
        {
            Title = "Rebound updates",
            Description = "All Rebound updates are easy to install via the \"Update or Repair all\" option.",
            IconPath = "/Assets/AppIcons/shell32_47.ico",
            Link = "https://ivirius.com/rebound"
        },
        new LinkCard
        {
            Title = "GitHub",
            Description = "Star the repo and contribute to the project!",
            IconPath = "/Assets/AppIcons/GitHub.png",
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
            //BKGImage.Visibility = CardsScrollViewer.Visibility = TitleGrid.Visibility = Visibility.Collapsed;
            Row1.Height = Row2.Height = new(0);
        }
    }
}