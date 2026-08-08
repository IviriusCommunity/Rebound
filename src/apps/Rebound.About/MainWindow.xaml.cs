// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using WinUIEx;

namespace Rebound.About;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(Views.MainPage));
        unsafe
        {
            var mica = TerraFX.Interop.Windows.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute(
                new((void*)this.GetWindowHandle()),
                (uint)TerraFX.Interop.Windows.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                &mica,
                sizeof(TerraFX.Interop.Windows.DWM_SYSTEMBACKDROP_TYPE));
        }
    }
}