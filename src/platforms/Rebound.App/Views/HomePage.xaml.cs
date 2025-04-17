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
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
        },
        new AppCard
        {
            Title = "Ivirius Text Editor",
            Description = "Fluent WordPad app for Windows 10 and 11.",
            IconPath = "/Assets/AppIcons/IviriusTextEditorFree.png",
            PicturePath = "/Assets/Backgrounds/BackgroundDarkNew.png",
            Link = "https://ivirius.com/ivirius-text-editor/"
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