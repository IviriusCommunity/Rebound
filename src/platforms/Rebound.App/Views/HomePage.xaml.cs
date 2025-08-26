using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.Views;

internal class LinkCard
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public string? Link { get; set; }
}

internal class AppCard
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public string? PicturePath { get; set; }
    public string? Link { get; set; }
    public string? Publisher { get; set; }
    public Color AccentColor { get; set; }
}

internal sealed partial class HomePage : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditor.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new LinkCard
        {
            Title = "Ivirius Text Editor Plus",
            Description = "Fluent WordPad app with advanced features and beautiful UI.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorPlus.png",
            Link = "https://ivirius.com/ivirius-text-editor-plus/"
        },
        new LinkCard
        {
            Title = "Rebound",
            Description = "The first WinUI Windows mod that aims to bring consistency to the OS.",
            IconPath = "/Assets/AppIcons/Rebound.ico",
            Link = "https://ivirius.com/rebound/"
        },
        new LinkCard
        {
            Title = "Docs",
            Description = "Learn more about how to use Ivirius apps and contribute to projects.",
            IconPath = "/Assets/Glyphs/Docs.png",
            Link = "https://ivirius.com/docs/"
        },
        new LinkCard
        {
            Title = "Discord Server",
            Description = "Talk with the developers, give feedback, and have a good time in our Discord server!",
            IconPath = "/Assets/Glyphs/DiscordLogo.png",
            Link = "https://ivirius.com/discord/"
        }
    ];

    private ObservableCollection<AppCard> AppCards { get; } =
    [
        new AppCard
        {
            Title = "Ambie",
            Description = "Focus, study, or relax. Sounds for every mood.",
            IconPath = "/Assets/AppIcons/PartnerApps/Ambie.png",
            PicturePath = "/Assets/AppBanners/Ambie.jpeg",
            Link = "https://ambieapp.com/",
            Publisher = "Jenius Apps",
            AccentColor = Color.FromArgb(255, 255, 204, 0)
        },
        new AppCard
        {
            Title = "Character Map UWP",
            Description = "A modern, native UWP replacement for the Win32 Character Map.",
            IconPath = "/Assets/AppIcons/PartnerApps/Character Map UWP.png",
            PicturePath = "/Assets/AppBanners/Character Map UWP.png",
            Link = "https://apps.microsoft.com/detail/9wzdncrdxf41",
            Publisher = "Edi Wang",
            AccentColor = Color.FromArgb(255, 0, 110, 255)
        },
        new AppCard
        {
            Title = "Files",
            Description = "Fluent file explorer app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/PartnerApps/Files.png",
            PicturePath = "/Assets/AppBanners/Files.png",
            Link = "https://files.community/",
            Publisher = "Yair A",
            AccentColor = Color.FromArgb(255, 255, 115, 0)
        },
        new AppCard
        {
            Title = "Fluent Store",
            Description = "A unifying frontend for Windows app stores and package managers.",
            IconPath = "/Assets/AppIcons/PartnerApps/Fluent Store.png",
            PicturePath = "/Assets/AppBanners/Fluent Store.png",
            Link = "https://josh.askharoun.com/fluentstore",
            Publisher = "yoshiask",
            AccentColor = Color.FromArgb(255, 3, 158, 0)
        },
        new AppCard
        {
            Title = "FluentHub",
            Description = "Stylish yet powerful GitHub client for Windows.",
            IconPath = "/Assets/AppIcons/PartnerApps/FluentHub.png",
            PicturePath = "/Assets/AppBanners/FluentHub.png",
            Link = "https://apps.microsoft.com/detail/9nkb9hx8rjz3",
            Publisher = "0x5BFA",
            AccentColor = Color.FromArgb(255, 209, 0, 115)
        },
        new AppCard
        {
            Title = "Fluetro PDF",
            Description = "Modern pdf viewer designed for Windows 11.",
            IconPath = "/Assets/AppIcons/PartnerApps/Fluetro.png",
            PicturePath = "/Assets/AppBanners/Fluetro.jpeg",
            Link = "https://apps.microsoft.com/detail/9nsr7b2lt6ln",
            Publisher = "FireCubeStudios",
            AccentColor = Color.FromArgb(255, 209, 0, 0)
        },
        new AppCard
        {
            Title = "PowerToys",
            Description = "A set of utilities for power users to tune their Windows experience.",
            IconPath = "/Assets/AppIcons/PartnerApps/PowerToys.png",
            PicturePath = "/Assets/AppBanners/PowerToys.png",
            Link = "https://learn.microsoft.com/en-us/windows/powertoys/",
            Publisher = "Microsoft Corporation",
            AccentColor = Color.FromArgb(255, 255, 228, 161)
        },
        new AppCard
        {
            Title = "Scanner",
            Description = "An all-in-one scanner app built for the Universal Windows Platform.",
            IconPath = "/Assets/AppIcons/PartnerApps/Scanner.png",
            PicturePath = "/Assets/AppBanners/Scanner.jpg",
            Link = "https://simon-knuth.github.io/scanner/",
            Publisher = "Simon Knuth",
            AccentColor = Color.FromArgb(255, 207, 86, 0)
        },
        new AppCard
        {
            Title = "Screenbox",
            Description = "Modern media player for all your devices.",
            IconPath = "/Assets/AppIcons/PartnerApps/Screenbox.png",
            PicturePath = "/Assets/AppBanners/Screenbox.png",
            Link = "https://apps.microsoft.com/detail/9ntsnmsvcb5l",
            Publisher = "Tung H.",
            AccentColor = Color.FromArgb(255, 0, 168, 224)
        },
        new AppCard
        {
            Title = "SecureFolderFS",
            Description = "SecureFolderFS lets you securely access your files.",
            IconPath = "/Assets/AppIcons/PartnerApps/SecureFolderFS.png",
            PicturePath = "/Assets/AppBanners/SecureFolderFS.png",
            Link = "https://www.microsoft.com/store/apps/9NZ7CZRN7GG8",
            Publisher = "d2dyno",
            AccentColor = Color.FromArgb(255, 0, 209, 224)
        },
        new AppCard
        {
            Title = "Wino Mail",
            Description = "Native mail client for Windows device families.",
            IconPath = "/Assets/AppIcons/PartnerApps/WinoMail.png",
            PicturePath = "/Assets/AppBanners/Wino Mail.png",
            Link = "https://apps.microsoft.com/detail/9ncrcvjc50wl",
            Publisher = "Burak Kaan Köse",
            AccentColor = Color.FromArgb(255, 0, 149, 224)
        },
    ];

    internal HomePage()
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