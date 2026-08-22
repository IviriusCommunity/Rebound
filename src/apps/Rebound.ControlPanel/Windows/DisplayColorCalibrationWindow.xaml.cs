// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Rebound.ControlPanel.Views;
using Rebound.Core.UI.Windowing;
using System;
using System.IO;
using WinUIEx;

namespace Rebound.ControlPanel.Windows;

internal sealed partial class DisplayColorCalibrationWindow : Window
{
    public DisplayColorCalibrationWindow()
    {
        InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = "Display Color Calibration (SDR)";
        AppWindow.SetIcon(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Assets", "Apps", "DisplayColorCalibration.ico"));
        this.SetWindowSize(800, 600);
        this.CenterOnScreen();
        ReboundWindowMenu.Register(this);
    }

    private void RootFrame_Loaded(object sender, RoutedEventArgs e)
        => RootFrame.Navigate(typeof(DisplayColorCalibrationPage), () => Close());
}