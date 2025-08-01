using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}

internal sealed partial class HomePage : Page
{
    private ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new LinkCard
        {
            Title = "Ivirius Text Editor Plus",
            Description = "Fluent WordPad app with advanced features and beautiful UI.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorPaid.png",
            Link = "https://ivirius.com/ivirius-text-editor-plus/"
        },
        new LinkCard
        {
            Title = "Rebound",
            Description = "The first WinUI 3 Windows mod that aims to bring consistency to the OS.",
            IconPath = "/Assets/AppIcons/ReboundIcon.ico",
            Link = "https://ivirius.com/rebound/"
        },
        new LinkCard
        {
            Title = "CubeKit",
            Description = "Advanced toolkit for WinUI 3, UWP, and WPF apps, featuring CrimsonUI.",
            IconPath = "/Assets/AppIcons/CubeKit.png",
            Link = "https://github.com/Lamparter/CubeKit/"
        },
        new LinkCard
        {
            Title = "Docs",
            Description = "Learn more about how to use Ivirius apps and contribute to projects.",
            IconPath = "/Assets/AppIcons/Docs.png",
            Link = "https://ivirius.com/docs/"
        },
        new LinkCard
        {
            Title = "Discord Server",
            Description = "Talk with the developers, give feedback, and have a good time in our Discord server!",
            IconPath = "/Assets/AppIcons/DiscordLogo.png",
            Link = "https://discord.com/invite/uasSwW5U2B/"
        }
    ];

    private ObservableCollection<AppCard> AppCards { get; } =
    [
        new AppCard
        {
            Title = "Ambie",
            Description = "Focus, study, or relax. Sounds for every mood.",
            IconPath = "/Assets/AppIcons/Ambie.png",
            PicturePath = "/Assets/AppBanners/Ambie.jpeg",
            Link = "https://ambieapp.com/",
            Publisher = "Jenius Apps"
        },
        new AppCard
        {
            Title = "Character Map UWP",
            Description = "A modern, native UWP replacement for the Win32 Character Map.",
            IconPath = "/Assets/AppIcons/Character Map UWP.png",
            PicturePath = "/Assets/AppBanners/Character Map UWP.png",
            Link = "https://apps.microsoft.com/detail/9wzdncrdxf41",
            Publisher = "Edi Wang"
        },
        new AppCard
        {
            Title = "Files",
            Description = "Fluent file explorer app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/Files.png",
            PicturePath = "/Assets/AppBanners/Files.png",
            Link = "https://files.community/",
            Publisher = "Yair A"
        },
        new AppCard
        {
            Title = "Fluent Store",
            Description = "A unifying frontend for Windows app stores and package managers.",
            IconPath = "/Assets/AppIcons/Fluent Store.png",
            PicturePath = "/Assets/AppBanners/Fluent Store.png",
            Link = "https://josh.askharoun.com/fluentstore",
            Publisher = "yoshiask"
        },
        new AppCard
        {
            Title = "FluentHub",
            Description = "Stylish yet powerful GitHub client for Windows.",
            IconPath = "/Assets/AppIcons/FluentHub.png",
            PicturePath = "/Assets/AppBanners/FluentHub.png",
            Link = "https://apps.microsoft.com/detail/9nkb9hx8rjz3",
            Publisher = "0x5BFA"
        },
        new AppCard
        {
            Title = "Fluetro PDF",
            Description = "Modern pdf viewer designed for Windows 11.",
            IconPath = "/Assets/AppIcons/Fluetro.png",
            PicturePath = "/Assets/AppBanners/Fluetro.jpeg",
            Link = "https://apps.microsoft.com/detail/9nsr7b2lt6ln",
            Publisher = "FireCubeStudios"
        },
        new AppCard
        {
            Title = "PowerToys",
            Description = "A set of utilities for power users to tune their Windows experience.",
            IconPath = "/Assets/AppIcons/PowerToys.png",
            PicturePath = "/Assets/AppBanners/PowerToys.png",
            Link = "https://learn.microsoft.com/en-us/windows/powertoys/",
            Publisher = "Microsoft Corporation"
        },
        new AppCard
        {
            Title = "Scanner",
            Description = "An all-in-one scanner app built for the Universal Windows Platform.",
            IconPath = "/Assets/AppIcons/Scanner.png",
            PicturePath = "/Assets/AppBanners/Scanner.jpg",
            Link = "https://simon-knuth.github.io/scanner/",
            Publisher = "Simon Knuth"
        },
        new AppCard
        {
            Title = "Screenbox",
            Description = "Modern media player for all your devices.",
            IconPath = "/Assets/AppIcons/Screenbox.png",
            PicturePath = "/Assets/AppBanners/Screenbox.png",
            Link = "https://apps.microsoft.com/detail/9ntsnmsvcb5l",
            Publisher = "Tung H."
        },
        new AppCard
        {
            Title = "SecureFolderFS",
            Description = "SecureFolderFS lets you securely access your files.",
            IconPath = "/Assets/AppIcons/SecureFolderFS.png",
            PicturePath = "/Assets/AppBanners/SecureFolderFS.png",
            Link = "https://www.microsoft.com/store/apps/9NZ7CZRN7GG8",
            Publisher = "d2dyno"
        },
        new AppCard
        {
            Title = "Wino Mail",
            Description = "Native mail client for Windows device families.",
            IconPath = "/Assets/AppIcons/WinoMail.png",
            PicturePath = "/Assets/AppBanners/Wino Mail.png",
            Link = "https://apps.microsoft.com/detail/9ncrcvjc50wl",
            Publisher = "Burak Kaan Köse"
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