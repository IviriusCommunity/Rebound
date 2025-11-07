// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Forge;
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
    }

    private async void ReboundView_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (var mod in Catalog.Mods)
        {
            await mod.UpdateIntegrityAsync().ConfigureAwait(true);
        }
    }
}