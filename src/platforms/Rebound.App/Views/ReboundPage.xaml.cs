using System.Collections.ObjectModel;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rebound.Helpers;
using Rebound.ViewModels;
using Color = Windows.UI.Color;

namespace Rebound.Views;

public partial class GlassBrush : XamlCompositionBrushBase
{
    protected override void OnConnected()
    {
        var LuminosityOpacity = 1F; // Opacity for luminosity overlay
        var TintOpacity = 0.2F; // Opacity for luminosity overlay
        var TintColor = (App.MainAppWindow.Content as FrameworkElement).ActualTheme == ElementTheme.Light ? Color.FromArgb(255, 223, 223, 223) : Color.FromArgb(255, 32, 32, 32); // Opacity for luminosity overlay

        var baseBrush = App.MainAppWindow.Compositor.CreateBackdropBrush();

        // --------- Blur Effect ---------
        var blurEffect = new GaussianBlurEffect
        {
            BlurAmount = 16,
            Source = new CompositionEffectSourceParameter("Wallpaper"),
            BorderMode = EffectBorderMode.Hard
        };

        var blurEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new CompositionEffectSourceParameter("Wallpaper"),
            Source2 = blurEffect,
            MultiplyAmount = 0,
            Source1Amount = 0,
            Source2Amount = 1,
            Offset = 0
        };

        var blurEffectFactory = App.MainAppWindow.Compositor.CreateEffectFactory(blurEffectComposite);
        var blurEffectBrush = blurEffectFactory.CreateBrush();

        blurEffectBrush.SetSourceParameter("Wallpaper", baseBrush);

        // --------- Luminosity Overlay Effect ---------
        var luminosityEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Color,
            Background = new CompositionEffectSourceParameter("BlurredWallpaper"),
            Foreground = new CompositionEffectSourceParameter("LuminosityOverlay")
        };

        var luminosityEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new CompositionEffectSourceParameter("BlurredWallpaper"),
            Source2 = luminosityEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - LuminosityOpacity,
            Source2Amount = LuminosityOpacity,
            Offset = 0
        };

        var luminosityEffectFactory = App.MainAppWindow.Compositor.CreateEffectFactory(luminosityEffectComposite);
        var luminosityEffectBrush = luminosityEffectFactory.CreateBrush();

        var luminosityTint = App.MainAppWindow.Compositor.CreateColorBrush(TintColor);
        luminosityEffectBrush.SetSourceParameter("BlurredWallpaper", blurEffectBrush);
        luminosityEffectBrush.SetSourceParameter("LuminosityOverlay", luminosityTint);

        // --------- Color Overlay Effect ---------
        var colorEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Luminosity,
            Background = new CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Foreground = new CompositionEffectSourceParameter("ColorOverlay")
        };

        var colorEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Source2 = colorEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - TintOpacity,
            Source2Amount = TintOpacity,
            Offset = 0
        };

        var colorEffectFactory = App.MainAppWindow.Compositor.CreateEffectFactory(colorEffectComposite);
        var colorEffectBrush = colorEffectFactory.CreateBrush();

        var colorTint = App.MainAppWindow.Compositor.CreateColorBrush(TintColor);
        colorEffectBrush.SetSourceParameter("LuminosityEffectOutput", luminosityEffectBrush); // Set luminosityEffectBrush as input
        colorEffectBrush.SetSourceParameter("ColorOverlay", colorTint);

        // Return the final brush with both effects applied
        CompositionBrush = colorEffectBrush;
    }
}

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
            BKGImage.Visibility = CardsScrollViewer.Visibility = TitleGrid.Visibility = Visibility.Collapsed;
            Row1.Height = Row2.Height = new(0);
        }
    }
}