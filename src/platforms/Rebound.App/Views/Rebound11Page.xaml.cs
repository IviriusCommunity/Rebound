using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ViewModels;

namespace Rebound.Views;

public partial class Rebound11Page : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Get Started",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/AppIcons/PropertiesWindow.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorPaid.png",
            Link = string.Empty
        },
        new LinkCard
        {
            Title = "Windows updates",
            Description = "Rebound does not disable Windows updates so you can enjoy fresh patches and releases.",
            IconPath = "/Assets/AppIcons/WindowsIcon.png",
            Link = string.Empty
        },
        new LinkCard
        {
            Title = "Rebound updates",
            Description = "All Rebound updates are easy to install via the \"Update or Repair all\" option.",
            IconPath = "/Assets/AppIcons/ReboundIcon.ico",
            Link = string.Empty
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

    public Rebound11Page()
    {
        InitializeComponent();
    }

    private void OnCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is AppCard card && !string.IsNullOrEmpty(card.Link))
        {
            var uri = new Uri(card.Link);
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}