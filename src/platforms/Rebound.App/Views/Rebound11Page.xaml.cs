using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Rebound.Helpers.Modding;
using Rebound.Modding.Instructions;

namespace Rebound.Views;

public partial class Rebound11Page : Page
{
    public WinverInstructions WinverInstructions { get; set; } = new();

    public OnScreenKeyboardInstructions OnScreenKeyboardInstructions { get; set; } = new();

    public Rebound11Page()
    {
        InitializeComponent();
    }
}