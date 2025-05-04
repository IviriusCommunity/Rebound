using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.System;
using WinRT.Interop;


namespace Rebound.Keyboard;

public sealed partial class MainWindow : Window
{
    private bool isShiftPressed = false;
    private bool isCapsLockOn = false;
    private IntPtr hwnd;
    private bool isSpecialCharactersVisible = false;
    private bool isDocked = false;
    private double opacity = 1.0;
    private double fontSize = 16.0;
    private AppWindow appWindow;
    private MicaController micaController;
    private SystemBackdropConfiguration backdropConfiguration;

    // Save settings in Documents folder
    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ReboundScreenKeyboard",
        "settings.json");

    // Settings class
    private class KeyboardSettings
    {
        public double Opacity { get; set; } = 1.0;
        public double FontSize { get; set; } = 16.0;
        public bool IsDocked { get; set; } = false;
    }

    // Import Windows API functions
    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Rectangle structure for window positioning
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // Constants
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOACTIVATE = 0x0010;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_SYSMENU = 0x00080000;

    // Virtual key code mapping
    private static readonly Dictionary<char, byte> CharToVirtualKey = new()
{
    {'a', 0x41}, {'b', 0x42}, {'c', 0x43}, {'d', 0x44}, {'e', 0x45},
    {'f', 0x46}, {'g', 0x47}, {'h', 0x48}, {'i', 0x49}, {'j', 0x4A},
    {'k', 0x4B}, {'l', 0x4C}, {'m', 0x4D}, {'n', 0x4E}, {'o', 0x4F},
    {'p', 0x50}, {'q', 0x51}, {'r', 0x52}, {'s', 0x53}, {'t', 0x54},
    {'u', 0x55}, {'v', 0x56}, {'w', 0x57}, {'x', 0x58}, {'y', 0x59},
    {'z', 0x5A}, {'0', 0x30}, {'1', 0x31}, {'2', 0x32}, {'3', 0x33},
    {'4', 0x34}, {'5', 0x35}, {'6', 0x36}, {'7', 0x37}, {'8', 0x38},
    {'9', 0x39}, {'.', 0xBE}, {',', 0xBC}, {'/', 0xBF}, {';', 0xBA},
    {'\'', 0xDE}, {'[', 0xDB}, {']', 0xDD}, {'\\', 0xDC}, {'-', 0xBD},
    {'=', 0xBB}, {'`', 0xC0}
};

    // Special characters mapping
    private static readonly Dictionary<string, string> SpecialCharacters = new()
{
    {"!", "1"}, {"@", "2"}, {"#", "3"}, {"$", "4"}, {"%", "5"},
    {"^", "6"}, {"&", "7"}, {"*", "8"}, {"(", "9"}, {")", "0"},
    {"_", "-"}, {"+", "="}, {"~", "`"}, {"{", "["}, {"}", "]"},
    {"|", "\\"}, {":", ";"}, {"\"", "'"}, {"<", ","}, {">", "."},
    {"?", "/"}
};

    public MainWindow()
    {
        this.InitializeComponent();

        // Set title
        Title = "Rebound Screen Keyboard";

        // Get the window handle
        hwnd = WindowNative.GetWindowHandle(this);

        // Get AppWindow
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        appWindow = AppWindow.GetFromWindowId(windowId);

        // Load settings BEFORE initializing UI components
        LoadSettings();

        // Set up Mica backdrop
        SetupSystemBackdrop();

        // Set default window size
        SetDefaultWindowSize();

        // Make the window stay on top and not take focus
        MakeWindowTopMostAndNoActivate();

        // Initialize keyboard layout
        InitializeKeyboard();

        // Set up the title bar
        SetupTitleBar();

        // Update UI with loaded settings
        UpdateUIFromSettings();

        // Subscribe to window events
        this.Activated += MainWindow_Activated;
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // Update font size when window is activated
        UpdateFontSize(fontSize);
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // Save settings when window is closed
        SaveSettings();
    }

    private void LoadSettings()
    {
        try
        {
            // Create directory if it doesn't exist
            string settingsDir = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<KeyboardSettings>(json);

                if (settings != null)
                {
                    // Load opacity setting
                    opacity = settings.Opacity;

                    // Load font size setting
                    fontSize = settings.FontSize;

                    // Load dock position setting
                    isDocked = settings.IsDocked;

                    // Log successful loading
                    System.Diagnostics.Debug.WriteLine($"Settings loaded from: {settingsPath}");
                    System.Diagnostics.Debug.WriteLine($"Opacity: {opacity}, FontSize: {fontSize}, IsDocked: {isDocked}");
                }
            }
            else
            {
                // Create default settings file if it doesn't exist
                SaveSettings();
                System.Diagnostics.Debug.WriteLine($"Default settings created at: {settingsPath}");
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");

            // Use default values
            opacity = 1.0;
            fontSize = 16.0;
            isDocked = false;
        }
    }

    private void SaveSettings()
    {
        try
        {
            // Create directory if it doesn't exist
            string settingsDir = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            var settings = new KeyboardSettings
            {
                Opacity = opacity,
                FontSize = fontSize,
                IsDocked = isDocked
            };

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(settingsPath, json);
            System.Diagnostics.Debug.WriteLine($"Settings saved to: {settingsPath}");
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");

            // Update status text if available
            if (StatusText != null)
            {
                StatusText.Text = $"Error saving settings: {ex.Message}";
            }
        }
    }

    private void UpdateUIFromSettings()
    {
        try
        {
            // Update opacity slider
            if (OpacitySlider != null)
            {
                OpacitySlider.Value = opacity * 100;
            }

            // Update font size slider
            if (FontSizeSlider != null)
            {
                FontSizeSlider.Value = fontSize;
            }

            // Update window opacity
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.Opacity = opacity;
            }

            // Update font size
            UpdateFontSize(fontSize);

            // Update dock position if needed
            if (isDocked)
            {
                DockToBottom();
            }

            // Log successful UI update
            System.Diagnostics.Debug.WriteLine("UI updated from settings");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating UI from settings: {ex.Message}");
        }
    }

    private void SetupSystemBackdrop()
    {
        // Try to use Mica Alt backdrop
        if (MicaController.IsSupported())
        {
            backdropConfiguration = new SystemBackdropConfiguration();
            micaController = new MicaController();

            // Always use BaseAlt for consistent dark appearance
            micaController.Kind = MicaKind.Base;

            // Set activation states
            backdropConfiguration.IsInputActive = true;
            backdropConfiguration.Theme = SystemBackdropTheme.Dark;

            micaController.SetSystemBackdropConfiguration(backdropConfiguration);
        }
    }

    private void SetDefaultWindowSize()
    {
        // Get screen dimensions
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        // Set window size to 80% of screen width and 30% of screen height
        int windowWidth = (int)(screenWidth * 0.8);
        int windowHeight = (int)(screenHeight * 0.45);

        // Position window at the bottom center of the screen
        int x = (screenWidth - windowWidth) / 2;
        int y = screenHeight - windowHeight - 50; // 50 pixels from bottom

        // Set window size and position
        appWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
        appWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private void SetupTitleBar()
    {
        // Get the title bar
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Set the background color of the title bar buttons
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Set the foreground color of the title bar buttons
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;

            // Set the hover colors
            titleBar.ButtonHoverBackgroundColor = Colors.SlateGray;
            titleBar.ButtonHoverForegroundColor = Colors.White;

            // Set the pressed colors
            titleBar.ButtonPressedBackgroundColor = Colors.DarkSlateGray;
            titleBar.ButtonPressedForegroundColor = Colors.White;
        }

        // We need to wait until the AppTitleBar is loaded to get its actual size
        AppTitleBar.Loaded += (sender, e) =>
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                // Get the position and size of AppTitleBar
                var transform = AppTitleBar.TransformToVisual(null);
                var bounds = transform.TransformBounds(new Rect(0, 0, AppTitleBar.ActualWidth, AppTitleBar.ActualHeight));

                // Set the drag region
                appWindow.TitleBar.SetDragRectangles(new Windows.Graphics.RectInt32[]
                {
                new Windows.Graphics.RectInt32(
                    (int)bounds.X,
                    (int)bounds.Y,
                    (int)bounds.Width,
                    (int)bounds.Height)
                });
            }
        };

        // Hide system buttons and prevent maximization using Win32 API
        HideSystemButtonsAndPreventMaximization();
    }

    private void HideSystemButtonsAndPreventMaximization()
    {
        // Get current window style
        int style = GetWindowLong(hwnd, GWL_STYLE);

        // Remove maximize box and minimize box
        style &= ~WS_MAXIMIZEBOX;  // Remove maximize button
        style &= ~WS_MINIMIZEBOX;  // Remove minimize button

        // Optionally remove system menu (includes close button)
        // style &= ~WS_SYSMENU;

        // Apply the modified style
        SetWindowLong(hwnd, GWL_STYLE, style);

        // If using OverlappedPresenter, set IsMaximizable to false if available
        try
        {
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }
        catch
        {
            // Ignore if these properties are not available
        }
    }

    private void MakeWindowTopMostAndNoActivate()
    {
        // Set the window to be topmost
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        // Set the window style to not activate when clicked
        IntPtr exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        exStyle = new IntPtr(exStyle.ToInt64() | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);
    }

    private void InitializeKeyboard()
    {
        // The keyboard layout will be initialized in XAML

        // Hide special characters panel initially
        SpecialCharactersPanel.Visibility = Visibility.Collapsed;
    }

    private void KeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Store the foreground window before we potentially change it
            IntPtr foregroundWindow = GetForegroundWindow();

            string keyValue = button.Tag?.ToString() ?? button.Content?.ToString() ?? "";

            // Handle special keys
            switch (keyValue)
            {
                case "Shift":
                    isShiftPressed = !isShiftPressed;
                    UpdateKeyboardCase();
                    return;
                case "Caps":
                    isCapsLockOn = !isCapsLockOn;
                    UpdateKeyboardCase();
                    return;
                case "Space":
                    SendKeyInput(VirtualKey.Space, foregroundWindow);
                    return;
                case "Enter":
                    SendKeyInput(VirtualKey.Enter, foregroundWindow);
                    return;
                case "Tab":
                    SendKeyInput(VirtualKey.Tab, foregroundWindow);
                    return;
                case "Backspace":
                    SendKeyInput(VirtualKey.Back, foregroundWindow);
                    return;
                case "Special":
                    ToggleSpecialCharacters();
                    return;
                case "Dock":
                    ToggleDockPosition();
                    return;
                case "Hide":
                    MinimizeKeyboard();
                    return;
                default:
                    // For regular keys, send the key input
                    if (keyValue.Length == 1)
                    {
                        char keyChar = keyValue[0];
                        if (isShiftPressed || isCapsLockOn)
                        {
                            keyChar = char.ToUpper(keyChar);
                        }
                        else
                        {
                            keyChar = char.ToLower(keyChar);
                        }

                        // Send the key input
                        SendCharInput(keyChar, foregroundWindow);

                        // If shift was pressed, toggle it off after a key press
                        if (isShiftPressed)
                        {
                            isShiftPressed = false;
                            UpdateKeyboardCase();
                        }
                    }
                    break;
            }

            // Restore focus to the previous foreground window
            SetForegroundWindow(foregroundWindow);
        }
    }

    private void SpecialCharButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Store the foreground window before we potentially change it
            IntPtr foregroundWindow = GetForegroundWindow();

            string keyValue = button.Content?.ToString() ?? "";

            if (keyValue.Length == 1)
            {
                // For special characters, we need to simulate the key combination
                if (SpecialCharacters.TryGetValue(keyValue, out string baseKey))
                {
                    // Press Shift down
                    keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                    // Press the base key
                    if (CharToVirtualKey.TryGetValue(baseKey[0], out byte vk))
                    {
                        keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    // Release Shift
                    keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                else
                {
                    // Direct character input for characters that don't need shift
                    SendCharInput(keyValue[0], foregroundWindow);
                }

                // Update status text
                StatusText.Text = $"Character typed: {keyValue}";
            }

            // Restore focus to the previous foreground window
            SetForegroundWindow(foregroundWindow);
        }
    }

    private void UpdateKeyboardCase()
    {
        bool useUpperCase = isShiftPressed || isCapsLockOn;

        // Update all letter keys to reflect current case
        foreach (var child in KeyboardPanel.Children)
        {
            if (child is Grid row)
            {
                foreach (var key in row.Children)
                {
                    if (key is Button keyButton)
                    {
                        string keyValue = keyButton.Tag?.ToString() ?? keyButton.Content?.ToString() ?? "";

                        // Only update letter keys
                        if (keyValue.Length == 1 && char.IsLetter(keyValue[0]))
                        {
                            keyButton.Content = useUpperCase ? keyValue.ToUpper() : keyValue.ToLower();
                        }
                    }
                }
            }
        }

        // Update visual state of Shift and Caps keys
        ShiftKey.Background = isShiftPressed ?
            Application.Current.Resources["AccentButtonBackground"] as Brush :
            Application.Current.Resources["ButtonBackground"] as Brush;

        CapsKey.Background = isCapsLockOn ?
            Application.Current.Resources["AccentButtonBackground"] as Brush :
            Application.Current.Resources["ButtonBackground"] as Brush;
    }

    private void SendKeyInput(VirtualKey key, IntPtr targetWindow)
    {
        // Convert VirtualKey to byte for keybd_event
        byte vk = (byte)key;

        // Ensure the target window has focus
        SetForegroundWindow(targetWindow);

        // Send key down and key up events
        keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        // Update status text
        StatusText.Text = $"Character typed: {key}";
    }

    private void SendCharInput(char character, IntPtr targetWindow)
    {
        // Ensure the target window has focus
        SetForegroundWindow(targetWindow);

        // For letters and numbers, use the virtual key code
        if (CharToVirtualKey.TryGetValue(char.ToLower(character), out byte vk))
        {
            // If uppercase, simulate shift key
            if (char.IsUpper(character))
            {
                // Press Shift down
                keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Press the key
                keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Release Shift
                keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            else
            {
                // Press the key without shift
                keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        // Update status text
        StatusText.Text = $"Character typed: {character}";
    }

    private void ToggleSpecialCharacters()
    {
        isSpecialCharactersVisible = !isSpecialCharactersVisible;
        SpecialCharactersPanel.Visibility = isSpecialCharactersVisible ? Visibility.Visible : Visibility.Collapsed;
        SpecialCharButton.Background = isSpecialCharactersVisible ?
            Application.Current.Resources["AccentButtonBackground"] as Brush :
            Application.Current.Resources["ButtonBackground"] as Brush;
    }

    private void ToggleDockPosition()
    {
        isDocked = !isDocked;

        if (isDocked)
        {
            DockToBottom();
        }
        else
        {
            CenterOnScreen();
        }

        // Save settings after changing dock position
        SaveSettings();
    }

    private void DockToBottom()
    {
        // Dock to bottom of screen
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        // Get current window size
        GetWindowRect(hwnd, out RECT rect);
        int windowWidth = rect.Right - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;

        // Position at bottom center of screen
        int x = (screenWidth - windowWidth) / 2;
        int y = screenHeight - windowHeight;

        SetWindowPos(hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);

        DockButton.Content = "📌";
    }

    private void CenterOnScreen()
    {
        // Return to center of screen
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        // Get current window size
        GetWindowRect(hwnd, out RECT rect);
        int windowWidth = rect.Right - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;

        // Position at center of screen
        int x = (screenWidth - windowWidth) / 2;
        int y = (screenHeight - windowHeight) / 2;

        SetWindowPos(hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);

        DockButton.Content = "📌";
    }

    private void MinimizeKeyboard()
    {
        // Minimize the window
        appWindow.Hide();
    }

    private void OpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider slider)
        {
            opacity = slider.Value / 100.0;

            // Update window opacity using the compositor
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.Opacity = opacity;
            }

            // Save settings after changing opacity
            SaveSettings();
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is Slider slider)
        {
            fontSize = slider.Value;
            UpdateFontSize(fontSize);

            // Save settings after changing font size
            SaveSettings();
        }
    }

    private void UpdateFontSize(double size)
    {
        // Store the font size value for later use
        fontSize = size;

        // Check if KeyboardPanel is initialized
        if (KeyboardPanel != null)
        {
            // Update font size for all keyboard buttons
            foreach (var child in KeyboardPanel.Children)
            {
                if (child is Grid row)
                {
                    foreach (var key in row.Children)
                    {
                        if (key is Button keyButton)
                        {
                            keyButton.FontSize = size;
                        }
                    }
                }
            }
        }

        // Check if SpecialCharactersPanel is initialized
        if (SpecialCharactersPanel != null)
        {
            foreach (var child in SpecialCharactersPanel.Children)
            {
                if (child is Grid row)
                {
                    foreach (var key in row.Children)
                    {
                        if (key is Button keyButton)
                        {
                            keyButton.FontSize = size;
                        }
                    }
                }
            }
        }

        // Update status text if it's initialized
        if (StatusText != null)
        {
            StatusText.Text = $"Font size: {size}px";
        }
    }

    private void ShowKeyboard_Click(object sender, RoutedEventArgs e)
    {
        // Show the keyboard if it's hidden
        appWindow.Show();
    }
}
