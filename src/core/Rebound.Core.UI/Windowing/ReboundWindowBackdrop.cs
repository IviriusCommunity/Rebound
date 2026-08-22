// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace Rebound.Core.UI.Windowing;

public static unsafe class ReboundWindowBackdrop
{
    public static void Register(Window window)
    {
        var mica = TerraFX.Interop.Windows.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
        TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(
            new((void*)window.GetWindowHandle()),
            (uint)TerraFX.Interop.Windows.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            &mica,
            sizeof(TerraFX.Interop.Windows.DWM_SYSTEMBACKDROP_TYPE));
    }
}
