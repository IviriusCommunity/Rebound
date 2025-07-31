using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.Keyboard.ViewModels;

public partial class KeyboardLayout
{
    public List<KeyboardRow> Rows { get; set; } = [];
}

public partial class KeyboardRow
{
    public List<KeyboardKey> Keys { get; set; } = [];
}

public partial class KeyboardKey
{
    public string Content { get; set; } = string.Empty;
    public bool IsToggle { get; set; }
    public bool IsChecked { get; set; }
    public double GridColumnRelativeWidthPoints { get; set; } = 1;
}

[ObservableObject]
public partial class MainViewModel
{
    [ObservableProperty]
    public partial bool IsShiftKeyPressed { get; set; }

    [ObservableProperty]
    public partial bool IsCtrlKeyPressed { get; set; }

    [ObservableProperty]
    public partial bool IsAltKeyPressed { get; set; }

    [ObservableProperty]
    public partial bool IsCapsLockOn { get; set; }

    [ObservableProperty]
    public partial bool IsNumLockOn { get; set; }

    [ObservableProperty]
    public partial bool IsNumberPadOn { get; set; }

    [ObservableProperty]
    public partial bool IsScrollLockOn { get; set; }

    [ObservableProperty]
    public partial bool IsWindowsKeyPressed { get; set; }

    [ObservableProperty]
    public partial double Opacity { get; set; } = 255;
}