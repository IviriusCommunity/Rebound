using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using WinUIEx.Messaging;

namespace Riverside.Toolkit.Controls;

public partial class TitleBarEx : Control
{
    /// <summary>
    /// The current window associated with this title bar. Do not set this property directly; use <see cref="InitializeForWindow(WindowEx)"/> instead.
    /// </summary>
    protected WindowEx? CurrentWindow { get; private set; }

    // UI controls
    protected Button? CloseButton { get; private set; }
    protected Button? MaximizeRestoreButton { get; private set; }
    protected Button? MinimizeButton { get; private set; }
    protected TextBlock? TitleTextBlock { get; private set; }
    protected Image? TitleBarIcon { get; private set; }
    protected Border? AccentStrip { get; private set; }
    protected MenuFlyout? CustomRightClickFlyout { get; private set; }

    /// <summary>
    /// The currently selected caption button.
    /// </summary>
    protected SelectedCaptionButton CurrentCaption { get; private set; } = SelectedCaptionButton.None;

    // Local variables
    private WindowMessageMonitor? _messageMonitor;
    private bool _isWindowFocused = false;
    private bool _isMaximized = false;
    private int _buttonDownHeight = 0;
    private double _additionalHeight = 0;
    private bool _closed = false;
    private bool _allowSizeCheck = false;
    private bool _loaded = false;

    /// <summary>
    /// XAML title bar control for WinUI 3 apps.
    /// </summary>
    public TitleBarEx()
    {
        this.DefaultStyleKey = typeof(TitleBarEx);
        this.Loaded += TitleBarEx_Loaded;
    }

    private void TitleBarEx_Loaded(object sender, RoutedEventArgs e) => _loaded = true;

    /// <summary>
    /// Loader method for the control template.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Using GetTemplateChild<T> to safely retrieve template children and cast them
        this.CloseButton = GetTemplateChild<Button>("CloseButton");
        this.MaximizeRestoreButton = GetTemplateChild<Button>("MaximizeButton");
        this.MinimizeButton = GetTemplateChild<Button>("MinimizeButton");
        this.TitleTextBlock = GetTemplateChild<TextBlock>("TitleTextBlock");
        this.TitleBarIcon = GetTemplateChild<Image>("TitleBarIcon");
        this.AccentStrip = GetTemplateChild<Border>("AccentStrip");
        this.CustomRightClickFlyout = GetTemplateChild<MenuFlyout>("CustomRightClickFlyout");

        // Using GetTemplateChild<T> to safely retrieve template children and cast them, then subscribe to events
        GetTemplateChild<MenuFlyoutItem>("MaximizeContextMenuItem").Click += MaximizeContextMenu_Click;
        GetTemplateChild<MenuFlyoutItem>("SizeContextMenuItem").Click += SizeContextMenu_Click;
        GetTemplateChild<MenuFlyoutItem>("MoveContextMenuItem").Click += MoveContextMenu_Click;
        GetTemplateChild<MenuFlyoutItem>("MinimizeContextMenuItem").Click += MinimizeContextMenu_Click;
        GetTemplateChild<MenuFlyoutItem>("CloseContextMenuItem").Click += CloseContextMenu_Click;
        GetTemplateChild<MenuFlyoutItem>("RestoreContextMenuItem").Click += RestoreContextMenu_Click;
    }

    /// <summary>
    /// Initialize the TitleBarEx control for the current window.
    /// </summary>
    /// <param name="windowEx"></param>
    public void InitializeForWindow(WindowEx windowEx)
    {
        this.CurrentWindow = windowEx;

        // Configure title bar
        this.CurrentWindow.ExtendsContentIntoTitleBar = true;
        this.CurrentWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

        // Attach pointer events
        var content = (FrameworkElement)this.CurrentWindow.Content;
        content.PointerMoved += CheckMouseButtonDownPointerEvent;
        content.PointerReleased += CheckMouseButtonDownPointerEvent;
        content.PointerExited += CheckMouseButtonDownPointerEvent;
        content.PointerEntered += SwitchButtonStatePointerEvent;
        PointerExited += SwitchButtonStatePointerEvent;

        // Attach window events
        this.CurrentWindow.WindowStateChanged += CurrentWindow_WindowStateChanged;
        this.CurrentWindow.Closed += CurrentWindow_Closed;
        this.CurrentWindow.SizeChanged += CurrentWindow_SizeChanged;
        this.CurrentWindow.PositionChanged += CurrentWindow_PositionChanged;

        // Attach load events
        content.Loaded += ContentLoaded;

        // Initialize window properties and behaviors
        UpdateWindowSizeAndPosition();
        UpdateWindowProperties();
        AttachWndProc();
        LoadDragRegion();
        InvokeChecks();
    }
}
