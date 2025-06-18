using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rebound.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Rectify11Page : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Windhawk based",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/AppIcons/PropertiesWindow.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/AppIcons/WinUI.png",
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

    public Rectify11Page()
    {
        InitializeComponent();
    }
}
