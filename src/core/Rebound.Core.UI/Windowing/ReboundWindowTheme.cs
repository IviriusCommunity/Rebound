// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace Rebound.Core.UI.Windowing;

public static unsafe class ReboundWindowTheme
{
    public static void Register(Window window)
    {
        using var listener = new ThemeListener();
        listener.ThemeChanged += (t) =>
        {
            var darkMode = (TerraFX.Interop.Windows.BOOL)(t.CurrentTheme == ApplicationTheme.Dark);
            TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(
                new((void*)window.GetWindowHandle()),
                (uint)TerraFX.Interop.Windows.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &darkMode,
                (uint)sizeof(TerraFX.Interop.Windows.BOOL));

            window.AppWindow.TitleBar.ButtonForegroundColor = darkMode ? Colors.White : Colors.Black;
        };
    }
}
