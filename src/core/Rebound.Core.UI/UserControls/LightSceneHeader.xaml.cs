// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.Threading;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace Rebound.Core.UI.UserControls;

public sealed partial class LightSceneHeader : UserControl
{
    // ------------------------------------------------------------------
    // Tuning
    // ------------------------------------------------------------------

    /// <summary>The beam, its centre light and the flare all use this one colour (#73a7fa).</summary>
    private Color BeamColor { get => (Color)Resources["SystemAccentColor"]; }

    /// <summary>Distance from the right edge to the beam's centre point.</summary>
    private readonly float BeamRightInset = 120f;

    /// <summary>Half-length of the beam as a fraction of the centre's distance from the left edge.</summary>
    private const float BeamHalfLengthFraction = 0.9f;

    /// <summary>
    /// How far the beam's edge follows the cursor, as a fraction of the cursor's
    /// distance from the edge. 0.5 means the beam shrinks by at most 50% (when the
    /// cursor reaches the centre point).
    /// </summary>
    private const float BeamCursorPull = 0.5f;

    private const float CursorGlowSize = 100f;
    private const float CursorDim = 0.5f;

    private const float FlareIdleOpacity = 0.55f;

    /// <summary>Fraction of the remaining distance the flare covers each frame (0..1).</summary>
    private const float FlareFollowSpeed = 0.2f;

    private readonly record struct BeamLayer(
        float Height,
        (float Offset, int Alpha)[] Stops,
        float ShimmerSeconds);

    // Stretched radial gradients: soft on every edge and mirrored around the centre.
    private static readonly BeamLayer[] BeamLayers =
    [
        //new(160f, [(0f, 50), (0.35f, 32), (0.7f, 12), (0.9f, 3), (1f, 0)], 6.5f),     // wide halo
        //new(100f,  [(0f, 105), (0.4f, 82), (0.7f, 38), (0.9f, 9), (1f, 0)], 5.0f),     // body
        new(32f,  [(0f, 190), (0.3f, 125), (0.6f, 55), (0.85f, 14), (1f, 0)], 1.6f),  // soft core
    ];

    /// <summary>
    /// One element of the solar reflection. Its position is
    /// beamCentre + T * (focus - beamCentre): negative T lands on the opposite
    /// side of the light from the focus ("against"), positive T lies towards
    /// and beyond it.
    /// </summary>
    private readonly record struct FlareGhost(float T, float Size, bool Ring, int Alpha);

    private static readonly FlareGhost[] FlareGhosts =
    [
        new(-0.67f,  24f, false, 87),
        new(-0.30f,  70f, true,  42),
        new( 0.18f,  34f, false, 70),
        new( 0.40f, 90f, true,  30),
        new( 0.62f,  22f, false, 95),
        new( 0.90f,  64f, false, 42),
        new( 1.30f, 140f, true,  24),
        new( 1.75f,  46f, false, 58),
        new( 2.17f, 110f, true,  32),
        new( 2.52f,  38f, false, 48),
    ];

    // ------------------------------------------------------------------
    // Dependency properties
    // ------------------------------------------------------------------

    public static readonly DependencyProperty SceneContentProperty =
        DependencyProperty.Register(
            nameof(SceneContent),
            typeof(object),
            typeof(LightSceneHeader),
            new PropertyMetadata(null));

    public object SceneContent
    {
        get => GetValue(SceneContentProperty);
        set => SetValue(SceneContentProperty, value);
    }

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------

    private Compositor? _compositor;
    private ContainerVisual? _sceneRoot;
    private bool _initialized;

    private readonly List<IDisposable> _resources = [];

    private T Track<T>(T obj) where T : IDisposable
    {
        _resources.Add(obj);
        return obj;
    }

    private CubicBezierEasingFunction? _easing;

    private readonly List<SpriteVisual> _beamLayers = [];
    private ContainerVisual? _centerStar;
    private ContainerVisual? _flare;
    private readonly List<SpriteVisual> _ghosts = [];
    private SpriteVisual? _dim;
    private SpriteVisual? _cursorGlow;

    private Vector2 _cursor;
    private bool _hasPointer;

    private Vector2 _beamCenter;
    private Vector2 _flareFocus;
    private Vector2 _flareFocusTarget;
    private bool _renderingHooked;

    private float _beamHalfLength;
    private float _beamHalfLengthTarget;

    // ------------------------------------------------------------------
    // Lifetime
    // ------------------------------------------------------------------

    public LightSceneHeader()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var wallpaper = await STAThread.RunOnSTAThread(() =>
        {
            string w = string.Empty;
            try { w = UserInformation.GetWallpaperPath()!; } catch { }
            return w;
        }).ConfigureAwait(true);

        if (wallpaper != null) WallpaperSurface.Source = new BitmapImage(new Uri(wallpaper));
        InitializeScene();
    }

    private void InitializeScene()
    {
        if (_initialized ||
            RootGrid.ActualWidth <= 0 ||
            RootGrid.ActualHeight <= 0)
        {
            return;
        }

        var hostVisual = ElementCompositionPreview.GetElementVisual(CompositionHost);
        _compositor = hostVisual.Compositor;

        _easing = Track(_compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.05f, 0f),
            new Vector2(0.95f, 1f)));

        _sceneRoot = Track(_compositor.CreateContainerVisual());
        _sceneRoot.Size = new Vector2(
            (float)RootGrid.ActualWidth,
            (float)RootGrid.ActualHeight);

        ElementCompositionPreview.SetElementChildVisual(CompositionHost, _sceneRoot);

        // Creation order is z-order (bottom to top).
        CreateBeam();
        CreateCenterStar();
        CreateFlare();
        CreateDimOverlay();
        CreateCursorGlow();

        _initialized = true;

        // Place everything, and start the flare on its resting position.
        LayoutScene();
        _flareFocus = _flareFocusTarget;
        PositionFlare();

        // Opacity only changes on enter/exit, so implicit animations are safe here.
        AddOpacitySmoothing(_flare!, 300);
        AddOpacitySmoothing(_dim!, 450);
        AddOpacitySmoothing(_cursorGlow!, 160);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnhookRendering();

        ElementCompositionPreview.SetElementChildVisual(CompositionHost, null);
        _sceneRoot?.Children.RemoveAll();

        for (int i = _resources.Count - 1; i >= 0; i--)
            _resources[i].Dispose();

        _resources.Clear();
        _beamLayers.Clear();
        _ghosts.Clear();

        _centerStar = null;
        _flare = null;
        _dim = null;
        _cursorGlow = null;
        _easing = null;
        _sceneRoot = null;
        _compositor = null;
        _initialized = false;
        _hasPointer = false;
    }

    // ------------------------------------------------------------------
    // Brush / visual helpers
    // ------------------------------------------------------------------

    private CompositionRadialGradientBrush CreateRadialBrush(
        Color color,
        (float Offset, int Alpha)[] stops)
    {
        var brush = Track(_compositor!.CreateRadialGradientBrush());
        brush.MappingMode = CompositionMappingMode.Relative;
        brush.EllipseCenter = new Vector2(0.5f, 0.5f);
        brush.EllipseRadius = new Vector2(0.5f, 0.5f);

        foreach (var (offset, alpha) in stops)
        {
            brush.ColorStops.Add(Track(_compositor.CreateColorGradientStop(
                offset,
                Color.FromArgb((byte)alpha, color.R, color.G, color.B))));
        }

        return brush;
    }

    private static Color Lighten(Color color, float amount) =>
        Color.FromArgb(
            255,
            (byte)(color.R + (255 - color.R) * amount),
            (byte)(color.G + (255 - color.G) * amount),
            (byte)(color.B + (255 - color.B) * amount));

    private SpriteVisual AddCenteredSprite(
        ContainerVisual parent,
        float width,
        float height,
        CompositionBrush brush,
        float rotation = 0f,
        float opacity = 1f)
    {
        var visual = Track(_compositor!.CreateSpriteVisual());
        visual.Size = new Vector2(width, height);
        visual.Offset = new Vector3(-width / 2f, -height / 2f, 0f);
        visual.CenterPoint = new Vector3(width / 2f, height / 2f, 0f);
        visual.RotationAngleInDegrees = rotation;
        visual.Opacity = opacity;
        visual.Brush = brush;

        parent.Children.InsertAtTop(visual);
        return visual;
    }

    private void AddOpacitySmoothing(Visual visual, int milliseconds)
    {
        var collection = Track(_compositor!.CreateImplicitAnimationCollection());

        var animation = Track(_compositor.CreateScalarKeyFrameAnimation());
        animation.Target = "Opacity";
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = TimeSpan.FromMilliseconds(milliseconds);

        collection["Opacity"] = animation;
        visual.ImplicitAnimations = collection;
    }

    // ------------------------------------------------------------------
    // Scene pieces
    // ------------------------------------------------------------------

    private void CreateBeam()
    {
        foreach (var layer in BeamLayers)
        {
            var visual = Track(_compositor!.CreateSpriteVisual());
            visual.Brush = CreateRadialBrush(BeamColor, layer.Stops);

            // Gentle shimmer. The beam never moves vertically.
            var shimmer = Track(_compositor.CreateScalarKeyFrameAnimation());
            shimmer.InsertKeyFrame(0f, 0.5f);
            shimmer.InsertKeyFrame(0.5f, 1f, _easing);
            shimmer.InsertKeyFrame(1f, 0.5f, _easing);
            shimmer.Duration = TimeSpan.FromSeconds(layer.ShimmerSeconds);
            shimmer.IterationBehavior = AnimationIterationBehavior.Forever;
            visual.StartAnimation("Opacity", shimmer);

            _sceneRoot!.Children.InsertAtTop(visual);
            _beamLayers.Add(visual);
        }
    }

    private void CreateCenterStar()
    {
        // The light point at the middle of the beam: round halo, cross streaks, white core.
        var star = Track(_compositor!.CreateContainerVisual());

        AddCenteredSprite(star, 180f, 180f, CreateRadialBrush(
            BeamColor,
            [(0f, 235), (0.12f, 150), (0.35f, 55), (0.65f, 14), (1f, 0)]));

        // One shared brush for all streaks: a stretched radial gradient is soft
        // on every edge and tapers to a point at both ends.
        var streak = CreateRadialBrush(BeamColor, [(0f, 235), (0.2f, 120), (0.5f, 35), (1f, 0)]);
        var whiteStreak = CreateRadialBrush(Colors.White, [(0f, 235), (0.2f, 120), (0.5f, 35), (1f, 0)]);

        AddCenteredSprite(star, 400f, 48f, streak);
        //AddCenteredSprite(star, 240f, 48f, streak, rotation: 90f);
        AddCenteredSprite(star, 320f, 8f, whiteStreak);
        //AddCenteredSprite(star, 200f, 8f, whiteStreak, rotation: 90f);
        AddCenteredSprite(star, 360f, 80f, streak, rotation: 45f, opacity: 0.7f);
        AddCenteredSprite(star, 360f, 80f, streak, rotation: 135f, opacity: 0.7f);

        AddCenteredSprite(star, 32f, 32f, CreateRadialBrush(
            BeamColor,
            [(0.9f, 255), (1f, 0)]));

        AddCenteredSprite(star, 32f, 32f, CreateRadialBrush(
            Colors.White,
            [(0f, 255), (0.3f, 200), (1f, 0)]));

        AddCenteredSprite(star, 16f, 16f, CreateRadialBrush(
            Colors.White,
            [(0f, 255), (0.3f, 200), (1f, 0)]));

        var pulse = Track(_compositor.CreateScalarKeyFrameAnimation());
        pulse.InsertKeyFrame(0f, 0.70f);
        pulse.InsertKeyFrame(0.5f, 1f, _easing);
        pulse.InsertKeyFrame(1f, 0.70f, _easing);
        pulse.Duration = TimeSpan.FromSeconds(4);
        pulse.IterationBehavior = AnimationIterationBehavior.Forever;
        star.StartAnimation("Opacity", pulse);

        _sceneRoot!.Children.InsertAtTop(star);
        _centerStar = star;
    }

    private readonly List<Color> _flareColors =
    [
        Color.FromArgb(255, 255, 184, 108), // Gold
        Color.FromArgb(255, 255, 126, 182), // Pink
        Color.FromArgb(255, 199, 146, 234), // Violet
        Color.FromArgb(255, 130, 170, 255), // Blue
        Color.FromArgb(255, 137, 221, 255), // Cyan
        Color.FromArgb(255, 130, 230, 190), // Mint
    ];

    private void CreateFlare()
    {
        _flare = Track(_compositor!.CreateContainerVisual());
        _flare.Opacity = FlareIdleOpacity;

        for (int i = 0; i < FlareGhosts.Length; i++)
        {
            var ghost = FlareGhosts[i];
            var color = _flareColors[i % _flareColors.Count];

            CompositionBrush brush;

            if (ghost.Ring)
            {
                brush = CreateRadialBrush(
                    color,
                    [
                        (0f, 0),
                        (0.62f, 0),
                        (0.82f, ghost.Alpha),
                        (0.93f, (int)(ghost.Alpha * 0.4f)),
                        (1f, 0)
                    ]);
            }
            else
            {
                Color tint = ghost.Size < 40f ? Lighten(color, 0.5f) : color;

                brush = CreateRadialBrush(
                    tint,
                    [
                        (0f, ghost.Alpha),
                        (0.4f, (int)(ghost.Alpha * 0.55f)),
                        (0.75f, (int)(ghost.Alpha * 0.15f)),
                        (1f, 0)
                    ]);
            }

            var visual = Track(_compositor.CreateSpriteVisual());
            visual.Size = new Vector2(ghost.Size);
            visual.Brush = brush;

            _flare.Children.InsertAtTop(visual);
            _ghosts.Add(visual);
        }

        _sceneRoot!.Children.InsertAtTop(_flare);
    }

    private void CreateDimOverlay()
    {
        var linearGradientBrush = _compositor!.CreateLinearGradientBrush();
        var stop1 = _compositor.CreateColorGradientStop();
        stop1.Color = Colors.Black;
        stop1.Offset = 0.5f;
        linearGradientBrush.ColorStops.Add(stop1);
        var stop2 = _compositor.CreateColorGradientStop();
        stop2.Color = Color.FromArgb(0, 0, 0, 0);
        stop2.Offset = 0.75f;
        linearGradientBrush.ColorStops.Add(stop2);
        linearGradientBrush.StartPoint = new Vector2(0, 0);
        linearGradientBrush.EndPoint = new Vector2(1, 0);
        _dim = Track(_compositor!.CreateSpriteVisual());
        _dim.Brush = Track(linearGradientBrush);
        _dim.RelativeSizeAdjustment = Vector2.One;
        _dim.Opacity = 0f;

        _sceneRoot!.Children.InsertAtTop(_dim);
    }

    private void CreateCursorGlow()
    {
        // Centre colour #24FFFFFF, fading to transparent.
        _cursorGlow = Track(_compositor!.CreateSpriteVisual());
        _cursorGlow.Size = new Vector2(CursorGlowSize);
        _cursorGlow.Opacity = 0f;
        _cursorGlow.Brush = CreateRadialBrush(
            BeamColor,
            [(1f, 0x0), (0.8f, 0x40), (0.6f, 0x0)]);

        _sceneRoot!.Children.InsertAtTop(_cursorGlow);
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private Vector2 GetRestingFocus(float width, float height) =>
        new(width * 0.3f, height / 2f);

    private void LayoutScene()
    {
        if (!_initialized || _sceneRoot is null || _centerStar is null)
            return;

        float width = (float)RootGrid.ActualWidth;
        float height = (float)RootGrid.ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        _sceneRoot.Size = new Vector2(width, height);

        // The beam is symmetrical about a point anchored to the right,
        // indented by BeamRightInset, at a constant height.
        float centerX = width - BeamRightInset;
        float centerY = height / 2f;

        _beamCenter = new Vector2(centerX, centerY);
        _beamHalfLengthTarget = GetBeamHalfLength(width);

        // Snap unless the per-frame easing is already running.
        if (!_renderingHooked)
            _beamHalfLength = _beamHalfLengthTarget;

        ApplyBeam();
        _centerStar.Offset = new Vector3(centerX, centerY, 0f);

        if (!_hasPointer)
            _flareFocusTarget = GetRestingFocus(width, height);

        PositionFlare();
    }

    /// <summary>
    /// The beam's edge follows the cursor's X, but only by BeamCursorPull of the
    /// way, and never past the centre point. Both halves shrink equally, so the
    /// beam stays symmetrical about its centre.
    /// </summary>
    private float GetBeamHalfLength(float width)
    {
        float centerX = width - BeamRightInset;
        float full = MathF.Max(centerX * BeamHalfLengthFraction, 80f);

        if (!_hasPointer)
            return full;

        float defaultEdge = centerX - full;
        float pull = Math.Clamp(_cursor.X - defaultEdge, 0f, full);

        return full - pull * BeamCursorPull;
    }

    private void ApplyBeam()
    {
        float length = _beamHalfLength * 2f;

        for (int i = 0; i < _beamLayers.Count; i++)
        {
            float layerHeight = BeamLayers[i].Height;

            _beamLayers[i].Size = new Vector2(length, layerHeight);
            _beamLayers[i].Offset = new Vector3(
                _beamCenter.X - _beamHalfLength,
                _beamCenter.Y - layerHeight / 2f,
                0f);
        }
    }

    private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_initialized)
        {
            InitializeScene();
            return;
        }

        LayoutScene();
    }

    // ------------------------------------------------------------------
    // Solar reflection
    // ------------------------------------------------------------------

    private void PositionFlare()
    {
        for (int i = 0; i < _ghosts.Count; i++)
        {
            FlareGhost ghost = FlareGhosts[i];

            Vector2 position = _beamCenter + ghost.T * (_flareFocus - _beamCenter);

            _ghosts[i].Offset = new Vector3(
                position.X - ghost.Size / 2f,
                position.Y - ghost.Size / 2f,
                0f);
        }
    }

    private void HookRendering()
    {
        if (_renderingHooked)
            return;

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
        _renderingHooked = true;
    }

    private void UnhookRendering()
    {
        if (!_renderingHooked)
            return;

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;
        _renderingHooked = false;
    }

    private void OnRendering(object? sender, object e)
    {
        // Ease the flare focus and the beam length towards their targets;
        // unhook once both have settled.
        Vector2 delta = _flareFocusTarget - _flareFocus;
        float lengthDelta = _beamHalfLengthTarget - _beamHalfLength;

        if (delta.LengthSquared() < 0.04f && MathF.Abs(lengthDelta) < 0.2f)
        {
            _flareFocus = _flareFocusTarget;
            _beamHalfLength = _beamHalfLengthTarget;
            PositionFlare();
            ApplyBeam();
            UnhookRendering();
            return;
        }

        _flareFocus += delta * FlareFollowSpeed;
        _beamHalfLength += lengthDelta * FlareFollowSpeed;

        PositionFlare();
        ApplyBeam();
    }

    // ------------------------------------------------------------------
    // Pointer
    // ------------------------------------------------------------------

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_initialized || _cursorGlow is null || _dim is null || _flare is null)
            return;

        Point p = e.GetCurrentPoint(RootGrid).Position;
        _cursor = new Vector2((float)p.X, (float)p.Y);
        _hasPointer = true;

        // Set directly every move: no animation sits between the pointer and the glow.
        _cursorGlow.Offset = new Vector3(
            _cursor.X - CursorGlowSize / 2f,
            _cursor.Y - CursorGlowSize / 2f,
            0f);

        _cursorGlow.Opacity = 1f;
        _dim.Opacity = CursorDim;
        _flare.Opacity = 1f;

        _flareFocusTarget = _cursor;
        _beamHalfLengthTarget = GetBeamHalfLength((float)RootGrid.ActualWidth);
        HookRendering();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_initialized || _cursorGlow is null || _dim is null || _flare is null)
            return;

        _hasPointer = false;

        _cursorGlow.Opacity = 0f;
        _dim.Opacity = 0f;
        _flare.Opacity = FlareIdleOpacity;

        _flareFocusTarget = GetRestingFocus(
            (float)RootGrid.ActualWidth,
            (float)RootGrid.ActualHeight);

        _beamHalfLengthTarget = GetBeamHalfLength((float)RootGrid.ActualWidth);
        HookRendering();
    }
}