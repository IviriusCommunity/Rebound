// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using WinUIEx;

namespace Rebound.Installer;

internal sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonBackgroundColor =
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(80, 120, 120, 120);
        AppWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(40, 120, 120, 120);
        RootFrame.Navigate(typeof(MainPage));
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
        => args.Handled = !App.CanClose;
}