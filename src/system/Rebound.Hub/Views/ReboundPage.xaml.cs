// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Hub.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Rebound.Hub.Views;

internal partial class ReboundPage : Page
{
    public ReboundViewModel ReboundViewModel { get; set; } = new();

    public ReboundPage()
    {
        DataContext = ReboundViewModel;
        InitializeComponent();

        // Branding settings
        if (!SettingsManager.GetValue("ShowBranding", "rebound", true))
        {
            BKGImage.Visibility = CardsScrollViewer.Visibility = TitleGrid.Visibility = Visibility.Collapsed;
            Row1.Height = Row2.Height = new(0);
        }
    }
}