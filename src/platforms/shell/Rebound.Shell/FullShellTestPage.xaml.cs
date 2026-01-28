// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Shell.ExperienceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Rebound.Shell;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FullShellTestPage : Page
{
    public FullShellTestPage()
    {
        this.InitializeComponent();
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        App.ToggleStartMenu();
    }

    private void Grid_PointerPressed_1(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
