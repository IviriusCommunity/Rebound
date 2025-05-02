// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Rebound.Cleanup.ViewModels;

namespace Rebound.Cleanup.Views;

internal sealed partial class MainPage : Page
{
    private MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        InitializeComponent();
        Load();
    }

    private async void Load()
    {
        await Task.Delay(500).ConfigureAwait(true);
        ViewModel.Refresh(ViewModel.ComboBoxItems[0]);
    }
}