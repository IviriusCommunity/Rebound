using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUIEx;
using ReboundHub.ReboundHub.Pages.ControlPanel;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI;
using System.Runtime.InteropServices;
using WindowMessageMonitor = WinUIEx.Messaging.WindowMessageMonitor;
using Microsoft.Win32;
using System.Threading;
using CommunityToolkit.WinUI.UI.Helpers;
using Microsoft.Graphics.Display;
using Color = Windows.UI.Color;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.System;

#pragma warning disable IDE0044 // Add readonly modifier

namespace ReboundHub;

public sealed partial class ControlPanelWindow : WindowEx
{
    public double Scale()
    {
        // Get the DisplayInformation object for the current view
        DisplayInformation displayInformation = DisplayInformation.CreateForWindowId(this.AppWindow.Id);
        // Get the RawPixelsPerViewPixel which gives the scale factor
        var scaleFactor = displayInformation.RawPixelsPerViewPixel;
        return scaleFactor;
    }

    public ControlPanelWindow()
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        this.Title = "Rebound Control Panel";
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(ModernHomePage));
        this.SetIcon($"{AppContext.BaseDirectory}\\Assets\\AppIcons\\rcontrol.ico");

        _msgMonitor ??= new WindowMessageMonitor(this);
        _msgMonitor.WindowMessageReceived -= Event;
        _msgMonitor.WindowMessageReceived += Event;

        this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

        mon = new RegistryMonitor(@"Software\Microsoft\Windows\DWM");
        mon.Start();
        var x = new ThemeListener();
        x.ThemeChanged += X_ThemeChanged;

        Rehook();
        CheckWindow();
        Read();

        AddressBox.Text = "Control Panel";
        AddressBox.ItemsSource = new List<string>()
        {
            @"Control Panel",
            @"Control Panel\Appearance and Personalization",
            @"Control Panel\System and Security",
        };
        NavigateToPath();
        RootFrame.BackStack.Clear();
        RootFrame.ForwardStack.Clear();
    }

    private async void Read()
    {
        await Task.Delay(50);
        try
        {
            BackButton.IsEnabled = RootFrame.CanGoBack;
            ForwardButton.IsEnabled = RootFrame.CanGoForward;

            Read();
        }
        catch
        {
        
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.GoBack();
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.GoForward();
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        App.cpanelWin.Close();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var oldHistory = RootFrame.ForwardStack;
        var newList = new List<PageStackEntry>();
        foreach (var item in oldHistory)
        {
            newList.Add(item);
        }
        RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        RootFrame.GoBack();
        RootFrame.ForwardStack.Clear();
        foreach (var item in newList)
        {
            RootFrame.ForwardStack.Add(item);
        }
        AddressBox.Text = CurrentPage();
        NavigateToPath();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel";
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel\Appearance and Personalization";
        NavigateToPath();
    }

    private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        AddressBox.Text = @"Control Panel\System and Security";
        NavigateToPath();
    }

    public async void Rehook()
    {
        _msgMonitor ??= new WindowMessageMonitor(this);
        _msgMonitor.WindowMessageReceived -= Event;
        _msgMonitor.WindowMessageReceived += Event;

        await Task.Delay(1000);

        Rehook();
    }

    public void SetWindowIcon(string path)
    {
        TitleBarIcon.Source = new BitmapImage(new Uri($"ms-appx:///{path}"));
        this.SetIcon($"{AppContext.BaseDirectory}\\{path}");
    }

    public async void CheckWindow()
    {
        try
        {
            if (WindowTitle != null && this != null) WindowTitle.Text = Title;

            await Task.Delay(50);

            CheckWindow();
        }
        catch (AccessViolationException)
        {

        }
        catch (COMException)
        {

        }
    }

    RegistryMonitor mon;

    private async void X_ThemeChanged(ThemeListener sender)
    {
        await Task.Delay(200);
        CheckFocus();
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        App.cpanelWin = null;
        mon.Stop();
        _msgMonitor.Dispose();
    }

    #region MaximizeButton

    WindowMessageMonitor _msgMonitor;

    double additionalHeight = 0;

    public bool isInMaxButton = false;

    private void CrimsonMaxRes_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        ev = e;
        isInMaxButton = true;
    }
    private void CrimsonMaxRes_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        ev = e;
        isInMaxButton = false;
    }

    private void CrimsonMaxRes_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ev = e;
        isInMaxButton = false;
    }

    PointerRoutedEventArgs ev;

    private void CrimsonMaxRes_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ev = e;
        _msgMonitor ??= new WindowMessageMonitor(this);
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _msgMonitor.WindowMessageReceived -= Event;
        _msgMonitor.WindowMessageReceived += Event;
    }

    private int GET_X_LPARAM(IntPtr lParam)
    {
        return unchecked((short)(long)lParam);
    }

    private int GET_Y_LPARAM(IntPtr lParam)
    {
        return unchecked((short)((long)lParam >> 16));
    }

    bool windowFocused = false;

    SolidColorBrush buttonBrush;

    private const string RegistryKeyPath = @"Software\Microsoft\Windows\DWM";
    private const string RegistryValueName = "ColorPrevalence";

    private static readonly IntPtr HKEY_CURRENT_USER = unchecked((IntPtr)0x80000001);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern IntPtr RegOpenKeyEx(IntPtr hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr hKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, uint dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

    private const uint REG_NOTIFY_CHANGE_NAME = 0x00000001;
    private const uint REG_NOTIFY_CHANGE_ATTRIBUTES = 0x00000002;
    private const uint REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;
    private const uint REG_NOTIFY_CHANGE_SECURITY = 0x00000008;

    private static ManualResetEvent _changeEvent = new(false);
    private static bool _running = true;

    private class RegistryMonitor : IDisposable
    {
        private readonly string _registryKeyPath;
        private IntPtr _hKey;
        private readonly Thread _monitorThread;

        public RegistryMonitor(string registryKeyPath)
        {
            _registryKeyPath = registryKeyPath;
            _monitorThread = new Thread(MonitorRegistry) { IsBackground = true };
        }

        public void Start()
        {
            _monitorThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _changeEvent.Set();
            _monitorThread.Join();
        }

        private void MonitorRegistry()
        {
            if (RegOpenKeyEx(HKEY_CURRENT_USER, _registryKeyPath, 0, 0x20019, out _hKey) != 0)
            {
                throw new InvalidOperationException("Failed to open registry key.");
            }

            while (_running)
            {
                // Wait for registry change notification
                if (RegNotifyChangeKeyValue(_hKey, true, REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_CHANGE_ATTRIBUTES | REG_NOTIFY_CHANGE_LAST_SET | REG_NOTIFY_CHANGE_SECURITY, _changeEvent.SafeWaitHandle.DangerousGetHandle(), true) == 0)
                {
                    // Handle registry change
                    if (App.cpanelWin != null) App.cpanelWin.CheckFocus();
                    _changeEvent.WaitOne();
                }
            }

            RegCloseKey(_hKey);
        }

        public void Dispose()
        {
            Stop();
            _changeEvent.Dispose();
        }
    }

    public static bool IsAccentColorEnabledForTitleBars()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            if (key != null)
            {
                var value = key.GetValue(RegistryValueName);
                if (value is int intValue)
                {
                    // ColorPrevalence value of 1 means the accent color is used for title bars and window borders.
                    return intValue == 1;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., security exceptions or key not found)
            Console.WriteLine($"Error checking accent color: {ex.Message}");
        }

        return false; // Default to false if any issues occur
    }

    private void CheckFocus()
    {
        if (IsAccentColorEnabledForTitleBars() == true)
        {
            try
            {
                if (AccentStrip != null) AccentStrip.Visibility = Visibility.Visible;
                if (!windowFocused)
                {
                    buttonBrush = new SolidColorBrush(Colors.White);
                    AccentStrip.Visibility = Visibility.Visible;
                }
                else
                {
                    buttonBrush = Application.Current.Resources["TextFillColorDisabledBrush"] as SolidColorBrush;
                    AccentStrip.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }
        else
        {
            try
            {
                if (AccentStrip != null) AccentStrip.Visibility = Visibility.Collapsed;
                if (!windowFocused)
                {
                    buttonBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                }
                else
                {
                    buttonBrush = Application.Current.Resources["TextFillColorDisabledBrush"] as SolidColorBrush;
                }
            }
            catch { }
        }
        UpdateBrush();
    }

    private void UpdateBrush()
    {
        try
        {
            Close.Foreground = buttonBrush;
            CrimsonMaxRes.Foreground = buttonBrush;
            Minimize.Foreground = buttonBrush;
            WindowTitle.Foreground = buttonBrush;
        }
        catch
        {
        
        }
    }

    public enum SelectedCaptionButton
    {
        None = 0,
        Minimize = 1,
        Maximize = 2,
        Close = 3
    }

    public SelectedCaptionButton currentCaption = SelectedCaptionButton.None;

    async void Event(object sender, WinUIEx.Messaging.WindowMessageEventArgs e)
    {
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int WM_NCHITTEST = 0x0084;
        const int WM_NCLBUTTONUP = 0x00A2;
        const int WM_NCMOUSELEAVE = 0x02A2;
        const int WM_ACTIVATE = 0x0006;
        const int WA_INACTIVE = 0;
        var x = GET_X_LPARAM(e.Message.LParam);
        var y = GET_Y_LPARAM(e.Message.LParam);

        var minButtonLeftPos = (Minimize.Width + CrimsonMaxRes.Width + closeWidth) * Scale();
        var maxButtonLeftPos = (CrimsonMaxRes.Width + closeWidth) * Scale();
        var closeButtonLeftPos = closeWidth * Scale();

        switch (e.Message.MessageId)
        {
            case WM_ACTIVATE:
                {
                    var wParam = e.Message.WParam.ToUInt32();
                    if (wParam == WA_INACTIVE)
                    {
                        // The window has lost focus
                        windowFocused = true;
                        CheckFocus();
                    }
                    else
                    {
                        // The window has gained focus
                        windowFocused = false;
                        CheckFocus();
                    }
                    break;
                }
            case WM_NCHITTEST:
                {
                    // Minimize Button
                    if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - minButtonLeftPos) && 
                        (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        e.Handled = true;
                        e.Result = new IntPtr(8);
                        if (currentCaption == SelectedCaptionButton.Minimize) VisualStateManager.GoToState(Minimize, "Pressed", true);
                        else if (currentCaption == SelectedCaptionButton.None) VisualStateManager.GoToState(Minimize, "PointerOver", true);
                        else VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                        await Task.Delay(1000);
                        e.Handled = false;
                    }

                    // Maximize Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - maxButtonLeftPos) && 
                             ((x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - closeButtonLeftPos)) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        e.Handled = true;
                        e.Result = new IntPtr(9);
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        if (currentCaption == SelectedCaptionButton.Maximize) VisualStateManager.GoToState(CrimsonMaxRes, "Pressed", true);
                        else if (currentCaption == SelectedCaptionButton.None) VisualStateManager.GoToState(CrimsonMaxRes, "PointerOver", true);
                        else VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                        await Task.Delay(1000);
                        e.Handled = false;
                    }

                    // Close Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - closeButtonLeftPos) &&
                             (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale()) + 2 &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        e.Handled = true;
                        e.Result = new IntPtr(20);
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        if (currentCaption == SelectedCaptionButton.Close) VisualStateManager.GoToState(Close, "Pressed", true);
                        else if (currentCaption == SelectedCaptionButton.None) VisualStateManager.GoToState(Close, "PointerOver", true);
                        else VisualStateManager.GoToState(Close, "Normal", true);
                        await Task.Delay(1000);
                        e.Handled = false;
                    }

                    // Title bar drag area
                    else
                    {
                        e.Handled = true;
                        e.Result = new IntPtr(1);
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                        e.Handled = false;
                    }

                    break;
                }
            case WM_NCLBUTTONDOWN:
                {
                    e.Handled = true;
                    e.Result = new IntPtr(1);
                    e.Handled = false;

                    // Minimize Button
                    if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - minButtonLeftPos) &&
                        (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        currentCaption = SelectedCaptionButton.Minimize;
                        VisualStateManager.GoToState(Minimize, "Pressed", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                    }

                    // Maximize Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                             ((x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - closeButtonLeftPos)) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        currentCaption = SelectedCaptionButton.Maximize;
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Pressed", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                    }

                    // Close Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - closeButtonLeftPos) &&
                             (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale()) + 2 &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        currentCaption = SelectedCaptionButton.Close;
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Pressed", true);
                    }

                    // Title bar drag area
                    else
                    {
                        currentCaption = SelectedCaptionButton.None;
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                    }

                    break;
                }
            case WM_NCLBUTTONUP:
                {
                    e.Handled = true;
                    e.Result = new IntPtr(1);
                    e.Handled = false;

                    // Minimize Button
                    if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - minButtonLeftPos) &&
                        (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        if (currentCaption == SelectedCaptionButton.Minimize)
                        {
                            this.Minimize();
                            VisualStateManager.GoToState(Minimize, "Normal", true);
                            VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                            VisualStateManager.GoToState(Close, "Normal", true);
                        }
                    }

                    // Maximize Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                             ((x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - closeButtonLeftPos)) &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        if (currentCaption == SelectedCaptionButton.Maximize) RunMaximization();
                    }

                    // Close Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - closeButtonLeftPos) &&
                             (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale()) + 2 &&
                        y * Scale() < this.AppWindow.Position.Y + 31 + additionalHeight &&
                        y * Scale() >= this.AppWindow.Position.Y + 1)
                    {
                        if (currentCaption == SelectedCaptionButton.Close)
                        {
                            this.Close();
                        }
                    }

                    // Title bar drag area
                    else
                    {

                    }

                    currentCaption = SelectedCaptionButton.None;

                    _msgMonitor.WindowMessageReceived -= Event;
                    _msgMonitor.Dispose();
                    break;
                }
            case WM_NCMOUSELEAVE:
                {
                    e.Handled = true;
                    e.Result = new IntPtr(1);
                    e.Handled = false;
                    VisualStateManager.GoToState(Minimize, "Normal", true);
                    VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                    VisualStateManager.GoToState(Close, "Normal", true);
                    break;
                }
            default:
                {
                    e.Handled = false;
                    break;
                }
        }
    }

    #endregion MaximizeButton

    double closeWidth = 46;

    public void RunMaximization()
    {
        var state = (this.Presenter as OverlappedPresenter).State;
        if (state == OverlappedPresenterState.Restored)
        {
            this.Maximize();
            RootFrame.Focus(FocusState.Programmatic);
            CheckMaximization();
            closeWidth = 48;
            additionalHeight = 6;
            CaptionButtons.Margin = new Thickness(0, 0, 2, 0);
            return;
        }
        else if (state == OverlappedPresenterState.Maximized)
        {
            this.Restore();
            RootFrame.Focus(FocusState.Programmatic);
            CheckMaximization();
            closeWidth = 46;
            additionalHeight = 0;
            CaptionButtons.Margin = new Thickness(0);
            return;
        }
    }

    public async void CheckMaximization()
    {
        if (Presenter is OverlappedPresenter)
        {
            var state = (Presenter as OverlappedPresenter).State;
            if (state == OverlappedPresenterState.Restored)
            {
                MaxResGlyph.Glyph = "";
                await Task.Delay(10);
                VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                return;
            }
            else if (state == OverlappedPresenterState.Maximized)
            {
                MaxResGlyph.Glyph = "";
                await Task.Delay(10);
                VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                return;
            }
        }
        else
        {
            MaxResGlyph.Glyph = "";
            await Task.Delay(10);
            VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
            return;
        }
    }

    public void LoadBounds()
    {
        var appWindow = this.AppWindow;
        var titleBar = appWindow.TitleBar;

        RectInt32 rect = new RectInt32(0, 0, (int)(this.Bounds.Width * Scale()), (int)(31 * Scale()));

        RectInt32[] rects =
        [
            rect,
        ];

        titleBar.SetDragRectangles(rects);
    }

    private void WindowEx_SizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs args)
    {
        LoadBounds();
        CheckMaximization();
    }

    private void WindowEx_PositionChanged(object sender, PointInt32 e)
    {
        LoadBounds();
        CheckMaximization();
    }

    private void CrimsonMaxRes_Click(object sender, RoutedEventArgs e)
    {
        RunMaximization();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(this.Content).Properties.IsLeftButtonPressed != true)
        {
            currentCaption = SelectedCaptionButton.None;
        }
    }

    private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(this.Content).Properties.IsLeftButtonPressed != true)
        {
            currentCaption = SelectedCaptionButton.None;
        }
    }

    private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (this.Content != null)
        {
            try
            {
                if (e.GetCurrentPoint(this.Content).Properties.IsLeftButtonPressed != true)
                {
                    currentCaption = SelectedCaptionButton.None;
                }
            }
            catch
            {
            
            }
        }
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.Visibility = Visibility.Visible;
        AddressBox.Visibility = Visibility.Collapsed;
    }

    private async void AddressBar_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        AddressBar.Visibility = Visibility.Collapsed;
        AddressBox.Visibility = Visibility.Visible;
        await Task.Delay(10);
        AddressBox.Focus(FocusState.Programmatic);
    }

    private void AddressBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {

    }

    public void HideAll()
    {
        AppearanceAndPersonalization.Visibility = Visibility.Collapsed;
        SystemAndSecurity.Visibility = Visibility.Collapsed;
    }

    public string CurrentPage()
    {
        switch (RootFrame.Content)
        {
            case ReboundHub.Pages.ControlPanel.AppearanceAndPersonalization:
                {
                    return @"Control Panel\Appearance and Personalization";
                }
            case ReboundHub.Pages.ControlPanel.SystemAndSecurity:
                {
                    return @"Control Panel\System and Security";
                }
            case ModernHomePage:
                {
                    return @"Control Panel";
                }
            case HomePage:
                {
                    return @"Control Panel";
                }
            default:
                {
                    return @"Control Panel";
                }
        }
    }

    private void AddressBox_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            NavigateToPath();
        }
    }

    public void NavigateToPath(bool legacyHomePage = false)
    {
        HideAll();
        RootFrame.Focus(FocusState.Programmatic);
        switch (AddressBox.Text)
        {
            case @"Control Panel\Appearance and Personalization":
                {
                    if (RootFrame.Content is not ReboundHub.Pages.ControlPanel.AppearanceAndPersonalization) App.cpanelWin.RootFrame.Navigate(typeof(AppearanceAndPersonalization), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    AppearanceAndPersonalization.Visibility = Visibility.Visible;
                    return;
                }
            case @"Control Panel\System and Security":
                {
                    if (RootFrame.Content is not ReboundHub.Pages.ControlPanel.SystemAndSecurity) App.cpanelWin.RootFrame.Navigate(typeof(SystemAndSecurity), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    SystemAndSecurity.Visibility = Visibility.Visible;
                    return;
                }
            case @"Control Panel":
                {
                    if (RootFrame.Content is not ModernHomePage or HomePage)
                    {
                        if (legacyHomePage == false) App.cpanelWin.RootFrame.Navigate(typeof(ModernHomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                        else App.cpanelWin.RootFrame.Navigate(typeof(HomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                    return;
                }
            default:
                {
                    AddressBox.Text = CurrentPage();
                    return;
                }
        }
    }

    private async void AddressBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        await Task.Delay(10);
        NavigateToPath();
    }
}
