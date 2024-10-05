using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Graphics.Display;
using WinUIEx;
using Microsoft.UI.Windowing;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Graphics;
using WindowMessageMonitor = WinUIEx.Messaging.WindowMessageMonitor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReboundTpm;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{
    public double Scale()
    {
        // Get the DisplayInformation object for the current view
        DisplayInformation displayInformation = DisplayInformation.CreateForWindowId(this.AppWindow.Id);
        // Get the RawPixelsPerViewPixel which gives the scale factor
        var scaleFactor = displayInformation.RawPixelsPerViewPixel;
        return scaleFactor;
    }

    public MainWindow()
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
        WindowTitle.Text = Title;
        RootFrame.Navigate(typeof(MainPage));
        this.SetWindowIcon($"Assets\\icon.ico");

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
        App.m_window = null;
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
                    if (App.m_window != null) (App.m_window as MainWindow).CheckFocus();
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
                    {
                        currentCaption = SelectedCaptionButton.Minimize;
                        VisualStateManager.GoToState(Minimize, "Pressed", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                    }

                    // Maximize Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - maxButtonLeftPos) &&
                             ((x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale() - closeButtonLeftPos)) &&
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
                    {
                        currentCaption = SelectedCaptionButton.Maximize;
                        VisualStateManager.GoToState(Minimize, "Normal", true);
                        VisualStateManager.GoToState(CrimsonMaxRes, "Pressed", true);
                        VisualStateManager.GoToState(Close, "Normal", true);
                    }

                    // Close Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - closeButtonLeftPos) &&
                             (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale()) + 2 &&
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
                    {
                        if (currentCaption == SelectedCaptionButton.Maximize) RunMaximization();
                    }

                    // Close Button
                    else if ((x - 7 * Scale() - this.AppWindow.Position.X) >= (this.Bounds.Width * Scale() - closeButtonLeftPos) &&
                             (x - 7 * Scale() - this.AppWindow.Position.X) < (this.Bounds.Width * Scale()) + 2 &&
                        y < this.AppWindow.Position.Y + 31 * Scale() + additionalHeight * Scale() &&
                        y >= this.AppWindow.Position.Y + 1 * Scale())
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
            CaptionButtons.Margin = new Thickness(0, 0, 2, 0);
            return;
        }
        else if (state == OverlappedPresenterState.Maximized)
        {
            this.Restore();
            RootFrame.Focus(FocusState.Programmatic);
            CheckMaximization();
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
                closeWidth = 46;
                additionalHeight = 0;
                return;
            }
            else if (state == OverlappedPresenterState.Maximized)
            {
                MaxResGlyph.Glyph = "";
                await Task.Delay(10);
                VisualStateManager.GoToState(CrimsonMaxRes, "Normal", true);
                closeWidth = 46;
                additionalHeight = 6;
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
        try
        {
            if (this.Content != null)
            {
                if (e.GetCurrentPoint(this.Content).Properties.IsLeftButtonPressed != true)
                {
                    currentCaption = SelectedCaptionButton.None;
                }
            }
        }
        catch
        {

        }
    }
}
