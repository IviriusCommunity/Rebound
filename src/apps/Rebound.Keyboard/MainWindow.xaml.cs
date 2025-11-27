using System;
using System.Collections.Generic;
using CommunityToolkit.WinUI.Converters;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Rebound.Helpers;
using Rebound.Helpers.Windowing;
using Rebound.Keyboard.ViewModels;
using Windows.System;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace Rebound.Keyboard;

public partial class WallpaperGlassBackdrop : CompositionBrushBackdrop
{
    protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
    {
        var LuminosityOpacity = SettingsHelper.GetValue("UseMicaKeyboard", "rosk", true) ? 1F : 0.8F; // Opacity for luminosity overlay
        var TintOpacity = SettingsHelper.GetValue("UseMicaKeyboard", "rosk", true) ? 0.7F : 0.3F; // Opacity for luminosity overlay
        FrameworkElement? root = App.MainAppWindow?.Content as FrameworkElement;

        Color TintColor = Color.FromArgb(255, 32, 32, 32); // fallback

        if (root != null)
        {
            var theme = root.ActualTheme;
            TintColor = theme == ElementTheme.Light
                ? Color.FromArgb(255, 223, 223, 223)
                : Color.FromArgb(255, 32, 32, 32);
        }

        var baseBrush = SettingsHelper.GetValue("UseMicaKeyboard", "rosk", true) ? compositor.TryCreateBlurredWallpaperBackdropBrush() : compositor.CreateHostBackdropBrush();

        // --------- Luminosity Overlay Effect ---------
        var luminosityEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Color,
            Background = new Windows.UI.Composition.CompositionEffectSourceParameter("Wallpaper"),
            Foreground = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityOverlay")
        };

        var luminosityEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new Windows.UI.Composition.CompositionEffectSourceParameter("Wallpaper"),
            Source2 = luminosityEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - LuminosityOpacity,
            Source2Amount = LuminosityOpacity,
            Offset = 0
        };

        var luminosityEffectFactory = compositor.CreateEffectFactory(luminosityEffectComposite);
        var luminosityEffectBrush = luminosityEffectFactory.CreateBrush();

        var luminosityTint = compositor.CreateColorBrush(TintColor);
        luminosityEffectBrush.SetSourceParameter("Wallpaper", baseBrush);
        luminosityEffectBrush.SetSourceParameter("LuminosityOverlay", luminosityTint);

        // --------- Color Overlay Effect ---------
        var colorEffect = new BlendEffect
        {
            Mode = BlendEffectMode.Luminosity,
            Background = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Foreground = new Windows.UI.Composition.CompositionEffectSourceParameter("ColorOverlay")
        };

        var colorEffectComposite = new ArithmeticCompositeEffect
        {
            Source1 = new Windows.UI.Composition.CompositionEffectSourceParameter("LuminosityEffectOutput"), // Use output of luminosityEffect
            Source2 = colorEffect,
            MultiplyAmount = 0,
            Source1Amount = 1 - TintOpacity,
            Source2Amount = TintOpacity,
            Offset = 0
        };

        var colorEffectFactory = compositor.CreateEffectFactory(colorEffectComposite);
        var colorEffectBrush = colorEffectFactory.CreateBrush();

        var colorTint = compositor.CreateColorBrush(TintColor);
        colorEffectBrush.SetSourceParameter("LuminosityEffectOutput", luminosityEffectBrush); // Set luminosityEffectBrush as input
        colorEffectBrush.SetSourceParameter("ColorOverlay", colorTint);

        // Return the final brush with both effects applied
        return colorEffectBrush;
    }
}

public sealed partial class MainWindow : WindowEx
{
    // Constants
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_LAYERED = 0x00080000;

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

    private static readonly Dictionary<string, string> SpecialCharacters = new()
    {
        {"1", "!"}, {"2", "@"}, {"3", "#"}, {"4", "$"}, {"5", "%"},
        {"6", "^"}, {"7", "&"}, {"8", "*"}, {"9", "("}, {"0", ")"},
    
        {"-", "_"}, {"=", "+"}, {"`", "~"}, {"[", "{"}, {"]", "}"},
    
        {"\\", "|"}, {";", ":"}, {"'", "\""}, {",", "<"}, {".", ">"}, {"/", "?"}
    };

    private readonly Dictionary<Button, string> KeyBaseLabels = new();

    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        this.InitializeComponent();

        // Set title
        Title = "Rebound Screen Keyboard";
        this.ExtendsContentIntoTitleBar = true;
        this.TurnOffDoubleClick();
        this.CenterOnScreen();
        SystemBackdrop = new WallpaperGlassBackdrop();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Make the window stay on top and not take focus
        MakeWindowTopMostAndNoActivate();

        var keyboardLayout = new KeyboardLayout
        {
            Rows =
            [
                new()
                {
                    Keys =
                    [
                        new() { Content = "`" },
                        new() { Content = "1" },
                        new() { Content = "2" },
                        new() { Content = "3" },
                        new() { Content = "4" },
                        new() { Content = "5" },
                        new() { Content = "6" },
                        new() { Content = "7" },
                        new() { Content = "8" },
                        new() { Content = "9" },
                        new() { Content = "0" },
                        new() { Content = "-" },
                        new() { Content = "=" },
                        new() { Content = "Backspace", GridColumnRelativeWidthPoints = 2 }
                    ]
                },

                new()
                {
                    Keys =
                    [
                        new() { Content = "Tab" },
                        new() { Content = "q" },
                        new() { Content = "w" },
                        new() { Content = "e" },
                        new() { Content = "r" },
                        new() { Content = "t" },
                        new() { Content = "y" },
                        new() { Content = "u" },
                        new() { Content = "i" },
                        new() { Content = "o" },
                        new() { Content = "p" },
                        new() { Content = "[" },
                        new() { Content = "]" },
                    ]
                },

                new()
                {
                    Keys =
                    [
                        new() { Content = "Caps", IsToggle = true, GridColumnRelativeWidthPoints = 1.5 },
                        new() { Content = "a" },
                        new() { Content = "s" },
                        new() { Content = "d" },
                        new() { Content = "f" },
                        new() { Content = "g" },
                        new() { Content = "h" },
                        new() { Content = "j" },
                        new() { Content = "k" },
                        new() { Content = "l" },
                        new() { Content = ";" },
                        new() { Content = "'" },
                        new() { Content = "\\" },
                        new() { Content = "Enter", GridColumnRelativeWidthPoints = 2 },
                    ]
                },

                new()
                {
                    Keys =
                    [
                        new() { Content = "Shift", IsToggle = true, GridColumnRelativeWidthPoints = 1.5 },
                        new() { Content = "123", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                        new() { Content = "z" },
                        new() { Content = "x" },
                        new() { Content = "c" },
                        new() { Content = "v" },
                        new() { Content = "b" },
                        new() { Content = "n" },
                        new() { Content = "m" },
                        new() { Content = "," },
                        new() { Content = "." },
                        new() { Content = "/" },
                        new() { Content = "Shift", IsToggle = true, GridColumnRelativeWidthPoints = 2.5 },
                    ]
                },

                new()
                {
                    Keys =
                    [
                        new() { Content = "Ctrl", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                        new() { Content = "Win", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                        new() { Content = "Alt", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                        new() { Content = "Space", GridColumnRelativeWidthPoints = 7 },
                        new() { Content = "Alt", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                        new() { Content = "Ctrl", IsToggle = true, GridColumnRelativeWidthPoints = 1 },
                    ]
                }
            ]
        };

        ApplyKeyboardLayout(keyboardLayout);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Opacity))
        {
            this.SetWindowOpacity((byte)ViewModel.Opacity);
        }
    }

    private void ApplyKeyboardLayout(KeyboardLayout layout)
    {
        if (layout == null || KeyboardPanel == null)
        {
            return;
        }

        // Clear existing keyboard panel
        KeyboardPanel.Children.Clear();
        KeyboardPanel.RowDefinitions.Clear();
        KeyBaseLabels.Clear();

        // Create rows and keys based on the layout
        for (var i = 0; i < layout.Rows.Count; i++)
        {
            KeyboardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var rowGrid = new Grid();
            rowGrid.VerticalAlignment = VerticalAlignment.Stretch;
            rowGrid.ColumnSpacing = 4;

            for (var j = 0; j < layout.Rows[i].Keys.Count; j++)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(layout.Rows[i].Keys[j].GridColumnRelativeWidthPoints, GridUnitType.Star) });

                FrameworkElement keyElement;

                if (layout.Rows[i].Keys[j].IsToggle)
                {
                    keyElement = new ToggleButton
                    {
                        Content = new Viewbox()
                        {
                            Child = new TextBlock
                            {
                                Text = layout.Rows[i].Keys[j].Content
                            }
                        },
                        Tag = layout.Rows[i].Keys[j].Content
                    };
                    ((ToggleButton)keyElement).Click += KeyButtonToggle_Click;
                    var binding = new Binding
                    {
                        Path = new PropertyPath(layout.Rows[i].Keys[j].Content switch
                        {
                            "Shift" => nameof(ViewModel.IsShiftKeyPressed),
                            "Alt" => nameof(ViewModel.IsAltKeyPressed),
                            "Caps" => nameof(ViewModel.IsCapsLockOn),
                            "Ctrl" => nameof(ViewModel.IsCtrlKeyPressed),
                            "123" => nameof(ViewModel.IsNumberPadOn),
                            "Win" => nameof(ViewModel.IsWindowsKeyPressed),
                        }),
                        Mode = BindingMode.TwoWay,
                        Converter = new BoolToObjectConverter
                        {
                            TrueValue = true,
                            FalseValue = false
                        },
                        Source = ViewModel
                    };

                    keyElement.SetBinding(ToggleButton.IsCheckedProperty, binding);
                }
                else
                {
                    keyElement = new Button
                    {
                        Content = new Viewbox()
                        {
                            Child = new TextBlock
                            {
                                Text = layout.Rows[i].Keys[j].Content
                            }
                        },
                        Tag = layout.Rows[i].Keys[j].Content
                    };
                    ((Button)keyElement).Click += KeyButton_Click;

                    // Save base label
                    KeyBaseLabels[(Button)keyElement] = layout.Rows[i].Keys[j].Content;
                }

                keyElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                keyElement.VerticalAlignment = VerticalAlignment.Stretch;

                rowGrid.Children.Add(keyElement);
                Grid.SetColumn(keyElement, j);
            }

            KeyboardPanel.Children.Add(rowGrid);
            Grid.SetRow(rowGrid, i);
        }
    }

    private void MakeWindowTopMostAndNoActivate()
    {
        // Set the window to be topmost
        PInvoke.SetWindowPos(new(this.GetWindowHandle()), new(-1), 0, 0, 0, 0, 
            Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOMOVE |
            Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOSIZE | 
            Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

        // Set the window style to not activate when clicked
        var exStyle = PInvoke.GetWindowLongPtr(new(this.GetWindowHandle()), Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle = new IntPtr(exStyle.ToInt64() | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED);
        PInvoke.SetWindowLongPtr(new(this.GetWindowHandle()), Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
    }

    private void KeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            // Store the foreground window before we potentially change it
            HWND foregroundWindow = PInvoke.GetForegroundWindow();

            var keyValue = ((button.Content as Viewbox)?.Child as TextBlock)?.Text;

            // Handle special keys
            switch (keyValue)
            {
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
                default:
                    // For regular keys, send the key input
                    if (keyValue.Length == 1)
                    {
                        var keyChar = keyValue[0];
                        if (ViewModel.IsShiftKeyPressed || ViewModel.IsCapsLockOn)
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
                        if (ViewModel.IsShiftKeyPressed)
                        {
                            ViewModel.IsShiftKeyPressed = false;
                            UpdateKeyboardCase();
                        }
                    }
                    break;
            }

            // Restore focus to the previous foreground window
            PInvoke.SetForegroundWindow(foregroundWindow);
        }
    }

    private void KeyButtonToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button)
        {
            // Store the foreground window before we potentially change it
            HWND foregroundWindow = PInvoke.GetForegroundWindow();

            var keyValue = ((button.Content as Viewbox)?.Child as TextBlock)?.Text;

            // Handle special keys
            switch (keyValue)
            {
                case "Shift":
                    UpdateKeyboardCase();
                    return;
                case "Caps":
                    UpdateKeyboardCase();
                    return;
                case "Win":
                    if (!ViewModel.IsWindowsKeyPressed)
                        SendKeyInput(VirtualKey.LeftWindows, foregroundWindow);
                    return;
            }

            // Restore focus to the previous foreground window
            PInvoke.SetForegroundWindow(foregroundWindow);
        }
    }

    private void UpdateKeyboardCase()
    {
        foreach (var child in KeyboardPanel.Children)
        {
            if (child is Grid row)
            {
                foreach (var key in row.Children)
                {
                    if (key is Button keyButton)
                    {
                        // Get base character from saved labels
                        if (!KeyBaseLabels.TryGetValue(keyButton, out var baseChar))
                            continue;

                        var textBlock = (keyButton.Content as Viewbox)?.Child as TextBlock;
                        if (textBlock == null) continue;

                        // If it's a letter
                        if (baseChar.Length == 1 && char.IsLetter(baseChar[0]))
                        {
                            var useUpper = ViewModel.IsCapsLockOn ^ ViewModel.IsShiftKeyPressed;
                            textBlock.Text = useUpper ? baseChar.ToUpper() : baseChar.ToLower();
                        }

                        // If it's a special char (like 1 -> !)
                        else if (SpecialCharacters.TryGetValue(baseChar, out var shiftedChar))
                        {
                            textBlock.Text = ViewModel.IsShiftKeyPressed ? shiftedChar : baseChar;
                        }

                        // Else just reset to base
                        else
                        {
                            textBlock.Text = baseChar;
                        }
                    }
                }
            }
        }
    }

    private void SendKeyInput(VirtualKey key, HWND targetWindow)
    {
        // Convert VirtualKey to byte for keybd_event
        byte vk = (byte)key;

        // Ensure the target window has focus
        PInvoke.SetForegroundWindow(targetWindow);

        // Send key down and key up events
        PInvoke.keybd_event(vk, 0, 0, UIntPtr.Zero);
        PInvoke.keybd_event(vk, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void SendCharInput(char character, HWND targetWindow)
    {
        // Ensure the target window has focus
        PInvoke.SetForegroundWindow(targetWindow);

        // For letters and numbers, use the virtual key code
        if (CharToVirtualKey.TryGetValue(char.ToLower(character), out byte vk))
        {
            // If uppercase, simulate shift key
            if (char.IsUpper(character))
            {
                // Press Shift down
                PInvoke.keybd_event(0x10, 0, 0, UIntPtr.Zero);

                // Press the key
                PInvoke.keybd_event(vk, 0, 0, UIntPtr.Zero);
                PInvoke.keybd_event(vk, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Release Shift
                PInvoke.keybd_event(0x10, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            else
            {
                // Press the key without shift
                PInvoke.keybd_event(vk, 0, 0, UIntPtr.Zero);
                PInvoke.keybd_event(vk, 0, Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
    }
}
